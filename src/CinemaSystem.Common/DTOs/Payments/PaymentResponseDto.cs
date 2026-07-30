using System;

namespace CinemaSystem.Common.DTOs.Payments;

public class PaymentResponseDto
{
    public Guid PaymentId { get; set; }
    public Guid BookingId { get; set; }
    public string Gateway { get; set; } = null!;
    public decimal Amount { get; set; }
    public string PaymentStatus { get; set; } = null!;
    public string BookingStatus { get; set; } = null!;
    public string? GatewayTxnId { get; set; }
    public string? PaymentUrl { get; set; }
    public string? RedirectUrl { get; set; }
}
