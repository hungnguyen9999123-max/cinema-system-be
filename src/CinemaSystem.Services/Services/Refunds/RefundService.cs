using System.Security.Cryptography;
using System.Text;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Refunds;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CinemaSystem.Services.Services.Refunds;

public sealed class RefundService(
    IRefundRepository refundRepository,
    IWalletRepository walletRepository,
    IUnitOfWork unitOfWork,
    IRefundNotificationService notificationService,
    IConfiguration configuration,
    ILogger<RefundService> logger) : IRefundService
{
    private const string Confirmed = "CONFIRMED";
    private const string RefundProcessing = "REFUND_PROCESSING";
    private const string Refunded = "REFUNDED";
    private readonly RefundSettings settings = RefundSettings.FromConfiguration(configuration);

    private static readonly HashSet<string> CustomerReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "PLAN_CHANGED", "SCHEDULE_CONFLICT", "OTHER"
    };

    private static readonly HashSet<string> RejectionReasons = new(StringComparer.OrdinalIgnoreCase)
    {
        "POLICY_CUTOFF", "PAYMENT_NOT_ELIGIBLE", "TICKET_SCANNED", "FNB_FULFILLED", "FRAUD_SUSPECTED", "OTHER"
    };

    public RefundPolicyDto GetPolicy() => new()
    {
        CutoffMinutes = settings.CutoffMinutes,
        MaxHoursAfterPurchase = settings.MaxHoursAfterPurchase,
        FullRefundOnly = true,
        SupportedGateways = ["VNPAY", "WALLET"],
        ReasonCodes = CustomerReasons.OrderBy(value => value).ToArray(),
        SettlementMessage = "Yêu cầu đủ điều kiện sẽ được hoàn tiền ngay vào ví CINE-MAX. Bạn có thể tạo yêu cầu rút tiền từ ví sau đó."
    };

    public async Task<RefundResponseDto> CreateAsync(Guid customerId, string idempotencyKey, CreateRefundRequestDto request, CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty) throw new UnauthorizedAccessException("Bạn cần đăng nhập để gửi yêu cầu hoàn tiền.");
        var normalizedReason = NormalizeCustomerReason(request.ReasonCode);
        var keyHash = Hash(idempotencyKey);

        Refund refund;
        Payment payment;
        try
        {
            (refund, payment) = await unitOfWork.ExecuteInTransactionAsync<(Refund, Payment)>(async ct =>
            {
                var existing = await refundRepository.GetByIdempotencyKeyAsync(customerId, keyHash, ct);
                if (existing is not null)
                    throw new BusinessConflictException("Yêu cầu hoàn tiền đã tồn tại.");

                var periodStart = DateTime.UtcNow.AddMinutes(-settings.CustomerRequestWindowMinutes);
                if (await refundRepository.CountRequestsByCustomerSinceAsync(customerId, periodStart, ct) >= settings.CustomerRequestsPerWindow)
                    throw new TooManyRequestsException($"Bạn chỉ có thể gửi tối đa {settings.CustomerRequestsPerWindow} yêu cầu hoàn tiền trong {settings.CustomerRequestWindowMinutes} phút.", settings.CustomerRequestWindowMinutes * 60);

                var paymentRef = await refundRepository.GetPaymentForRefundAsync(request.BookingId, ct)
                    ?? throw new BusinessConflictException("Không tìm thấy thanh toán thành công cho đơn đặt vé này.");
                EnsureCustomerEligibility(paymentRef, customerId, allowRefundProcessing: false);

                var active = await refundRepository.GetActiveForPaymentAsync(paymentRef.Id, ct);
                if (active is not null) throw new BusinessConflictException("Đơn này đã có yêu cầu hoàn tiền đang được xử lý.");

                var now = DateTime.UtcNow;
                var refundRef = new Refund
                {
                    Id = Guid.NewGuid(),
                    PaymentId = paymentRef.Id,
                    Payment = paymentRef,
                    RequestedBy = customerId,
                    RefundAmount = paymentRef.Amount,
                    Status = RefundStatus.Requested,
                    ReasonCode = normalizedReason,
                    Reason = normalizedReason,
                    IdempotencyKeyHash = keyHash,
                    RequestedAt = now,
                    UpdatedAt = now
                };
                await refundRepository.AddAsync(refundRef, ct);
                await CreditToWalletAsync(refundRef, paymentRef, processedBy: null, decisionReason: "AUTO_APPROVED", cancellationToken: ct);
                return (refundRef, paymentRef);
            }, cancellationToken);
        }
        catch (BusinessConflictException) { throw; }
        catch (TooManyRequestsException) { throw; }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            logger.LogError(ex, "Refund creation failed for customer {CustomerId}", customerId);
            throw;
        }

        await NotifySafelyAsync(refund, cancellationToken);
        return ToResponse(refund, payment.Booking);
    }

    public async Task<RefundPagedResultDto> GetMineAsync(Guid customerId, RefundListQueryRequest request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await refundRepository.GetByCustomerAsync(customerId, request.Status, request.Page, request.PageSize, cancellationToken);
        return ToPage(items, total, request);
    }

    public async Task<RefundPagedResultDto> GetForOperationsAsync(RefundListQueryRequest request, CancellationToken cancellationToken = default)
    {
        var (items, total) = await refundRepository.GetForOperationsAsync(request.Status, request.Page, request.PageSize, cancellationToken);
        return ToPage(items, total, request);
    }

    public async Task<RefundResponseDto> ApproveAsync(Guid refundId, Guid managerId, string? note, CancellationToken cancellationToken = default)
    {
        Refund refund;
        Payment payment;
        try
        {
            (refund, payment) = await unitOfWork.ExecuteInTransactionAsync<(Refund, Payment)>(async ct =>
            {
                var refundRef = await GetRefundAsync(refundId, ct);
                if (!CanCreditToWallet(refundRef.Status))
                    throw new BusinessConflictException("Yêu cầu này không thể hoàn vào ví.");

                var bookingId = refundRef.Payment.BookingId
                    ?? throw new BusinessConflictException("Thanh toán này không thuộc booking nên không thể hoàn vé.");
                var paymentRef = await refundRepository.GetPaymentForRefundAsync(bookingId, ct)
                    ?? throw new BusinessConflictException("Thanh toán gốc không còn đủ điều kiện hoàn tiền.");
                EnsureOperationalEligibility(paymentRef, allowRefundProcessing: true);
                await CreditToWalletAsync(refundRef, paymentRef, managerId, NormalizeNote(note), ct);
                return (refundRef, paymentRef);
            }, cancellationToken);
        }
        catch (BusinessConflictException) { throw; }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            logger.LogError(ex, "Refund approval failed for {RefundId}", refundId);
            throw;
        }

        await NotifySafelyAsync(refund, cancellationToken);
        return ToResponse(refund, payment.Booking);
    }

    public async Task<RefundResponseDto> RejectAsync(Guid refundId, Guid managerId, RefundDecisionRequestDto request, CancellationToken cancellationToken = default)
    {
        var reason = NormalizeRejectionReason(request.ReasonCode);
        Refund refund;
        try
        {
            refund = await unitOfWork.ExecuteInTransactionAsync<Refund>(async ct =>
            {
                var refundRef = await GetRefundAsync(refundId, ct);
                if (refundRef.Status != RefundStatus.Requested)
                    throw new BusinessConflictException("Chỉ yêu cầu đang chờ mới có thể bị từ chối.");

                var now = DateTime.UtcNow;
                refundRef.Status = RefundStatus.Rejected;
                refundRef.ProcessedBy = managerId;
                refundRef.DecisionReason = reason;
                refundRef.FailureMessage = NormalizeNote(request.InternalNote);
                refundRef.DecidedAt = now;
                refundRef.UpdatedAt = now;
                refundRepository.Update(refundRef);
                await walletRepository.SaveChangesAsync(ct);
                return refundRef;
            }, cancellationToken);
        }
        catch (BusinessConflictException) { throw; }
        catch (Exception ex) when (ex is not UnauthorizedAccessException)
        {
            logger.LogError(ex, "Refund rejection failed for {RefundId}", refundId);
            throw;
        }

        await NotifySafelyAsync(refund, cancellationToken);
        return ToResponse(refund);
    }

    private async Task<Refund> GetRefundAsync(Guid refundId, CancellationToken cancellationToken) =>
        await refundRepository.GetByIdAsync(refundId, cancellationToken)
            ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu hoàn tiền.");

    private async Task CreditToWalletAsync(
        Refund refund,
        Payment payment,
        Guid? processedBy,
        string? decisionReason,
        CancellationToken cancellationToken)
    {
        if (!refund.RequestedBy.HasValue)
            throw new BusinessConflictException("Không xác định được chủ ví nhận tiền hoàn.");
        if (await walletRepository.HasRefundCreditAsync(refund.Id, cancellationToken))
            throw new BusinessConflictException("Yêu cầu này đã được ghi có vào ví.");

        var now = DateTime.UtcNow;
        var wallet = await walletRepository.GetOrCreateAsync(refund.RequestedBy.Value, cancellationToken);
        wallet.Balance += refund.RefundAmount;
        wallet.UpdatedAt = now;
        await walletRepository.AddTransactionAsync(new WalletTransaction
        {
            Id = Guid.NewGuid(),
            WalletId = wallet.Id,
            Wallet = wallet,
            RefundId = refund.Id,
            Refund = refund,
            Type = WalletTransactionType.RefundCredit,
            Amount = refund.RefundAmount,
            BalanceAfter = wallet.Balance,
            Description = $"Hoàn tiền đơn vé {payment.Booking.BookingRef} vào ví",
            CreatedAt = now
        }, cancellationToken);

        refund.Status = RefundStatus.Succeeded;
        refund.ProcessedBy = processedBy;
        refund.DecisionReason = decisionReason;
        refund.DecidedAt = now;
        refund.ProcessedAt = now;
        refund.UpdatedAt = now;
        refund.NextReconciliationAt = null;
        refund.FailureCode = null;
        refund.FailureMessage = null;
        refund.GatewayRefundId = null;
        payment.Booking.Status = Refunded;
        payment.Booking.CancelledAt = now;
        foreach (var ticket in payment.Booking.Tickets) ticket.Status = TicketStatus.Cancelled;
        foreach (var bookingSeat in payment.Booking.BookingSeatBookings)
        {
            bookingSeat.SeatStatus = "RELEASED";
        }

        refundRepository.Update(refund);
        walletRepository.Update(wallet);
        await walletRepository.SaveChangesAsync(cancellationToken);
    }

    private async Task NotifySafelyAsync(Refund refund, CancellationToken cancellationToken)
    {
        try { await notificationService.NotifyCustomerAsync(refund, cancellationToken); }
        catch (Exception exception) { logger.LogError(exception, "Could not persist customer refund notification for {RefundId}.", refund.Id); }
    }

    private void EnsureCustomerEligibility(Payment payment, Guid customerId, bool allowRefundProcessing)
    {
        if (payment.Booking.CustomerId != customerId) throw new ForbiddenAccessException("Bạn không có quyền hoàn tiền cho đơn này.");
        EnsureOperationalEligibility(payment, allowRefundProcessing);
    }

    private void EnsureOperationalEligibility(Payment payment, bool allowRefundProcessing)
    {
        if (payment.Booking.Status != Confirmed && !(allowRefundProcessing && payment.Booking.Status == RefundProcessing))
            throw new BusinessConflictException("Đơn đặt vé không ở trạng thái có thể hoàn tiền.");
        if (payment.Booking.Showtime.StartTime <= DateTime.UtcNow.AddMinutes(settings.CutoffMinutes))
            throw new BusinessConflictException($"Chỉ có thể hoàn tiền trước giờ chiếu ít nhất {settings.CutoffMinutes} phút.");
        var purchasedAt = payment.PaidAt ?? payment.Booking.BookedAt;
        if (DateTime.UtcNow > purchasedAt.AddHours(settings.MaxHoursAfterPurchase))
            throw new BusinessConflictException($"Chỉ có thể hoàn tiền trong vòng {settings.MaxHoursAfterPurchase} giờ sau khi mua vé.");
        if (payment.Booking.Tickets.Any(ticket => ticket.Status.Equals(TicketStatus.Scanned, StringComparison.OrdinalIgnoreCase)))
            throw new BusinessConflictException("Không thể hoàn tiền khi vé đã được quét.");
        if (payment.Booking.FnbOrders.Any(order => !order.OrderStatus.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase)))
            throw new BusinessConflictException("Không thể hoàn tiền khi đơn bắp nước đã được xử lý.");
    }

    private static bool CanCreditToWallet(string status) =>
        status is RefundStatus.Requested or RefundStatus.Processing or RefundStatus.ReconciliationRequired;

    private static string NormalizeCustomerReason(string? reason)
    {
        var value = (reason ?? string.Empty).Trim().ToUpperInvariant();
        if (!CustomerReasons.Contains(value)) throw new InvalidOperationException("Lý do hoàn tiền không hợp lệ.");
        return value;
    }

    private static string NormalizeRejectionReason(string? reason)
    {
        var value = (reason ?? string.Empty).Trim().ToUpperInvariant();
        if (!RejectionReasons.Contains(value)) throw new InvalidOperationException("Lý do từ chối không hợp lệ.");
        return value;
    }

    private static string? NormalizeNote(string? note) => string.IsNullOrWhiteSpace(note) ? null : note.Trim()[..Math.Min(note.Trim().Length, 1000)];
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()))).ToLowerInvariant();

    private static RefundPagedResultDto ToPage(IReadOnlyList<Refund> refunds, int totalCount, RefundListQueryRequest request) => new()
    {
        Items = refunds.Select(ToResponse).ToArray(),
        Page = Math.Max(1, request.Page),
        PageSize = Math.Clamp(request.PageSize, 1, 100),
        TotalCount = totalCount
    };

    private static RefundResponseDto ToResponse(Refund refund) => ToResponse(refund, refund.Payment?.Booking);
    private static RefundResponseDto ToResponse(Refund refund, Booking? booking) => new()
    {
        RefundId = refund.Id,
        BookingId = booking?.Id ?? refund.Payment?.BookingId ?? Guid.Empty,
        BookingRef = booking?.BookingRef ?? refund.Payment?.Booking?.BookingRef ?? string.Empty,
        Amount = refund.RefundAmount,
        Status = refund.Status,
        ReasonCode = refund.ReasonCode ?? string.Empty,
        CustomerMessage = CustomerMessage(refund),
        RequestedAt = refund.RequestedAt,
        ProcessedAt = refund.ProcessedAt
    };

    private static string CustomerMessage(Refund refund) => refund.Status switch
    {
        RefundStatus.Requested => "Yêu cầu hoàn tiền đang được xử lý.",
        RefundStatus.Succeeded => "Tiền vé đã được hoàn tự động vào ví CINE-MAX của bạn.",
        RefundStatus.Rejected => RejectionCustomerMessage(refund.DecisionReason),
        _ => "Yêu cầu hoàn tiền đang được chuyển sang ví CINE-MAX."
    };

    private static string RejectionCustomerMessage(string? reasonCode) => (reasonCode ?? string.Empty).ToUpperInvariant() switch
    {
        "POLICY_CUTOFF" => "Yêu cầu hoàn tiền bị từ chối vì đã quá thời hạn hoàn tiền.",
        "PAYMENT_NOT_ELIGIBLE" => "Yêu cầu hoàn tiền bị từ chối vì giao dịch không đủ điều kiện.",
        "TICKET_SCANNED" => "Yêu cầu hoàn tiền bị từ chối vì vé đã được quét.",
        "FNB_FULFILLED" => "Yêu cầu hoàn tiền bị từ chối vì đơn bắp nước đã được xử lý.",
        "FRAUD_SUSPECTED" => "Yêu cầu hoàn tiền cần được xác minh thêm trước khi có thể xử lý.",
        _ => "Yêu cầu hoàn tiền bị từ chối."
    };

    private sealed class RefundSettings
    {
        public int CutoffMinutes { get; private init; } = 120;
        public int MaxHoursAfterPurchase { get; private init; } = 12;
        public int CustomerRequestsPerWindow { get; private init; } = 3;
        public int CustomerRequestWindowMinutes { get; private init; } = 15;

        public static RefundSettings FromConfiguration(IConfiguration configuration) => new()
        {
            CutoffMinutes = Clamp(configuration.GetValue<int?>("Refunds:CutoffMinutes") ?? 120, 1, 240),
            MaxHoursAfterPurchase = Clamp(configuration.GetValue<int?>("Refunds:MaxHoursAfterPurchase") ?? 12, 1, 168),
            CustomerRequestsPerWindow = Clamp(configuration.GetValue<int?>("Refunds:CustomerRequestsPer15Minutes") ?? 3, 1, 20)
        };

        private static int Clamp(int value, int min, int max) => Math.Clamp(value, min, max);
    }
}
