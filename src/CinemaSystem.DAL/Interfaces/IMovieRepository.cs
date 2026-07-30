using CinemaSystem.DAL.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.DAL.Interfaces;

public interface IMovieRepository
{
    IQueryable<Movie> Query();

    Task<Movie?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Movie movie, CancellationToken cancellationToken = default);

    Task<bool> HasShowtimesAsync(Guid movieId, CancellationToken cancellationToken = default);

    Task DeleteAsync(Movie movie, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active movies (Status == "ACTIVE") that have at least one
    /// showtime starting on or after <paramref name="fromTime"/>. Used by the
    /// recommendation engine to filter out movies that the user cannot book.
    /// </summary>
    Task<List<Movie>> GetActiveMoviesWithFutureShowtimesAsync(
        DateTime fromTime,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the top <paramref name="limit"/> movies ordered by the number of
    /// distinct CONFIRMED bookings they have, descending. Used by the trending
    /// fallback for cold-start users.
    /// </summary>
    Task<List<(Guid MovieId, int BookingCount)>> GetTopMoviesByConfirmedBookingsAsync(
        int limit,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the average rating and total feedback count for every movie.
    /// Result is sparse — movies with no feedback are excluded.
    /// </summary>
    Task<List<(Guid MovieId, double AverageRating, int FeedbackCount)>> GetMovieRatingAggregatesAsync(
        CancellationToken cancellationToken = default);
}

