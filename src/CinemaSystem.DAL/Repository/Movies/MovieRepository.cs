using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.DAL.Repositories.Movies;

public sealed class MovieRepository(CinemaDbContext dbContext) : IMovieRepository
{
    private const string BookingConfirmed = "CONFIRMED";

    public IQueryable<Movie> Query()
    {
        return dbContext.Movies.AsQueryable();
    }

    public async Task<Movie?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Movies.FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(
        Movie movie,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Movies.AddAsync(movie, cancellationToken);
    }

    public async Task<bool> HasShowtimesAsync(
        Guid movieId,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Showtimes
            .AnyAsync(showtime => showtime.MovieId == movieId, cancellationToken);
    }

    public Task DeleteAsync(
        Movie movie,
        CancellationToken cancellationToken = default)
    {
        dbContext.Movies.Remove(movie);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<List<Movie>> GetActiveMoviesWithFutureShowtimesAsync(
        DateTime fromTime,
        CancellationToken cancellationToken = default)
    {
        // Use List<string> rather than string[] because EF Core's SQL Server
        // translator does not handle Contains() over arrays correctly (it tries
        // to treat the array as ReadOnlySpan).
        var bookableStatuses = new List<string> { "UPCOMING", "NOW_SHOWING" };
        return await dbContext.Movies
            .AsNoTracking()
            .Where(m => bookableStatuses.Contains(m.Status))
            .Where(m => m.Showtimes.Any(s => s.StartTime >= fromTime))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(Guid MovieId, int BookingCount)>> GetTopMoviesByConfirmedBookingsAsync(
        int limit,
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Bookings
            .AsNoTracking()
            .Where(b => b.Status == BookingConfirmed)
            .GroupBy(b => b.Showtime!.MovieId)
            .Select(g => new { MovieId = g.Key, BookingCount = g.Count() })
            .OrderByDescending(x => x.BookingCount)
            .Take(limit)
            .Select(x => new ValueTuple<Guid, int>(x.MovieId, x.BookingCount))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<(Guid MovieId, double AverageRating, int FeedbackCount)>> GetMovieRatingAggregatesAsync(
        CancellationToken cancellationToken = default)
    {
        return await dbContext.Feedbacks
            .AsNoTracking()
            .GroupBy(f => f.MovieId)
            .Select(g => new
            {
                MovieId = g.Key,
                AverageRating = g.Average(x => (double)x.Rating),
                FeedbackCount = g.Count(),
            })
            .Select(x => new ValueTuple<Guid, double, int>(x.MovieId, x.AverageRating, x.FeedbackCount))
            .ToListAsync(cancellationToken);
    }
}
