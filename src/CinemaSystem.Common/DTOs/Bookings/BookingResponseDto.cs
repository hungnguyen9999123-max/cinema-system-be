using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Bookings;

public class BookingResponseDto
{
    public Guid BookingId { get; set; }
    public string BookingRef { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public decimal TotalAmount { get; set; }
    public Guid? PromotionId { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }
    public string Status { get; set; } = null!;
    public Guid? PaymentId { get; set; }
    public string? PaymentGateway { get; set; }
    public string? PaymentStatus { get; set; }
    public IReadOnlyList<FnbOrderSummaryDto> FnbOrders { get; set; } = Array.Empty<FnbOrderSummaryDto>();
}
