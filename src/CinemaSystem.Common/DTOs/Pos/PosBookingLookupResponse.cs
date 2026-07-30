using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Pos;

/// <summary>
/// Returned by GET /api/pos/tickets/by-ref/{bookingRef}. Mirrors the shape
/// of <see cref="PosBookingResponse"/> but exposes more complete booking
/// metadata so POS staff can re-print or look up historical bookings by
/// reference. Seat / ticket detail is populated regardless of booking status;
/// a CANCELLED or EXPIRED booking still lists the seat list but <c>Tickets</c>
/// will be empty.
/// </summary>
public sealed class PosBookingLookupResponse
{
    public Guid BookingId { get; init; }
    public string BookingRef { get; init; } = null!;
    public string BookingStatus { get; init; } = null!;
    public DateTime BookedAt { get; init; }
    public DateTime? ExpiresAt { get; init; }

    public Guid ShowtimeId { get; init; }
    public string MovieTitle { get; init; } = null!;
    public string CinemaName { get; init; } = null!;
    public string RoomName { get; init; } = null!;
    public DateTime ShowtimeStart { get; init; }
    public DateTime ShowtimeEnd { get; init; }

    public IReadOnlyList<string> SeatLabels { get; init; } = [];

    public decimal TotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal FinalAmount { get; init; }

    /// <summary>
    /// All payments attached to this booking (CASH sale usually has one,
    /// online VNPay retry can leave multiple). Includes the matching payment
    /// id the staff typed into the URL path; matches against
    /// <see cref="Payments"/>.Status for the front-end.
    /// </summary>
    public IReadOnlyList<PosLookupPayment> Payments { get; init; } = [];

    /// <summary>
    /// QR tickets. Populated only for CONFIRMED bookings.
    /// </summary>
    public IReadOnlyList<PosTicketItem> Tickets { get; init; } = [];
}

public sealed class PosLookupPayment
{
    public Guid PaymentId { get; init; }
    public string Gateway { get; init; } = null!;
    public string Status { get; init; } = null!;
    public decimal Amount { get; init; }
    public DateTime? PaidAt { get; init; }
}
