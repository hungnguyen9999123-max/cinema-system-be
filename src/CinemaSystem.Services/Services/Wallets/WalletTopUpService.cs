using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Data.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Wallets;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.Services.Services.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CinemaSystem.Services.Services.Wallets;

public sealed class WalletTopUpService(
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IConfiguration configuration,
    ILogger<WalletTopUpService> logger) : IWalletTopUpService
{
    private const string Pending = "PENDING";
    private const string Success = "SUCCESS";
    private const string Failed = "FAILED";
    private const string Expired = "EXPIRED";
    private const int MaxTransientDatabaseAttempts = 3;

    private readonly VnPayOptions _vnpay = VnPayOptions.FromConfiguration(configuration);
    private readonly decimal _minimumAmount = configuration.GetValue<decimal?>("Wallet:TopUpMinAmount") ?? 10_000m;
    private readonly decimal _maximumAmount = configuration.GetValue<decimal?>("Wallet:TopUpMaxAmount") ?? 10_000_000m;

    public async Task<WalletTopUpResponseDto> CreateAsync(
        Guid customerId,
        string idempotencyKey,
        CreateWalletTopUpRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty) throw new UnauthorizedAccessException("Bạn cần đăng nhập để nạp tiền vào ví.");
        if (!Guid.TryParse(idempotencyKey, out _)) throw new InvalidOperationException("Idempotency-Key phải là UUID.");
        if (request.Amount != decimal.Truncate(request.Amount) || request.Amount < _minimumAmount || request.Amount > _maximumAmount)
            throw new InvalidOperationException($"Số tiền nạp phải là số nguyên từ {_minimumAmount:N0} đến {_maximumAmount:N0} VND.");

        var keyHash = Hash(idempotencyKey);
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var existing = await walletRepository.GetTopUpByIdempotencyKeyAsync(customerId, keyHash, cancellationToken);
            if (existing is not null)
            {
                if (existing.Status == Pending && existing.ExpiresAt <= DateTime.UtcNow)
                {
                    existing.Status = Expired;
                    walletRepository.Update(existing);
                }
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                return ToResponse(existing, existing.Status == Pending ? BuildPaymentUrl(existing) : null);
            }

            var now = DateTime.UtcNow;
            var wallet = await walletRepository.GetOrCreateAsync(customerId, cancellationToken);
            var topUp = new WalletTopUp
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Wallet = wallet,
                RequestedBy = customerId,
                Amount = decimal.Round(request.Amount, 0),
                Status = Pending,
                IdempotencyKeyHash = keyHash,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(15)
            };
            await walletRepository.AddTopUpAsync(topUp, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return ToResponse(topUp, BuildPaymentUrl(topUp));
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<WalletTopUpPagedResultDto> GetMineAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        // Expire stale local records before returning history.  The gateway may
        // never call back when the customer closes its page, so this prevents
        // an old PENDING top-up from remaining actionable in the UI.
        await walletRepository.ExpirePendingTopUpsAsync(customerId, DateTime.UtcNow, cancellationToken);
        var (items, total) = await walletRepository.GetTopUpsForCustomerAsync(customerId, page, pageSize, cancellationToken);
        return new WalletTopUpPagedResultDto
        {
            Items = items.Select(item => ToResponse(item)).ToArray(),
            Page = Math.Max(1, page),
            PageSize = Math.Clamp(pageSize, 1, 100),
            TotalCount = total
        };
    }

    public async Task<WalletTopUpCallbackResult> HandleVnPayReturnAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken = default)
    {
        if (!IsValidSignature(query)) throw new InvalidOperationException("Invalid VNPay signature.");
        try
        {
            var result = await ProcessVnPayNotificationWithRetryAsync(query, cancellationToken);
            return new WalletTopUpCallbackResult(result.Response, result.AlreadyProcessed, BuildFrontendReturnUrl(result.Response));
        }
        catch (Exception ex) when (IsTransientDatabaseFailure(ex) && Guid.TryParse(GetValue(query, "vnp_txnref"), out var topUpId))
        {
            // VNPAY will also call the IPN endpoint. Keep the customer on the
            // wallet page while that callback retries instead of exposing a raw
            // database error after a successful bank payment.
            logger.LogError(ex, "Wallet top-up callback could not reach the database after retries for {TopUpId}.", topUpId);
            var pending = new WalletTopUpResponseDto { TopUpId = topUpId, Status = Pending };
            return new WalletTopUpCallbackResult(pending, AlreadyProcessed: false, BuildFrontendReturnUrl(pending));
        }
    }

    public async Task<VnPayIpnResponse> HandleVnPayIpnAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken = default)
    {
        if (!IsValidSignature(query)) return new VnPayIpnResponse("97", "Invalid Checksum");
        try
        {
            var result = await ProcessVnPayNotificationWithRetryAsync(query, cancellationToken);
            return result.AlreadyProcessed ? new VnPayIpnResponse("02", "Order already confirmed") : new VnPayIpnResponse("00", "Confirm Success");
        }
        catch (KeyNotFoundException) { return new VnPayIpnResponse("01", "Order not found"); }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("VNPay amount", StringComparison.Ordinal)) { return new VnPayIpnResponse("04", "Invalid amount"); }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling VNPay wallet top-up IPN.");
            return new VnPayIpnResponse("99", "Unknown error");
        }
    }

    private async Task<NotificationResult> ProcessVnPayNotificationWithRetryAsync(
        IReadOnlyDictionary<string, string> query,
        CancellationToken cancellationToken)
    {
        for (var attempt = 1; ; attempt++)
        {
            try
            {
                return await ProcessVnPayNotificationAsync(query, cancellationToken);
            }
            catch (Exception ex) when (attempt < MaxTransientDatabaseAttempts && IsTransientDatabaseFailure(ex))
            {
                logger.LogWarning(
                    ex,
                    "Transient database failure while processing a VNPAY wallet top-up callback. Retrying attempt {Attempt} of {MaxAttempts}.",
                    attempt + 1,
                    MaxTransientDatabaseAttempts);
                await Task.Delay(TimeSpan.FromSeconds(attempt), cancellationToken);
            }
        }
    }

    private async Task<NotificationResult> ProcessVnPayNotificationAsync(IReadOnlyDictionary<string, string> query, CancellationToken cancellationToken)
    {
        var reference = GetValue(query, "vnp_txnref");
        if (!Guid.TryParse(reference, out var topUpId)) throw new InvalidOperationException("Invalid VNPay transaction reference.");

        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var topUp = await walletRepository.GetTopUpByIdAsync(topUpId, cancellationToken) ?? throw new KeyNotFoundException("Wallet top-up not found.");
            EnsureAmountMatches(query, topUp);
            if (topUp.Status != Pending)
            {
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                return new NotificationResult(ToResponse(topUp), true);
            }

            // VNPAY Sandbox uses transaction number "0" for a cancelled or
            // otherwise unsuccessful checkout. The database reserves gateway
            // transaction numbers as unique values, so persisting "0" would
            // make later failed callbacks collide and remain PENDING.
            var gatewayTxnId = GetValue(query, "vnp_transactionno");
            topUp.GatewayTxnId = string.Equals(gatewayTxnId, "0", StringComparison.Ordinal) ? null : gatewayTxnId;
            topUp.ResponseCode = GetValue(query, "vnp_responsecode");
            topUp.TransactionStatus = GetValue(query, "vnp_transactionstatus");
            topUp.CompletedAt = DateTime.UtcNow;

            if (topUp.ExpiresAt <= DateTime.UtcNow)
            {
                topUp.Status = Expired;
            }
            else if (IsSuccess(query))
            {
                var wallet = topUp.Wallet;
                if (!await walletRepository.HasTopUpCreditAsync(topUp.Id, cancellationToken))
                {
                    wallet.Balance += topUp.Amount;
                    wallet.UpdatedAt = DateTime.UtcNow;
                    await walletRepository.AddTransactionAsync(new WalletTransaction
                    {
                        Id = Guid.NewGuid(),
                        WalletId = wallet.Id,
                        Wallet = wallet,
                        WalletTopUpId = topUp.Id,
                        WalletTopUp = topUp,
                        Type = WalletTransactionType.TopUpCredit,
                        Amount = topUp.Amount,
                        BalanceAfter = wallet.Balance,
                        Description = "Nạp tiền vào ví qua VNPAY",
                        CreatedAt = DateTime.UtcNow
                    }, cancellationToken);
                    walletRepository.Update(wallet);
                }
                topUp.Status = Success;
            }
            else
            {
                topUp.Status = Failed;
            }

            walletRepository.Update(topUp);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return new NotificationResult(ToResponse(topUp), false);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private string BuildPaymentUrl(WalletTopUp topUp)
    {
        _vnpay.EnsureConfigured();
        var createdAt = topUp.CreatedAt.AddHours(7);
        var expiresAt = topUp.ExpiresAt.AddHours(7);
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnpay.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _vnpay.TmnCode,
            ["vnp_Amount"] = ((long)(topUp.Amount * 100m)).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = createdAt.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_ExpireDate"] = expiresAt.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_IpAddr"] = "127.0.0.1",
            ["vnp_Locale"] = _vnpay.Locale,
            // VNPAY requires OrderInfo to be plain, non-accented text without
            // special punctuation.  Keep the top-up reference alphanumeric so
            // bank-specific sandbox validation does not reject the transaction
            // before it reaches the card/OTP step.
            ["vnp_OrderInfo"] = $"Nap vi CINE MAX {topUp.Id:N}",
            ["vnp_OrderType"] = _vnpay.OrderType,
            ["vnp_ReturnUrl"] = GetTopUpReturnUrl(),
            ["vnp_TxnRef"] = topUp.Id.ToString("N")
        };
        var signData = BuildQueryString(parameters);
        return $"{_vnpay.PaymentUrl}?{signData}&vnp_SecureHash={ComputeHmacSha512(_vnpay.HashSecret, signData)}";
    }

    private bool IsValidSignature(IReadOnlyDictionary<string, string> query)
    {
        _vnpay.EnsureConfigured();
        var provided = GetValue(query, "vnp_securehash");
        if (string.IsNullOrWhiteSpace(provided)) return false;
        var values = query.Where(pair => pair.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase)
                && !pair.Key.Equals("vnp_securehash", StringComparison.OrdinalIgnoreCase)
                && !pair.Key.Equals("vnp_securehashtype", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(pair => pair.Key, pair => pair.Value);
        var expected = ComputeHmacSha512(_vnpay.HashSecret, BuildQueryString(values));
        return CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(expected.ToUpperInvariant()), Encoding.UTF8.GetBytes(provided.ToUpperInvariant()));
    }

    private static void EnsureAmountMatches(IReadOnlyDictionary<string, string> query, WalletTopUp topUp)
    {
        var amountText = GetValue(query, "vnp_amount");
        if (!long.TryParse(amountText, out var amount) || amount != (long)(topUp.Amount * 100m))
            throw new InvalidOperationException("VNPay amount does not match wallet top-up.");
    }

    private static bool IsSuccess(IReadOnlyDictionary<string, string> query) =>
        GetValue(query, "vnp_responsecode") == "00" && GetValue(query, "vnp_transactionstatus") == "00";

    private string GetTopUpReturnUrl()
    {
        if (!Uri.TryCreate(_vnpay.ReturnUrl, UriKind.Absolute, out var returnUri)) return _vnpay.ReturnUrl;
        var builder = new UriBuilder(returnUri) { Path = "/api/wallet/topups/vnpay/return", Query = string.Empty };
        return builder.Uri.ToString();
    }

    private string BuildFrontendReturnUrl(WalletTopUpResponseDto topUp)
    {
        var frontend = Uri.TryCreate(_vnpay.FrontendReturnUrl, UriKind.Absolute, out var returnUri)
            ? new UriBuilder(returnUri.Scheme, returnUri.Host, returnUri.Port, "/wallet").Uri.ToString()
            : "/wallet";
        var separator = frontend.Contains('?') ? "&" : "?";
        return $"{frontend}{separator}topupId={topUp.TopUpId}&status={topUp.Status}";
    }

    private static WalletTopUpResponseDto ToResponse(WalletTopUp topUp, string? paymentUrl = null) => new()
    {
        TopUpId = topUp.Id,
        Amount = topUp.Amount,
        Status = topUp.Status,
        GatewayTxnId = topUp.GatewayTxnId,
        PaymentUrl = paymentUrl,
        CreatedAt = topUp.CreatedAt,
        ExpiresAt = topUp.ExpiresAt,
        CompletedAt = topUp.CompletedAt
    };

    private static string? GetValue(IReadOnlyDictionary<string, string> query, string key)
    {
        var pair = query.FirstOrDefault(value => value.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        return pair.Key is null ? null : pair.Value;
    }

    private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> values) => string.Join("&", values
        .Where(pair => !string.IsNullOrEmpty(pair.Value))
        .OrderBy(pair => pair.Key, StringComparer.Ordinal)
        .Select(pair => $"{Encode(pair.Key)}={Encode(pair.Value)}"));
    private static string Encode(string value) => Uri.EscapeDataString(value).Replace("%20", "+");
    private static string ComputeHmacSha512(string secret, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secret));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()))).ToLowerInvariant();

    private static bool IsTransientDatabaseFailure(Exception exception)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
        {
            if (current is TimeoutException or DbException ||
                current.Message.Contains("likely due to a transient failure", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private sealed record NotificationResult(WalletTopUpResponseDto Response, bool AlreadyProcessed);

    private sealed class VnPayOptions
    {
        public string TmnCode { get; private init; } = string.Empty;
        public string HashSecret { get; private init; } = string.Empty;
        public string PaymentUrl { get; private init; } = string.Empty;
        public string ReturnUrl { get; private init; } = string.Empty;
        public string FrontendReturnUrl { get; private init; } = string.Empty;
        public string Version { get; private init; } = "2.1.0";
        public string Locale { get; private init; } = "vn";
        public string OrderType { get; private init; } = "other";
        public static VnPayOptions FromConfiguration(IConfiguration configuration)
        {
            var section = configuration.GetSection("VnPay");
            return new VnPayOptions
            {
                TmnCode = (section["TmnCode"] ?? string.Empty).Trim(),
                HashSecret = (section["HashSecret"] ?? string.Empty).Trim(),
                PaymentUrl = (section["PaymentUrl"] ?? string.Empty).Trim(),
                ReturnUrl = (section["ReturnUrl"] ?? string.Empty).Trim(),
                FrontendReturnUrl = (section["FrontendReturnUrl"] ?? string.Empty).Trim(),
                Version = (section["Version"] ?? "2.1.0").Trim(),
                Locale = (section["Locale"] ?? "vn").Trim(),
                OrderType = (section["OrderType"] ?? "other").Trim()
            };
        }
        public void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(TmnCode) || string.IsNullOrWhiteSpace(HashSecret) || string.IsNullOrWhiteSpace(PaymentUrl) || string.IsNullOrWhiteSpace(ReturnUrl) || string.IsNullOrWhiteSpace(FrontendReturnUrl))
                throw new InvalidOperationException("VNPay configuration is missing.");
        }
    }
}
