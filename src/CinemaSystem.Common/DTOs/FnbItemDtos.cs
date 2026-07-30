using System.ComponentModel.DataAnnotations;
using CinemaSystem.Common.Constants;

namespace CinemaSystem.Common.DTOs.Fnb;

public sealed record FnbItemResponse(
    Guid Id,
    Guid CreatedBy,
    string Name,
    string Type,
    string? Description,
    decimal Price,
    string? ImageUrl,
    string? ImagePublicId,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed class CreateFnbItemRequest
{
    [Required(ErrorMessage = FnbMessages.Required)]
    [StringLength(100, ErrorMessage = FnbMessages.MaxLengthExceeded)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = FnbMessages.Required)]
    [AllowedValues("COMBO", "FOOD", "DRINK", ErrorMessage = FnbMessages.InvalidType)]
    public string Type { get; init; } = string.Empty;

    public string? Description { get; init; }

    [Range(0.01, double.MaxValue, ErrorMessage = FnbMessages.InvalidPrice)]
    public decimal Price { get; init; }

    [Url(ErrorMessage = FnbMessages.InvalidImageUrl)]
    [StringLength(500, ErrorMessage = FnbMessages.MaxLengthExceeded)]
    public string? ImageUrl { get; init; }

    [StringLength(255, ErrorMessage = FnbMessages.MaxLengthExceeded)]
    public string? ImagePublicId { get; init; }

    [Required(ErrorMessage = FnbMessages.Required)]
    [AllowedValues("ACTIVE", "INACTIVE", ErrorMessage = FnbMessages.InvalidStatus)]
    public string Status { get; init; } = "ACTIVE";
}

public sealed class UpdateFnbItemRequest
{
    [Required(ErrorMessage = FnbMessages.Required)]
    [StringLength(100, ErrorMessage = FnbMessages.MaxLengthExceeded)]
    public string Name { get; init; } = string.Empty;

    [Required(ErrorMessage = FnbMessages.Required)]
    [AllowedValues("COMBO", "FOOD", "DRINK", ErrorMessage = FnbMessages.InvalidType)]
    public string Type { get; init; } = string.Empty;

    public string? Description { get; init; }

    [Range(0.01, double.MaxValue, ErrorMessage = FnbMessages.InvalidPrice)]
    public decimal Price { get; init; }

    [Url(ErrorMessage = FnbMessages.InvalidImageUrl)]
    [StringLength(500, ErrorMessage = FnbMessages.MaxLengthExceeded)]
    public string? ImageUrl { get; init; }

    [StringLength(255, ErrorMessage = FnbMessages.MaxLengthExceeded)]
    public string? ImagePublicId { get; init; }

    [Required(ErrorMessage = FnbMessages.Required)]
    [AllowedValues("ACTIVE", "INACTIVE", ErrorMessage = FnbMessages.InvalidStatus)]
    public string Status { get; init; } = "ACTIVE";
}

public sealed class FnbItemSearchRequest
{
    [StringLength(100, ErrorMessage = FnbMessages.MaxLengthExceeded)]
    public string? Keyword { get; init; }

    [StringLength(50, ErrorMessage = FnbMessages.MaxLengthExceeded)]
    public string? Type { get; init; }

    [StringLength(20, ErrorMessage = FnbMessages.MaxLengthExceeded)]
    public string? Status { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = FnbMessages.OutOfRange)]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = FnbMessages.OutOfRange)]
    public int PageSize { get; init; } = 20;
}
