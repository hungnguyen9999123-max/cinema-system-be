using CinemaSystem.Common.DTOs.Bookings;
using CinemaSystem.Common.DTOs.QrTickets;
using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Pos;

public sealed record PosBookingResponse
{
    public Guid BookingId { get; init; }
    public string BookingRef { get; init; } = null!;
    public string MovieTitle { get; init; } = null!;
    public string CinemaName { get; init; } = null!;
    public string RoomName { get; init; } = null!;
    public DateTime ShowtimeStart { get; init; }
    public DateTime ShowtimeEnd { get; init; }
    public IReadOnlyList<string> SeatLabels { get; init; } = [];
    public string PaymentGateway { get; init; } = null!;
    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }
    public IReadOnlyList<PosTicketItem> Tickets { get; init; } = [];

    /// <summary>
    /// F&amp;B orders attached to this booking (typically one POS sale). Null
    /// when the booking did not include any F&amp;B items. Each entry exposes
    /// the full line-item list so the counter UI can render the receipt
    /// breakdown without an extra round-trip.
    /// </summary>
    public IReadOnlyList<FnbOrderSummaryDto>? FnbOrders { get; init; }

    /// <summary>
    /// Sum of all attached F&amp;B orders' <c>TotalAmount</c>. Zero when no
    /// F&amp;B was included. Useful for the counter UI to show the F&amp;B
    /// subtotal separately from the ticket price.
    /// </summary>
    public decimal FnbTotalAmount { get; init; }

    /// <summary>
    /// True on the first response from POST /api/pos/tickets when the gateway
    /// is CASH. The booking + payment are still PENDING and no QR exists yet;
    /// staff must call POST /api/pos/tickets/{paymentId}/confirm to flip them
    /// to CONFIRMED and receive the QR list. False (or unset) on every other
    /// response path.
    /// </summary>
    public bool IsPendingConfirmation { get; init; }

    /// <summary>
    /// UTC instant when this PENDING booking will be auto-expired by
    /// BookingExpiryBackgroundService. Populated only on the initial CASH
    /// response; the confirm step returns the final response without this.
    /// </summary>
    public DateTime ExpiresAt { get; init; }
}

public sealed record PosTicketItem
{
    public Guid TicketId { get; init; }
    public string SeatLabel { get; init; } = null!;
    public string QrImageBase64 { get; init; } = null!;
    public string Token { get; init; } = null!;
}
