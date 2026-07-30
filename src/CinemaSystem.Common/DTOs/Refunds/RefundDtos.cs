namespace CinemaSystem.Common.DTOs.Refunds;

public sealed class CreateRefundRequestDto
{
    public Guid BookingId { get; set; }
    public string ReasonCode { get; set; } = string.Empty;
}

public sealed class RefundDecisionRequestDto
{
    public string? ReasonCode { get; set; }
    public string? InternalNote { get; set; }
}

public sealed class RefundListQueryRequest
{
    public string? Status { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class RefundResponseDto
{
    public Guid RefundId { get; set; }
    public Guid BookingId { get; set; }
    public string BookingRef { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string ReasonCode { get; set; } = string.Empty;
    public string? CustomerMessage { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? GatewayRefundId { get; set; }
}

public sealed class RefundPagedResultDto
{
    public IReadOnlyList<RefundResponseDto> Items { get; set; } = Array.Empty<RefundResponseDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class RefundPolicyDto
{
    public int CutoffMinutes { get; set; } = 120;
    public int MaxHoursAfterPurchase { get; set; } = 12;
    public bool FullRefundOnly { get; set; } = true;
    public IReadOnlyList<string> SupportedGateways { get; set; } = new[] { "VNPAY", "WALLET" };
    public IReadOnlyList<string> ReasonCodes { get; set; } = new[] { "PLAN_CHANGED", "SCHEDULE_CONFLICT", "OTHER" };
    public string SettlementMessage { get; set; } = "Yêu cầu đủ điều kiện sẽ được hoàn tiền ngay vào ví CINE-MAX. Bạn có thể tạo yêu cầu rút tiền từ ví sau đó.";
}
