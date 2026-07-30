using System.ComponentModel.DataAnnotations;

namespace CinemaSystem.Common.DTOs.Rooms;

public sealed record RoomResponse(
    Guid Id,
    Guid CinemaId,
    string CinemaName,
    string Name,
    string RoomType,
    int TotalCapacity,
    string Status,
    int SeatCount,
    DateTime CreatedAt);

public sealed class CreateRoomRequest
{
    [Required]
    public Guid CinemaId { get; init; }

    [Required]
    [StringLength(50)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [AllowedValues("STANDARD", "VIP", "IMAX", "4DX")]
    public string RoomType { get; init; } = "STANDARD";

    [Range(1, 1000)]
    public int TotalCapacity { get; init; }
}

public sealed class UpdateRoomRequest
{
    [Required]
    [StringLength(50)]
    public string Name { get; init; } = string.Empty;

    [Required]
    [AllowedValues("STANDARD", "VIP", "IMAX", "4DX")]
    public string RoomType { get; init; } = "STANDARD";

    [Range(1, 1000)]
    public int TotalCapacity { get; init; }

    [Required]
    [AllowedValues("ACTIVE", "INACTIVE", "MAINTENANCE")]
    public string Status { get; init; } = "ACTIVE";
}

public sealed class RoomSearchRequest
{
    public Guid? CinemaId { get; init; }

    [StringLength(20)]
    public string? RoomType { get; init; }

    [StringLength(20)]
    public string? Status { get; init; }

    [StringLength(100)]
    public string? Keyword { get; init; }

    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, 100)]
    public int PageSize { get; init; } = 20;
}

public sealed record SeatResponse(
    Guid Id,
    Guid RoomId,
    string SeatLabel,
    string RowLetter,
    byte ColNumber,
    string SeatTypeName,
    decimal SeatMultiplier,
    string Status);

public sealed class CreateSeatRequest
{
    [Required]
    [StringLength(1, MinimumLength = 1)]
    public string RowLetter { get; init; } = string.Empty;

    [Range(1, byte.MaxValue)]
    public byte ColNumber { get; init; }

    [Required]
    [StringLength(50)]
    public string SeatTypeName { get; init; } = string.Empty;
}

public sealed class UpdateSeatRequest
{
    [StringLength(50)]
    public string? SeatTypeName { get; init; }

    [Required]
    [AllowedValues("ACTIVE", "DISABLED")]
    public string Status { get; init; } = "ACTIVE";
}

public sealed class SeatRangeOverride
{
    [Required]
    [StringLength(1, MinimumLength = 1)]
    public string RowFrom { get; init; } = string.Empty;

    [Required]
    [StringLength(1, MinimumLength = 1)]
    public string RowTo { get; init; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ColFrom { get; init; }

    [Range(1, int.MaxValue)]
    public int ColTo { get; init; }

    [Required]
    [StringLength(50)]
    public string SeatTypeName { get; init; } = string.Empty;

    public string? Status { get; init; }
}

public sealed class GenerateSeatLayoutRequest
{
    [Range(1, 26)]
    public int Rows { get; init; }

    [Range(1, 50)]
    public int SeatsPerRow { get; init; }

    [Required]
    [StringLength(50)]
    public string DefaultSeatTypeName { get; init; } = string.Empty;

    public List<SeatRangeOverride>? Overrides { get; init; }

    public bool ReplaceExisting { get; init; }
}

public sealed record SeatRowResponse(string RowLetter, IReadOnlyList<SeatResponse> Seats);

public sealed record SeatLayoutResponse(
    Guid RoomId,
    string RoomName,
    int TotalSeats,
    IReadOnlyList<SeatRowResponse> Rows);
