using System.ComponentModel.DataAnnotations;
using CinemaSystem.Common.Constants;

namespace CinemaSystem.Common.DTOs.Cinemas;

public sealed record CinemaResponse(
    Guid Id,
    string Name,
    string Address,
    string City,
    string? Phone,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed class CreateCinemaRequest
{
    [Required(ErrorMessage = CinemaMessages.Required)]
    [StringLength(150, ErrorMessage = CinemaMessages.MaxLengthExceeded)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = CinemaMessages.Required)]
    public string Address { get; init; } = string.Empty;

    [Required(ErrorMessage = CinemaMessages.Required)]
    [StringLength(100, ErrorMessage = CinemaMessages.MaxLengthExceeded)]
    public string City { get; init; } = string.Empty;

    [StringLength(20, ErrorMessage = CinemaMessages.MaxLengthExceeded)]
    public string? Phone { get; init; }

    [Required(ErrorMessage = CinemaMessages.Required)]
    [AllowedValues("ACTIVE", "INACTIVE", ErrorMessage = CinemaMessages.InvalidStatus)]
    public string Status { get; init; } = "ACTIVE";
}

public sealed class UpdateCinemaRequest
{
    [Required(ErrorMessage = CinemaMessages.Required)]
    [StringLength(150, ErrorMessage = CinemaMessages.MaxLengthExceeded)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = CinemaMessages.Required)]
    public string Address { get; init; } = string.Empty;

    [Required(ErrorMessage = CinemaMessages.Required)]
    [StringLength(100, ErrorMessage = CinemaMessages.MaxLengthExceeded)]
    public string City { get; init; } = string.Empty;

    [StringLength(20, ErrorMessage = CinemaMessages.MaxLengthExceeded)]
    public string? Phone { get; init; }

    [Required(ErrorMessage = CinemaMessages.Required)]
    [AllowedValues("ACTIVE", "INACTIVE", ErrorMessage = CinemaMessages.InvalidStatus)]
    public string Status { get; init; } = "ACTIVE";
}

public sealed class CinemaSearchRequest
{
    [StringLength(100, ErrorMessage = CinemaMessages.MaxLengthExceeded)]
    public string? Keyword { get; init; }

    [StringLength(100, ErrorMessage = CinemaMessages.MaxLengthExceeded)]
    public string? City { get; init; }

    [StringLength(20, ErrorMessage = CinemaMessages.MaxLengthExceeded)]
    public string? Status { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = CinemaMessages.OutOfRange)]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = CinemaMessages.OutOfRange)]
    public int PageSize { get; init; } = 20;
}
