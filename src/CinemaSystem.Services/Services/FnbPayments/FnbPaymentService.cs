using CinemaSystem.Common.DTOs.Payments;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.Services.Services.FnbPayments;

public sealed class FnbPaymentService : IFnbPaymentService
{
    private const string GatewayVnPay = "VNPAY";
    private const string PaymentPending = "PENDING";
    private const string PaymentSuccess = "SUCCESS";
    private const string PaymentFailed = "FAILED";
    private const string FnbOrderConfirmed = "CONFIRMED";
    private const string FnbOrderPending = "PENDING";

    private readonly IFnbOrderRepository _fnbOrderRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<FnbPaymentService> _logger;
    private readonly VnPayOptions _vnPayOptions;

    public FnbPaymentService(
        IFnbOrderRepository fnbOrderRepository,
        IPaymentRepository paymentRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ILogger<FnbPaymentService> logger)
    {
        _fnbOrderRepository = fnbOrderRepository;
        _paymentRepository = paymentRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
        _vnPayOptions = VnPayOptions.FromConfiguration(configuration);
    }

    public async Task<FnbPaymentResponseDto> CreatePaymentAsync(
        Guid staffId,
        CreateFnbPaymentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var gateway = NormalizeGateway(request.Gateway);
        if (gateway != GatewayVnPay)
        {
            throw new InvalidOperationException("Only VNPAY gateway is supported for F&B payments.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var fnbOrder = await _fnbOrderRepository.GetByIdWithDetailsAsync(request.FnbOrderId, cancellationToken);
            if (fnbOrder == null)
            {
                throw new KeyNotFoundException("F&B order not found.");
            }

            if (fnbOrder.OrderStatus != FnbOrderPending)
            {
                throw new InvalidOperationException($"F&B order is not payable because it is {fnbOrder.OrderStatus}.");
            }

            var latestPayment = await _paymentRepository.GetLatestForFnbOrderAsync(request.FnbOrderId, gateway, cancellationToken);
            if (latestPayment != null && latestPayment.Status == PaymentPending)
            {
                latestPayment.Amount = fnbOrder.TotalAmount;
                _paymentRepository.Update(latestPayment);
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return ToResponse(latestPayment, fnbOrder, BuildVnPayPaymentUrl(latestPayment, fnbOrder));
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                FnbOrderId = request.FnbOrderId,
                Gateway = gateway,
                Amount = fnbOrder.TotalAmount,
                Status = PaymentPending,
                CreatedAt = DateTime.UtcNow
            };

            await _paymentRepository.AddAsync(payment, cancellationToken);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return ToResponse(payment, fnbOrder, BuildVnPayPaymentUrl(payment, fnbOrder));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error creating F&B VNPay payment.");
            throw;
        }
    }

    public async Task<FnbPaymentResponseDto> HandleVnPayReturnAsync(
        IReadOnlyDictionary<string, string> query,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidVnPaySignature(query))
        {
            throw new InvalidOperationException("Invalid VNPay signature.");
        }

        var paymentIdText = GetValue(query, "vnp_txnref");
        if (string.IsNullOrWhiteSpace(paymentIdText) || !Guid.TryParse(paymentIdText, out var paymentId))
        {
            throw new InvalidOperationException("Invalid VNPay transaction reference.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
            if (payment == null)
            {
                throw new KeyNotFoundException("Payment not found.");
            }

            var fnbOrder = payment.FnbOrder;
            if (fnbOrder == null)
            {
                throw new InvalidOperationException("F&B order not found for this payment.");
            }

            EnsureVnPayAmountMatchesPayment(query, payment);

            if (payment.Status == PaymentSuccess)
            {
                await _unitOfWork.CommitTransactionAsync(cancellationToken);
                return ToResponse(payment, fnbOrder, BuildFrontendReturnUrl(payment));
            }

            payment.GatewayTxnId = GetValue(query, "vnp_transactionno");
            payment.IpnSignature = GetValue(query, "vnp_securehash");

            if (IsVnPaySuccess(query))
            {
                payment.Status = PaymentSuccess;
                payment.PaidAt = DateTime.UtcNow;
                fnbOrder.OrderStatus = FnbOrderConfirmed;
                _fnbOrderRepository.Update(fnbOrder);
            }
            else
            {
                payment.Status = PaymentFailed;
                if (fnbOrder.OrderStatus == FnbOrderPending)
                {
                    fnbOrder.OrderStatus = "CANCELLED";
                    _fnbOrderRepository.Update(fnbOrder);
                }
            }

            _paymentRepository.Update(payment);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);
            return ToResponse(payment, fnbOrder, BuildFrontendReturnUrl(payment));
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            _logger.LogError(ex, "Error handling F&B VNPay return.");
            throw;
        }
    }

    public string BuildVnPayPaymentUrl(Payment payment, FnbOrder fnbOrder)
    {
        _vnPayOptions.EnsureConfigured();

        var now = DateTime.UtcNow.AddHours(7);
        var expireDate = now.AddMinutes(30);
        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnPayOptions.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _vnPayOptions.TmnCode,
            ["vnp_Amount"] = ((long)(payment.Amount * 100)).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_ExpireDate"] = expireDate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_IpAddr"] = "127.0.0.1",
            ["vnp_Locale"] = _vnPayOptions.Locale,
            ["vnp_OrderInfo"] = $"Thanh toan F&B order {fnbOrder.Id.ToString()[..8]}",
            ["vnp_OrderType"] = _vnPayOptions.OrderType,
            ["vnp_ReturnUrl"] = !string.IsNullOrWhiteSpace(_vnPayOptions.StaffFnbReturnUrl)
                ? _vnPayOptions.StaffFnbReturnUrl
                : _vnPayOptions.FrontendReturnUrl,
            ["vnp_TxnRef"] = payment.Id.ToString("N")
        };

        var query = BuildQueryString(parameters);
        var secureHash = ComputeHmacSha512(_vnPayOptions.HashSecret, query);
        return $"{_vnPayOptions.PaymentUrl}?{query}&vnp_SecureHash={secureHash}";
    }

    private bool IsValidVnPaySignature(IReadOnlyDictionary<string, string> query)
    {
        _vnPayOptions.EnsureConfigured();
        var secureHash = GetValue(query, "vnp_securehash");
        if (string.IsNullOrWhiteSpace(secureHash)) return false;

        var signedParameters = query
            .Where(p => p.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase) &&
                        !p.Key.Equals("vnp_securehash", StringComparison.OrdinalIgnoreCase) &&
                        !p.Key.Equals("vnp_securehashtype", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(p => p.Key, p => p.Value);

        var signData = BuildQueryString(signedParameters);
        var expectedHash = ComputeHmacSha512(_vnPayOptions.HashSecret, signData);

        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedHash.ToUpperInvariant()),
            Encoding.UTF8.GetBytes(secureHash.ToUpperInvariant()));
    }

    private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Join("&", parameters
            .Where(p => !string.IsNullOrEmpty(p.Value))
            .OrderBy(p => p.Key, StringComparer.Ordinal)
            .Select(p => $"{p.Key}={VnPayEncode(p.Value)}"));
    }

    private static string VnPayEncode(string value)
    {
        return Uri.EscapeDataString(value).Replace("%20", "+");
    }

    private static string ComputeHmacSha512(string secretKey, string data)
    {
        using var hmac = new HMACSHA512(Encoding.UTF8.GetBytes(secretKey));
        return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(data))).ToLowerInvariant();
    }

    private static void EnsureVnPayAmountMatchesPayment(IReadOnlyDictionary<string, string> query, Payment payment)
    {
        var amountText = GetValue(query, "vnp_amount");
        if (string.IsNullOrWhiteSpace(amountText) || !long.TryParse(amountText, out var vnpAmount))
        {
            throw new InvalidOperationException("Invalid VNPay amount.");
        }
        var expectedAmount = (long)(payment.Amount * 100);
        if (vnpAmount != expectedAmount)
        {
            throw new InvalidOperationException("VNPay amount does not match payment.");
        }
    }

    private static bool IsVnPaySuccess(IReadOnlyDictionary<string, string> query)
    {
        return GetValue(query, "vnp_responsecode") == "00" &&
               GetValue(query, "vnp_transactionstatus") == "00";
    }

    private static string? GetValue(IReadOnlyDictionary<string, string> query, string key)
    {
        var match = query.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        return match.Key != null ? match.Value : null;
    }

    private FnbPaymentResponseDto ToResponse(Payment payment, FnbOrder fnbOrder, string? paymentUrl = null, string? redirectUrl = null)
    {
        return new FnbPaymentResponseDto
        {
            PaymentId = payment.Id,
            FnbOrderId = fnbOrder.Id,
            Gateway = payment.Gateway,
            Amount = payment.Amount,
            PaymentStatus = payment.Status,
            GatewayTxnId = payment.GatewayTxnId,
            PaymentUrl = paymentUrl,
            RedirectUrl = redirectUrl
        };
    }

    private string BuildFrontendReturnUrl(Payment payment)
    {
        var separator = _vnPayOptions.StaffFnbResultUrl.Contains('?') ? "&" : "?";
        return $"{_vnPayOptions.StaffFnbResultUrl}{separator}paymentId={payment.Id}&status={payment.Status}&type=fnb";
    }

    private static string NormalizeGateway(string? gateway)
    {
        return string.IsNullOrWhiteSpace(gateway) ? GatewayVnPay : gateway.Trim().ToUpperInvariant();
    }

    private sealed class VnPayOptions
    {
        public string TmnCode { get; private init; } = string.Empty;
        public string HashSecret { get; private init; } = string.Empty;
        public string PaymentUrl { get; private init; } = string.Empty;
        public string ReturnUrl { get; private init; } = string.Empty;
        public string FrontendReturnUrl { get; private init; } = string.Empty;
        public string StaffFnbReturnUrl { get; private init; } = string.Empty;
        public string StaffFnbResultUrl { get; private init; } = string.Empty;
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
                StaffFnbReturnUrl = (section["StaffFnbReturnUrl"] ?? string.Empty).Trim(),
                StaffFnbResultUrl = (section["StaffFnbResultUrl"] ?? string.Empty).Trim(),
                Version = (section["Version"] ?? "2.1.0").Trim(),
                Locale = (section["Locale"] ?? "vn").Trim(),
                OrderType = (section["OrderType"] ?? "other").Trim()
            };
        }

        public void EnsureConfigured()
        {
            if (string.IsNullOrWhiteSpace(TmnCode) ||
                string.IsNullOrWhiteSpace(HashSecret) ||
                string.IsNullOrWhiteSpace(PaymentUrl) ||
                string.IsNullOrWhiteSpace(ReturnUrl) ||
                string.IsNullOrWhiteSpace(FrontendReturnUrl))
            {
                throw new InvalidOperationException("VNPay configuration is missing.");
            }
        }
    }
}
