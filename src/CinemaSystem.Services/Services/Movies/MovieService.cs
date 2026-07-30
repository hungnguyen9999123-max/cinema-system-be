using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Movies;
using CinemaSystem.Common.Enums;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.Services.Services.Uploads;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.Movies;

public sealed class MovieService(
    IMovieRepository movieRepository,
    ICloudinaryService cloudinaryService) : IMovieService
{
    private const int AutocompleteResultLimit = 10;
    private const int MaxKeywordLength = 255;

    public async Task<IReadOnlyList<MovieSearchResponse>> SearchByTitleAsync(
        string? keyword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            throw new InvalidOperationException(CommonMessages.Required);
        }

        var normalizedKeyword = keyword.Trim();
        if (normalizedKeyword.Length > MaxKeywordLength)
        {
            throw new InvalidOperationException(CommonMessages.MaxLengthExceeded);
        }

        var caseInsensitiveKeyword = normalizedKeyword.ToLowerInvariant();
        return await movieRepository.Query()
            .AsNoTracking()
            .Where(movie => movie.Title.ToLower().Contains(caseInsensitiveKeyword))
            .OrderBy(movie => movie.Title)
            .Take(AutocompleteResultLimit)
            .Select(movie => new MovieSearchResponse(
                movie.Id,
                movie.Title,
                movie.PosterUrl,
                movie.DurationMin,
                movie.Status,
                movie.Showtimes.Any()))
            .ToListAsync(cancellationToken);
    }

    public async Task<PagedResult<MovieResponse>> SearchAsync(
        MovieSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = movieRepository.Query().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var term = request.Query.Trim();
            query = query.Where(movie =>
                movie.Title.Contains(term) ||
                movie.Genre.Contains(term) ||
                movie.Language.Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Genre))
        {
            query = query.Where(movie => movie.Genre == request.Genre.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            query = query.Where(movie => movie.Language == request.Language.Trim());
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            query = query.Where(movie => movie.Status == request.Status.Trim());
        }

        if (request.ReleaseFrom.HasValue)
        {
            query = query.Where(movie => movie.ReleaseDate >= request.ReleaseFrom.Value);
        }

        if (request.ReleaseTo.HasValue)
        {
            query = query.Where(movie => movie.ReleaseDate <= request.ReleaseTo.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(movie => movie.ReleaseDate)
            .ThenBy(movie => movie.Title)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(movie => ToResponse(movie))
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResult<MovieResponse>(
            items,
            request.Page,
            request.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<MovieResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await movieRepository.Query()
            .AsNoTracking()
            .Where(movie => movie.Id == id)
            .Select(movie => ToResponse(movie))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<MovieResponse> CreateAsync(
        CreateMovieRequest request,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var movie = new Movie
        {
            Id = Guid.NewGuid(),
            CreatedBy = request.CreatedBy,
            Title = request.Title.Trim(),
            Genre = request.Genre.Trim(),
            Language = request.Language.Trim(),
            DurationMin = request.DurationMin,
            ReleaseDate = request.ReleaseDate,
            Synopsis = NormalizeOptional(request.Synopsis),
            AgeRating = request.AgeRating.Trim(),
            PosterUrl = NormalizeOptional(request.PosterUrl),
            PosterPublicId = NormalizeOptional(request.PosterPublicId),
            BannerUrl = NormalizeOptional(request.BannerUrl),
            BannerPublicId = NormalizeOptional(request.BannerPublicId),
            TrailerUrl = NormalizeOptional(request.TrailerUrl),
            Status = request.Status.Trim(),
            CreatedAt = now,
            UpdatedAt = now
        };

        await movieRepository.AddAsync(movie, cancellationToken);
        await movieRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(movie);
    }

    public async Task<MovieResponse?> UpdateAsync(
        Guid id,
        UpdateMovieRequest request,
        CancellationToken cancellationToken = default)
    {
        var movie = await movieRepository.GetByIdAsync(id, cancellationToken);
        if (movie is null)
        {
            return null;
        }

        await DeleteReplacedImageAsync(
            movie.PosterPublicId,
            request.PosterPublicId,
            cancellationToken);
        await DeleteReplacedImageAsync(
            movie.BannerPublicId,
            request.BannerPublicId,
            cancellationToken);

        movie.Title = request.Title.Trim();
        movie.Genre = request.Genre.Trim();
        movie.Language = request.Language.Trim();
        movie.DurationMin = request.DurationMin;
        movie.ReleaseDate = request.ReleaseDate;
        movie.Synopsis = NormalizeOptional(request.Synopsis);
        movie.AgeRating = request.AgeRating.Trim();
        movie.PosterUrl = NormalizeOptional(request.PosterUrl);
        movie.PosterPublicId = NormalizeOptional(request.PosterPublicId);
        movie.BannerUrl = NormalizeOptional(request.BannerUrl);
        movie.BannerPublicId = NormalizeOptional(request.BannerPublicId);
        movie.TrailerUrl = NormalizeOptional(request.TrailerUrl);
        movie.Status = request.Status.Trim();
        movie.UpdatedAt = DateTime.UtcNow;

        await movieRepository.SaveChangesAsync(cancellationToken);
        return ToResponse(movie);
    }

    public async Task<MovieResponse?> UpdatePosterAsync(
        Guid id,
        string posterUrl,
        string? posterPublicId = null,
        CancellationToken cancellationToken = default)
    {
        var movie = await movieRepository.GetByIdAsync(id, cancellationToken);
        if (movie is null)
        {
            return null;
        }

        await DeleteReplacedImageAsync(
            movie.PosterPublicId,
            posterPublicId,
            cancellationToken);

        movie.PosterUrl = posterUrl;
        movie.PosterPublicId = NormalizeOptional(posterPublicId);
        movie.UpdatedAt = DateTime.UtcNow;
        await movieRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(movie);
    }

    public async Task<DeleteMovieResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var movie = await movieRepository.GetByIdAsync(id, cancellationToken);
        if (movie is null)
        {
            return DeleteMovieResult.NotFound;
        }

        if (await movieRepository.HasShowtimesAsync(id, cancellationToken))
        {
            return DeleteMovieResult.HasShowtimes;
        }

        await DeleteImagesAsync(
            [movie.PosterPublicId, movie.BannerPublicId],
            cancellationToken);
        await movieRepository.DeleteAsync(movie, cancellationToken);
        await movieRepository.SaveChangesAsync(cancellationToken);
        return DeleteMovieResult.Deleted;
    }

    private static MovieResponse ToResponse(Movie movie) =>
        new(
            movie.Id,
            movie.CreatedBy,
            movie.Title,
            movie.Genre,
            movie.Language,
            movie.DurationMin,
            movie.ReleaseDate,
            movie.Synopsis,
            movie.AgeRating,
            movie.PosterUrl,
            movie.PosterPublicId,
            movie.BannerUrl,
            movie.BannerPublicId,
            movie.TrailerUrl,
            movie.Status,
            movie.CreatedAt,
            movie.UpdatedAt);

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task DeleteReplacedImageAsync(
        string? currentPublicId,
        string? newPublicId,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(currentPublicId) &&
            !string.Equals(
                currentPublicId,
                NormalizeOptional(newPublicId),
                StringComparison.Ordinal))
        {
            await cloudinaryService.DeleteImageAsync(
                currentPublicId,
                cancellationToken);
        }
    }

    private async Task DeleteImagesAsync(
        IEnumerable<string?> publicIds,
        CancellationToken cancellationToken)
    {
        foreach (var publicId in publicIds
                     .Where(value => !string.IsNullOrWhiteSpace(value))
                     .Distinct(StringComparer.Ordinal))
        {
            await cloudinaryService.DeleteImageAsync(
                publicId!,
                cancellationToken);
        }
    }
}
