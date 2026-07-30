namespace CinemaSystem.Common.DTOs.QrTickets;

public sealed record VerifyQrResponseDto
{
    public bool IsValid { get; init; }
    public string CheckInStatus { get; init; } = null!;
    public string? Message { get; init; }

    public Guid? TicketId { get; init; }
    public string? BookingRef { get; init; }
    public string? MovieTitle { get; init; }
    public string? CinemaName { get; init; }
    public string? RoomName { get; init; }
    public string? SeatLabel { get; init; }
    public DateTime? ShowtimeStart { get; init; }
    public DateTime? ShowtimeEnd { get; init; }
    public DateTime? ScannedAt { get; init; }
}
