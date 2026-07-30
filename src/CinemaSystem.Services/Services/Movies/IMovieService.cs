using CinemaSystem.Common;
using CinemaSystem.Common.DTOs;
using CinemaSystem.Common.DTOs.Movies;
using CinemaSystem.Common.Enums;
namespace CinemaSystem.Services.Services.Movies;

public interface IMovieService
{
    Task<IReadOnlyList<MovieSearchResponse>> SearchByTitleAsync(
        string? keyword,
        CancellationToken cancellationToken = default);

    Task<PagedResult<MovieResponse>> SearchAsync(
        MovieSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<MovieResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<MovieResponse> CreateAsync(
        CreateMovieRequest request,
        CancellationToken cancellationToken = default);

    Task<MovieResponse?> UpdateAsync(
        Guid id,
        UpdateMovieRequest request,
        CancellationToken cancellationToken = default);

    Task<MovieResponse?> UpdatePosterAsync(
        Guid id,
        string posterUrl,
        string? posterPublicId = null,
        CancellationToken cancellationToken = default);

    Task<DeleteMovieResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}


