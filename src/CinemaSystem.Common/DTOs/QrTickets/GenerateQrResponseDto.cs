namespace CinemaSystem.Common.DTOs.QrTickets;

public sealed record GenerateQrResponseDto
{
    public Guid TicketId { get; init; }
    public Guid BookingId { get; init; }
    public string SeatLabel { get; init; } = null!;
    public string Token { get; init; } = null!;
    public string QrImageBase64 { get; init; } = null!;
    public DateTime ExpiredAt { get; init; }
    public string Status { get; init; } = null!;
}
