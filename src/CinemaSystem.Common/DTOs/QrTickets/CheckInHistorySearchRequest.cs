namespace CinemaSystem.Common.DTOs.QrTickets;

public sealed record CheckInHistorySearchRequest
{
    public Guid? CinemaId { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}
