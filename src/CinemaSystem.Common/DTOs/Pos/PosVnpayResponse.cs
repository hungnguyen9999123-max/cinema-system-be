using CinemaSystem.Common.DTOs.Bookings;
using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Pos;

public sealed record PosVnpayResponse
{
    public Guid BookingId { get; init; }
    public string BookingRef { get; init; } = null!;
    public string MovieTitle { get; init; } = null!;
    public string CinemaName { get; init; } = null!;
    public string RoomName { get; init; } = null!;
    public DateTime ShowtimeStart { get; init; }
    public DateTime ShowtimeEnd { get; init; }
    public IReadOnlyList<string> SeatLabels { get; init; } = [];
    public string PaymentGateway { get; init; } = "VNPAY";
    public Guid PaymentId { get; init; }
    public string PaymentStatus { get; init; } = "PENDING";
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public string PaymentUrl { get; init; } = null!;
    public DateTime ExpiresAt { get; init; }

    /// <summary>
    /// F&amp;B orders attached to this booking. Null when no F&amp;B items
    /// were included at the counter. Line items are returned in full so the
    /// POS UI can render the receipt without an extra API call.
    /// </summary>
    public IReadOnlyList<FnbOrderSummaryDto>? FnbOrders { get; init; }

    /// <summary>
    /// Sum of all attached F&amp;B orders' <c>TotalAmount</c>. Zero when the
    /// booking did not include any F&amp;B items.
    /// </summary>
    public decimal FnbTotalAmount { get; init; }
}