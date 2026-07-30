namespace CinemaSystem.Common.DTOs.QrTickets;

public sealed record CheckInHistoryItemDto
{
    public Guid TicketId { get; init; }
    public string BookingRef { get; init; } = null!;
    public string MovieTitle { get; init; } = null!;
    public string CinemaName { get; init; } = null!;
    public string RoomName { get; init; } = null!;
    public string SeatLabel { get; init; } = null!;
    public DateTime ScannedAt { get; init; }
    public string ScannedByName { get; init; } = null!;
}
