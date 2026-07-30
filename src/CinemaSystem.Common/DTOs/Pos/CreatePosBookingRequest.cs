using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Pos;

public sealed class CreatePosBookingRequest
{
    public Guid ShowtimeId { get; set; }
    public List<Guid> SeatIds { get; set; } = [];
    public Guid AudienceTypeId { get; set; }
    public string? PromotionCode { get; set; }
    public string Gateway { get; set; } = "CASH";
    public PosCustomerInfo? CustomerInfo { get; set; }

    /// <summary>
    /// Optional F&amp;B items attached to this POS booking. When supplied,
    /// a separate <c>fnb_order</c> is created against the new booking and the
    /// booking's total + payment amount are bumped by the F&amp;B subtotal
    /// so the customer pays both ticket + F&amp;B in a single transaction.
    /// </summary>
    public List<PosFnbItemRequest> FnbItems { get; set; } = [];
}

public sealed class PosFnbItemRequest
{
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
}

public sealed class PosCustomerInfo
{
    /// <summary>
    /// Optional existing user id when the POS customer already has a
    /// member account. Null = walk-in sale (BookingService will store
    /// the booking under the staff id).
    /// </summary>
    public Guid? CustomerId { get; set; }
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
