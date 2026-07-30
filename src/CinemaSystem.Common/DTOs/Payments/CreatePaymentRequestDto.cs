using System;

namespace CinemaSystem.Common.DTOs.Payments;

public class CreatePaymentRequestDto
{
    public Guid BookingId { get; set; }
    public string Gateway { get; set; } = "VNPAY";
}
