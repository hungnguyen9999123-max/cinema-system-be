using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Bookings;

public sealed class MyBookingListItemDto
{
    public Guid BookingId { get; init; }
    public string BookingRef { get; init; } = null!;
    public Guid ShowtimeId { get; init; }
    public string MovieTitle { get; init; } = null!;
    public string? PosterUrl { get; init; }
    public string CinemaName { get; init; } = null!;
    public string RoomName { get; init; } = null!;
    public DateTime ShowtimeStart { get; init; }
    public DateTime ShowtimeEnd { get; init; }
    public IReadOnlyList<string> SeatLabels { get; init; } = [];
    public int TicketCount { get; init; }
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public Guid? PromotionId { get; init; }
    public DateTime BookedAt { get; init; }
    public DateTime? CancelledAt { get; init; }
    public string Status { get; init; } = null!;
    public IReadOnlyList<FnbOrderSummaryDto> FnbOrders { get; init; } = [];
    public bool HasFnbOrders => FnbOrders.Count > 0;
    public int FnbItemTotalCount => FnbOrders.SelectMany(o => o.Items).Sum(i => i.Quantity);
}