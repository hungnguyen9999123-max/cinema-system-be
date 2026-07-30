using System.ComponentModel.DataAnnotations;

namespace CinemaSystem.Common.DTOs.Showtimes;

public sealed record ShowtimeResponse(
    Guid Id,
    Guid MovieId,
    string MovieTitle,
    Guid RoomId,
    string RoomName,
    Guid CinemaId,
    string CinemaName,
    DateTime StartTime,
    DateTime EndTime,
    string TimeSlot,
    string LanguageType,
    string Status);

public sealed class CreateShowtimeRequest
{
    [Required]
    public Guid MovieId { get; init; }

    [Required]
    public Guid RoomId { get; init; }

    [Required]
    public DateTime StartTime { get; init; }

    [Required]
    [AllowedValues("MORNING", "AFTERNOON", "EVENING", "MIDNIGHT", "PEAK")]
    public string TimeSlot { get; init; } = "MORNING";

    [Required]
    [AllowedValues("DUBBED", "SUBTITLED")]
    public string LanguageType { get; init; } = "SUBTITLED";
}

public sealed class UpdateShowtimeRequest
{
    public Guid? MovieId { get; init; }

    public Guid? RoomId { get; init; }

    public DateTime? StartTime { get; init; }

    public DateTime? EndTime { get; init; }

    public string? TimeSlot { get; init; }

    public string? LanguageType { get; init; }

    public string? Status { get; init; }
}

public sealed class ShowtimeSearchRequest
{
    public Guid? MovieId { get; init; }

    public Guid? CinemaId { get; init; }

    public Guid? RoomId { get; init; }

    public DateTime? DateFrom { get; init; }

    public DateTime? DateTo { get; init; }

    [StringLength(100)]
    public string? Search { get; init; }

    [StringLength(20)]
    public string? Status { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}
