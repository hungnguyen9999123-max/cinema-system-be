
using System.ComponentModel.DataAnnotations;
using CinemaSystem.Common.Constants;

namespace CinemaSystem.Common.DTOs.Movies;

/// <summary>
/// Dữ liệu phim trả về cho Client
/// </summary>
public sealed record MovieResponse(
    Guid Id,
    Guid CreatedBy,
    string Title,
    string Genre,
    string Language,
    int DurationMin,
    DateOnly ReleaseDate,
    string? Synopsis,
    string AgeRating,
    string? PosterUrl,
    string? PosterPublicId,
    string? BannerUrl,
    string? BannerPublicId,
    string? TrailerUrl,
    string Status,
    DateTime CreatedAt,
    DateTime UpdatedAt);

/// <summary>
/// Dữ liệu phim rút gọn cho chức năng gợi ý tìm kiếm
/// </summary>
public sealed record MovieSearchResponse(
    Guid Id,
    string Title,
    string? PosterUrl,
    int DurationMin,
    string Status,
    bool HasShowtime);

/// <summary>
/// Yêu cầu tạo phim mới
/// </summary>
public sealed class CreateMovieRequest
{
    [Required(ErrorMessage = CommonMessages.Required)]
    public Guid CreatedBy { get; init; }

    [Required(ErrorMessage = CommonMessages.Required)]
    [StringLength(255, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = CommonMessages.Required)]
    [StringLength(100, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string Genre { get; init; } = string.Empty;

    [Required(ErrorMessage = CommonMessages.Required)]
    [StringLength(50, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string Language { get; init; } = string.Empty;

    [Range(1, 1000, ErrorMessage = CommonMessages.OutOfRange)]
    public int DurationMin { get; init; }

    [Required(ErrorMessage = CommonMessages.Required)]
    public DateOnly ReleaseDate { get; init; }

    public string? Synopsis { get; init; }

    [Required(ErrorMessage = CommonMessages.Required)]
    [StringLength(10, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string AgeRating { get; init; } = string.Empty;

    [Url(ErrorMessage = CommonMessages.InvalidUrl)]
    [StringLength(500, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string? PosterUrl { get; init; }

    [StringLength(255, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string? PosterPublicId { get; init; }

    [Url(ErrorMessage = CommonMessages.InvalidUrl)]
    [StringLength(500, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string? BannerUrl { get; init; }

    [StringLength(255, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string? BannerPublicId { get; init; }

    [Url(ErrorMessage = CommonMessages.InvalidUrl)]
    [StringLength(500, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string? TrailerUrl { get; init; }

    [Required(ErrorMessage = CommonMessages.Required)]
    [AllowedValues("UPCOMING", "NOW_SHOWING", "ARCHIVED", ErrorMessage = CommonMessages.InvalidValue)]
    public string Status { get; init; } = "UPCOMING";
}

/// <summary>
/// Yêu cầu cập nhật thông tin phim
/// </summary>
public sealed class UpdateMovieRequest
{
    [Required(ErrorMessage = CommonMessages.Required)]
    [StringLength(255, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string Title { get; init; } = string.Empty;

    [Required(ErrorMessage = CommonMessages.Required)]
    [StringLength(100, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string Genre { get; init; } = string.Empty;

    [Required(ErrorMessage = CommonMessages.Required)]
    [StringLength(50, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string Language { get; init; } = string.Empty;

    [Range(1, 1000, ErrorMessage = CommonMessages.OutOfRange)]
    public int DurationMin { get; init; }

    [Required(ErrorMessage = CommonMessages.Required)]
    public DateOnly ReleaseDate { get; init; }

    public string? Synopsis { get; init; }

    [Required(ErrorMessage = CommonMessages.Required)]
    [StringLength(10, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string AgeRating { get; init; } = string.Empty;

    [Url(ErrorMessage = CommonMessages.InvalidUrl)]
    [StringLength(500, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string? PosterUrl { get; init; }

    [StringLength(255, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string? PosterPublicId { get; init; }

    [Url(ErrorMessage = CommonMessages.InvalidUrl)]
    [StringLength(500, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string? BannerUrl { get; init; }

    [StringLength(255, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string? BannerPublicId { get; init; }

    [Url(ErrorMessage = CommonMessages.InvalidUrl)]
    [StringLength(500, ErrorMessage = CommonMessages.MaxLengthExceeded)]
    public string? TrailerUrl { get; init; }

    [Required(ErrorMessage = CommonMessages.Required)]
    [AllowedValues("UPCOMING", "NOW_SHOWING", "ARCHIVED", ErrorMessage = CommonMessages.InvalidValue)]
    public string Status { get; init; } = "UPCOMING";
}

/// <summary>
/// Yêu cầu tìm kiếm và phân trang phim
/// </summary>
public sealed class MovieSearchRequest
{
    [StringLength(255)]
    public string? Query { get; init; }

    [StringLength(100)]
    public string? Genre { get; init; }

    [StringLength(50)]
    public string? Language { get; init; }

    [StringLength(20)]
    public string? Status { get; init; }

    public DateOnly? ReleaseFrom { get; init; }
    public DateOnly? ReleaseTo { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = CommonMessages.OutOfRange)]
    public int Page { get; init; } = 1;

    [Range(1, 100, ErrorMessage = CommonMessages.OutOfRange)]
    public int PageSize { get; init; } = 20;
}
