using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Payments;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.Common.Helpers;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.Services.Services.QrTickets;
using CinemaSystem.Services.Services.Promotions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.Services.Services.Payments;

public class PaymentService : IPaymentService
{
    private const string GatewayVnPay = "VNPAY";
    private const string GatewayWallet = "WALLET";
    private const string PaymentPending = "PENDING";
    private const string PaymentPaid = "SUCCESS";
    private const string PaymentFailed = "FAILED";
    private const string BookingPending = "PENDING";
    private const string BookingConfirmed = "CONFIRMED";
    private const string BookingCancelled = "CANCELLED";
    private const string BookingExpired = "EXPIRED";
    private const string SeatHeld = "HELD";
    private const string SeatBooked = "BOOKED";
    private const string SeatReleased = "RELEASED";
    private const string FnbPending = "PENDING";
    private const string FnbConfirmed = "CONFIRMED";
    private const string FnbCancelled = "CANCELLED";
    private const string FallbackIpAddress = "127.0.0.1";

    private readonly IBookingRepository _bookingRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IQrTicketService _qrTicketService;
    private readonly IPromotionService _promotionService;
    private readonly IWalletRepository _walletRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<PaymentService> _logger;
    private readonly VnPayOptions _vnPayOptions;

    public PaymentService(
        IBookingRepository bookingRepository,
        IPaymentRepository paymentRepository,
        IQrTicketService qrTicketService,
        IPromotionService promotionService,
        IWalletRepository walletRepository,
        IUnitOfWork unitOfWork,
        IHttpContextAccessor httpContextAccessor,
        IConfiguration configuration,
        ILogger<PaymentService> logger)
    {
        _bookingRepository = bookingRepository;
        _paymentRepository = paymentRepository;
        _qrTicketService = qrTicketService;
        _promotionService = promotionService;
        _walletRepository = walletRepository;
        _unitOfWork = unitOfWork;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _vnPayOptions = VnPayOptions.FromConfiguration(configuration);
    }

    public async Task<PaymentResponseDto> CreatePaymentAsync(
        Guid customerId,
        string idempotencyKey,
        CreatePaymentRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var gateway = NormalizeGateway(request.Gateway);
        if (gateway is not (GatewayVnPay or GatewayWallet)) throw new InvalidOperationException("Unsupported payment gateway.");
        if (!Guid.TryParse(idempotencyKey, out _)) throw new InvalidOperationException("Idempotency-Key must be a UUID.");
        var idempotencyKeyHash = Hash(idempotencyKey);

        PaymentResponseDto response;
        try
        {
            response = await _unitOfWork.ExecuteInTransactionAsync(async ct =>
            {
                var booking = await _bookingRepository.GetByIdAsync(request.BookingId, ct);
                if (booking == null) throw new KeyNotFoundException("Booking not found.");

                if (booking.CustomerId != customerId)
                    throw new UnauthorizedAccessException("You can only pay for your own booking.");

                var idempotentPayment = await _paymentRepository.GetByBookingAndIdempotencyKeyAsync(booking.Id, idempotencyKeyHash, ct);
                if (idempotentPayment is not null)
                {
                    return ToResponse(idempotentPayment, booking,
                        idempotentPayment.Status == PaymentPending && idempotentPayment.Gateway == GatewayVnPay
                            ? BuildVnPayPaymentUrl(idempotentPayment, booking)
                            : null,
                        idempotentPayment.Status == PaymentPaid ? BuildFrontendReturnUrl(idempotentPayment) : null);
                }

                var successfulPayment = await _paymentRepository.GetSuccessfulForBookingAsync(booking.Id, ct);
                if (successfulPayment is not null)
                    return ToResponse(successfulPayment, booking, redirectUrl: BuildFrontendReturnUrl(successfulPayment));

                EnsurePendingBookingCanBePaid(booking);

                var latestPayment = await _paymentRepository.GetLatestForBookingAsync(booking.Id, gateway, ct);
                if (gateway == GatewayVnPay && latestPayment != null && latestPayment.Status is PaymentPending or PaymentPaid)
                {
                    latestPayment.Amount = booking.FinalAmount;
                    _paymentRepository.Update(latestPayment);
                    return ToResponse(latestPayment, booking, BuildVnPayPaymentUrl(latestPayment, booking));
                }

                var createdAt = DateTime.UtcNow;
                var payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    Gateway = gateway,
                    Amount = booking.FinalAmount,
                    Status = gateway == GatewayWallet ? PaymentPaid : PaymentPending,
                    CreatedAt = createdAt,
                    GatewayRequestAt = gateway == GatewayVnPay ? createdAt : null,
                    IdempotencyKeyHash = idempotencyKeyHash,
                    Booking = booking
                };

                await _paymentRepository.AddAsync(payment, ct);
                if (!booking.Payments.Contains(payment)) booking.Payments.Add(payment);

                if (gateway == GatewayWallet)
                {
                    var walletDebit = await _walletRepository.TryDebitAsync(customerId, payment.Amount, createdAt, ct);
                    if (walletDebit is null)
                        throw new BusinessConflictException("Số dư Ví CINE-MAX không đủ. Vui lòng nạp thêm tiền.");

                    payment.PaidAt = createdAt;
                    MarkPaymentSuccess(payment, booking);
                    await _walletRepository.AddTransactionAsync(new WalletTransaction
                    {
                        Id = Guid.NewGuid(),
                        WalletId = walletDebit.Value.WalletId,
                        PaymentId = payment.Id,
                        Payment = payment,
                        Type = WalletTransactionType.BookingPaymentDebit,
                        Amount = -payment.Amount,
                        BalanceAfter = walletDebit.Value.BalanceAfter,
                        Description = $"Thanh toán booking {booking.BookingRef} bằng Ví CINE-MAX",
                        CreatedAt = createdAt
                    }, ct);

                    if (booking.PromotionId.HasValue)
                        await _promotionService.RecordUsageAsync(booking.Id, booking.CustomerId, booking.PromotionId.Value, ct);

                    await _qrTicketService.GenerateTicketsForBookingAsync(booking, ct);
                    _paymentRepository.Update(payment);
                    _bookingRepository.Update(booking);
                    return ToResponse(payment, booking, redirectUrl: BuildFrontendReturnUrl(payment));
                }

                return ToResponse(payment, booking, paymentUrl: BuildVnPayPaymentUrl(payment, booking));
            }, cancellationToken);
        }
        catch (BusinessConflictException) { throw; }
        catch (UnauthorizedAccessException) { throw; }
        catch (InvalidOperationException) { throw; }
        catch (KeyNotFoundException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment.");
            throw;
        }

        return response;
    }

    public async Task<PaymentResponseDto> HandleVnPayReturnAsync(
        IReadOnlyDictionary<string, string> query,
        bool isPosStaff = false,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidVnPaySignature(query))
        {
            throw new InvalidOperationException("Invalid VNPay signature.");
        }

        var result = await ProcessVnPayNotificationAsync(query, isPosStaff, cancellationToken);
        return result.Response;
    }

    public async Task<VnPayIpnResponse> HandleVnPayIpnAsync(
        IReadOnlyDictionary<string, string> query,
        CancellationToken cancellationToken = default)
    {
        if (!IsValidVnPaySignature(query))
        {
            _logger.LogWarning("VNPay IPN rejected because its signature is invalid.");
            return new VnPayIpnResponse("97", "Invalid Checksum");
        }

        try
        {
            var result = await ProcessVnPayNotificationAsync(query, isPosStaff: false, cancellationToken);
            return result.AlreadyProcessed
                ? new VnPayIpnResponse("02", "Order already confirmed")
                : new VnPayIpnResponse("00", "Confirm Success");
        }
        catch (KeyNotFoundException)
        {
            return new VnPayIpnResponse("01", "Order not found");
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("VNPay amount", StringComparison.Ordinal))
        {
            return new VnPayIpnResponse("04", "Invalid amount");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling VNPay IPN.");
            return new VnPayIpnResponse("99", "Unknown error");
        }
    }

    private async Task<VnPayNotificationResult> ProcessVnPayNotificationAsync(
        IReadOnlyDictionary<string, string> query,
        bool isPosStaff,
        CancellationToken cancellationToken)
    {
        var paymentIdText = GetValue(query, "vnp_txnref");
        if (string.IsNullOrWhiteSpace(paymentIdText) || !Guid.TryParse(paymentIdText, out var paymentId))
        {
            throw new InvalidOperationException("Invalid VNPay transaction reference.");
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async ct =>
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, ct);
            if (payment == null) throw new KeyNotFoundException("Payment not found.");

            var booking = payment.Booking
                ?? throw new InvalidOperationException("Booking not found for this payment.");

            EnsureVnPayAmountMatchesPayment(query, payment);

            if (payment.Status != PaymentPending)
            {
                return new VnPayNotificationResult(
                    ToResponse(payment, booking, redirectUrl: BuildFrontendReturnUrl(payment, isPosStaff)),
                    AlreadyProcessed: true);
            }

            payment.GatewayTxnId = GetValue(query, "vnp_transactionno");
            payment.IpnSignature = GetValue(query, "vnp_securehash");

            if (IsVnPaySuccess(query))
            {
                if (IsExpired(booking))
                {
                    MarkPaymentFailed(payment, booking);
                    ExpireBooking(booking);
                }
                else
                {
                    EnsurePendingBookingCanBePaid(booking);
                    MarkPaymentSuccess(payment, booking);
                    if (booking.PromotionId.HasValue)
                        await _promotionService.RecordUsageAsync(booking.Id, booking.CustomerId, booking.PromotionId.Value, ct);
                    await _qrTicketService.GenerateTicketsForBookingAsync(booking, ct);
                }
            }
            else
            {
                MarkPaymentFailed(payment, booking);
            }

            _paymentRepository.Update(payment);
            _bookingRepository.Update(booking);
            return new VnPayNotificationResult(
                ToResponse(payment, booking, redirectUrl: BuildFrontendReturnUrl(payment, isPosStaff)),
                AlreadyProcessed: false);
        }, cancellationToken);
    }

    public string BuildVnPayPaymentUrl(Payment payment, Booking booking, bool isPosStaff = false)
    {
        _vnPayOptions.EnsureConfigured();

        // Refund requests must reuse this exact original gateway-request timestamp.
        // Do not regenerate it when a pending payment URL is requested again.
        var now = (payment.GatewayRequestAt ?? payment.CreatedAt).AddHours(7);
        var expireDate = booking.ExpiresAt.AddHours(7);
        var returnUrl = isPosStaff ? _vnPayOptions.StaffBookingReturnUrl : _vnPayOptions.ReturnUrl;
        var orderInfo = isPosStaff
            ? $"Thanh toan booking {booking.BookingRef} - POS quay"
            : $"Thanh toan booking {booking.BookingRef}";

        var parameters = new SortedDictionary<string, string>(StringComparer.Ordinal)
        {
            ["vnp_Version"] = _vnPayOptions.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _vnPayOptions.TmnCode,
            ["vnp_Amount"] = ((long)(payment.Amount * 100)).ToString(CultureInfo.InvariantCulture),
            ["vnp_CreateDate"] = now.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_CurrCode"] = "VND",
            ["vnp_ExpireDate"] = expireDate.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture),
            ["vnp_IpAddr"] = ResolveClientIpAddress(),
            ["vnp_Locale"] = _vnPayOptions.Locale,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_OrderType"] = _vnPayOptions.OrderType,
            ["vnp_ReturnUrl"] = returnUrl,
            ["vnp_TxnRef"] = payment.Id.ToString("N")
        };

        var query = BuildQueryString(parameters);
        var secureHash = ComputeHmacSha512(_vnPayOptions.HashSecret, query);
        return $"{_vnPayOptions.PaymentUrl}?{query}&vnp_SecureHash={secureHash}";
    }

    private string ResolveClientIpAddress()
    {
        var context = _httpContextAccessor?.HttpContext;
        if (context == null)
        {
            return FallbackIpAddress;
        }

        if (TryGetForwardedIp(context, out var forwardedIp) && !string.IsNullOrWhiteSpace(forwardedIp))
        {
            return forwardedIp;
        }

        var remoteIp = context.Connection?.RemoteIpAddress;
        if (remoteIp == null || IPAddress.IsLoopback(remoteIp))
        {
            return FallbackIpAddress;
        }

        if (remoteIp.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var mapped = remoteIp.MapToIPv4();
            return mapped.ToString();
        }

        return remoteIp.ToString();
    }

    private static bool TryGetForwardedIp(HttpContext context, out string ipAddress)
    {
        ipAddress = string.Empty;

        if (!context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedForValues))
        {
            return false;
        }

        var raw = forwardedForValues.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        ipAddress = raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;
        return !string.IsNullOrWhiteSpace(ipAddress);
    }

    private bool IsValidVnPaySignature(IReadOnlyDictionary<string, string> query)
    {
        _vnPayOptions.EnsureConfigured();

        var secureHash = GetValue(query, "vnp_securehash");

        if (string.IsNullOrWhiteSpace(secureHash))
        {
            return false;
        }

        var signedParameters = query
            .Where(parameter =>
                // QUAN TRỌNG: Chỉ lấy các tham số của VNPAY
                parameter.Key.StartsWith("vnp_", StringComparison.OrdinalIgnoreCase) &&
                !parameter.Key.Equals("vnp_securehash", StringComparison.OrdinalIgnoreCase) &&
                !parameter.Key.Equals("vnp_securehashtype", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(parameter => parameter.Key, parameter => parameter.Value);

        var signData = BuildQueryString(signedParameters);
        var expectedHash = ComputeHmacSha512(_vnPayOptions.HashSecret, signData);

        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expectedHash.ToUpperInvariant()),
            Encoding.UTF8.GetBytes(secureHash.ToUpperInvariant()));

        if (!isValid)
        {
            _logger.LogWarning("VNPay signature mismatch for payment callback.");
        }

        return isValid;
    }

    private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
    {
        return string.Join(
            "&",
            parameters
                .Where(parameter => !string.IsNullOrEmpty(parameter.Value))
                .OrderBy(parameter => parameter.Key, StringComparer.Ordinal)
                .Select(parameter =>
                    $"{parameter.Key}={VnPayEncode(parameter.Value)}"));
    }

    private static string VnPayEncode(string value)
    {
        return Uri.EscapeDataString(value).Replace("%20", "+");
    }

    private static string ComputeHmacSha256(string secretKey, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA256(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLowerInvariant();
    }

    private static string ComputeHmacSha512(string secretKey, string data)
    {
        var keyBytes = Encoding.UTF8.GetBytes(secretKey);
        var dataBytes = Encoding.UTF8.GetBytes(data);
        using var hmac = new HMACSHA512(keyBytes);
        return Convert.ToHexString(hmac.ComputeHash(dataBytes)).ToLowerInvariant();
    }

    private static void EnsurePendingBookingCanBePaid(Booking booking)
    {
        if (booking.Status != BookingPending)
        {
            throw new InvalidOperationException(
                $"Booking is not payable because it is {booking.Status}.");
        }

        if (IsExpired(booking))
        {
            throw new InvalidOperationException("Booking has expired.");
        }
    }

    private static void EnsureVnPayAmountMatchesPayment(
    IReadOnlyDictionary<string, string> query,
    Payment payment)
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

    private static bool IsExpired(Booking booking)
    {
        return booking.ExpiresAt <= DateTime.UtcNow;
    }

    private static void ExpireBooking(Booking booking)
    {
        booking.Status = BookingExpired;
        foreach (var bookingSeat in booking.BookingSeatBookings)
        {
            bookingSeat.SeatStatus = SeatReleased;
        }
        CancelPendingFnbOrders(booking);
    }

    private static void MarkPaymentSuccess(Payment payment, Booking booking)
    {
        payment.Status = PaymentPaid;
        payment.PaidAt = DateTime.UtcNow;

        booking.Status = BookingConfirmed;
        foreach (var bookingSeat in booking.BookingSeatBookings)
        {
            if (bookingSeat.SeatStatus == SeatHeld)
            {
                bookingSeat.SeatStatus = SeatBooked;
            }
        }
        foreach (var fnbOrder in booking.FnbOrders.Where(order => order.OrderStatus == FnbPending))
        {
            fnbOrder.OrderStatus = FnbConfirmed;
            fnbOrder.PaymentMethod = payment.Gateway;
        }
    }

    private static void MarkPaymentFailed(Payment payment, Booking booking)
    {
        payment.Status = PaymentFailed;

        if (booking.Status == BookingPending)
        {
            booking.Status = BookingCancelled;
            booking.CancelledAt = DateTime.UtcNow;
            foreach (var bookingSeat in booking.BookingSeatBookings)
            {
                bookingSeat.SeatStatus = SeatReleased;
            }
            CancelPendingFnbOrders(booking);
        }
    }

    private PaymentResponseDto ToResponse(
        Payment payment,
        Booking booking,
        string? paymentUrl = null,
        string? redirectUrl = null)
    {
        return new PaymentResponseDto
        {
            PaymentId = payment.Id,
            BookingId = booking.Id,
            Gateway = payment.Gateway,
            Amount = payment.Amount,
            PaymentStatus = payment.Status,
            BookingStatus = booking.Status,
            GatewayTxnId = payment.GatewayTxnId,
            PaymentUrl = paymentUrl,
            RedirectUrl = redirectUrl
        };
    }

    private string BuildFrontendReturnUrl(Payment payment, bool isPosStaff = false)
    {
        var baseUrl = isPosStaff ? _vnPayOptions.StaffBookingResultUrl : _vnPayOptions.FrontendReturnUrl;
        var separator = baseUrl.Contains('?') ? "&" : "?";
        return $"{baseUrl}{separator}paymentId={payment.Id}&status={payment.Status}";
    }

    private static string NormalizeGateway(string? gateway)
    {
        return string.IsNullOrWhiteSpace(gateway)
            ? GatewayVnPay
            : gateway.Trim().ToUpperInvariant();
    }

    private static void CancelPendingFnbOrders(Booking booking)
    {
        foreach (var fnbOrder in booking.FnbOrders.Where(order => order.OrderStatus == FnbPending))
        {
            fnbOrder.OrderStatus = FnbCancelled;
        }
    }

    private static string Hash(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()))).ToLowerInvariant();

    private static string? GetValue(IReadOnlyDictionary<string, string> query, string key)
    {
        // Lấy value mà không phân biệt chữ hoa chữ thường của key
        var match = query.FirstOrDefault(x => x.Key.Equals(key, StringComparison.OrdinalIgnoreCase));
        return match.Key != null ? match.Value : null;
    }

    private sealed record VnPayNotificationResult(PaymentResponseDto Response, bool AlreadyProcessed);

    private sealed class VnPayOptions
    {
        public string TmnCode { get; private init; } = string.Empty;
        public string HashSecret { get; private init; } = string.Empty;
        public string PaymentUrl { get; private init; } = string.Empty;
        public string ReturnUrl { get; private init; } = string.Empty;
        public string FrontendReturnUrl { get; private init; } = string.Empty;
        public string StaffBookingReturnUrl { get; private init; } = string.Empty;
        public string StaffBookingResultUrl { get; private init; } = string.Empty;
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
                StaffBookingReturnUrl = (section["StaffBookingReturnUrl"] ?? string.Empty).Trim(),
                StaffBookingResultUrl = (section["StaffBookingResultUrl"] ?? string.Empty).Trim(),
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

    /// <summary>
    /// Lấy booking + tickets để hiển thị QR cho khách sau khi thanh toán VNPay thành công.
    /// </summary>
    public async Task<BookingCallbackDto?> GetBookingByPaymentIdAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var payment = await _paymentRepository.GetByIdWithBookingAsync(paymentId, cancellationToken);
        if (payment?.Booking == null) return null;

        var booking = payment.Booking;

        // Tính F&B total
        var fnbTotal = booking.FnbOrders
            .Where(o => o.OrderStatus != "CANCELLED")
            .Sum(o => o.TotalAmount);

        // Sinh QR cho từng ticket
        var tickets = new List<TicketCallbackDto>();
        foreach (var ticket in booking.Tickets)
        {
            var seatLabel = ticket.BookingSeat?.Seat?.SeatLabel ?? "?";
            var qrBase64 = _qrTicketService.RenderQrImageBase64(ticket.QrCode, "PNG");
            tickets.Add(new TicketCallbackDto
            {
                TicketId = ticket.Id,
                SeatLabel = seatLabel,
                QrImageBase64 = qrBase64,
                Token = ticket.QrCode
            });
        }

        // F&B orders
        var fnbOrders = booking.FnbOrders
            .Where(o => o.OrderStatus != "CANCELLED")
            .Select(o => new FnbOrderCallbackDto
            {
                Id = o.Id,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                Items = o.FnbOrderDetails.Select(d => new FnbOrderItemCallbackDto
                {
                    Id = d.Id,
                    ItemId = d.ItemId,
                    ItemName = d.Item?.Name ?? "?",
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal
                }).ToList()
            }).ToList();

        return new BookingCallbackDto
        {
            BookingId = booking.Id,
            BookingRef = booking.BookingRef,
            MovieTitle = booking.Showtime?.Movie?.Title ?? "?",
            CinemaName = booking.Showtime?.Room?.Cinema?.Name ?? "?",
            RoomName = booking.Showtime?.Room?.Name ?? "?",
            ShowtimeStart = CinemaTime.ToLocal(booking.Showtime?.StartTime ?? DateTime.MinValue),
            ShowtimeEnd = CinemaTime.ToLocal(booking.Showtime?.EndTime ?? DateTime.MinValue),
            SeatLabels = booking.BookingSeatBookings
                .Select(bs => bs.Seat?.SeatLabel ?? "?")
                .ToList(),
            TotalAmount = booking.TotalAmount,
            DiscountAmount = booking.DiscountAmount,
            // booking.FinalAmount đã bao gồm F&B (BookingService.CreateBookingAsync đã cộng).
            // KHÔNG cộng thêm fnbTotal.
            FinalAmount = booking.FinalAmount,
            FnbTotalAmount = fnbTotal,
            Tickets = tickets,
            FnbOrders = fnbOrders
        };
    }
}
