using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Bookings;

public sealed class CreateBookingResponseDto
{
    public Guid BookingId { get; set; }
    public string BookingRef { get; set; } = null!;
    public string BookingStatus { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal FinalAmount { get; set; }

    public Guid? PaymentId { get; set; }
    public string? PaymentGateway { get; set; }
    public string? PaymentStatus { get; set; }

    public string? PaymentUrl { get; set; }

    public IReadOnlyList<BookingTicketDto> Tickets { get; set; } = [];

    /// <summary>
    /// F&B orders included in this booking.
    /// </summary>
    public IReadOnlyList<FnbOrderSummaryDto> FnbOrders { get; set; } = [];
}
