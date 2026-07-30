using System;
using System.Collections.Generic;

namespace CinemaSystem.Common.DTOs.Bookings;

public class CreateBookingRequestDto
{
    public Guid ShowtimeId { get; set; }

    public Guid AudienceTypeId { get; set; }

    public List<Guid> SeatIds { get; set; } = new List<Guid>();

    /// <summary>
    /// Deprecated for booking creation. F&amp;B orders must be created through
    /// the F&amp;B order API after the booking exists.
    /// </summary>
    public List<CreateBookingFnbItemDto> FnbItems { get; set; } = new();

    public string? PromotionCode { get; set; }

    /// <summary>
    /// Target customer user id. When null, BookingService uses the caller's id
    /// (online customer flow). POS staff can pass a walk-in customer's id; if
    /// absent the booking falls back to the staff id.
    /// </summary>
    public Guid? CustomerId { get; set; }

    /// <summary>
    /// "VNPAY" or "CASH". Online customer flow always uses VNPAY;
    /// POS can be either. Defaults to VNPAY for backwards compatibility.
    /// </summary>
    public string Gateway { get; set; } = "VNPAY";

    /// <summary>
    /// Walk-in customer information captured by POS staff. Optional,
    /// ignored for online flow.
    /// </summary>
    public PosCustomerInfoDto? PosCustomer { get; set; }

}

public sealed class CreateBookingFnbItemDto
{
    public Guid ItemId { get; set; }
    public int Quantity { get; set; }
}

public sealed class PosCustomerInfoDto
{
    public string? FullName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
}
