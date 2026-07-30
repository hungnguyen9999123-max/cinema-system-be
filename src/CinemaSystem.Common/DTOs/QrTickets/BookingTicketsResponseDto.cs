namespace CinemaSystem.Common.DTOs.QrTickets;

public sealed record BookingTicketsResponseDto
{
    public Guid BookingId { get; init; }
    public string BookingRef { get; init; } = null!;
    public IReadOnlyList<GenerateQrResponseDto> Tickets { get; init; } = [];
}
