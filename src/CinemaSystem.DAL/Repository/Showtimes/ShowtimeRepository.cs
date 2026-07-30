using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Showtimes;

public sealed class ShowtimeRepository(CinemaDbContext dbContext) : IShowtimeRepository
{
    public IQueryable<Showtime> Query() => dbContext.Showtimes.AsQueryable();

    // public async Task<Showtime?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    //     => await dbContext.Showtimes.FindAsync([id], cancellationToken);
    public async Task<Showtime?> GetByIdAsync(
    Guid id,
    CancellationToken cancellationToken = default)
{
    return await dbContext.Showtimes
        .AsNoTracking()
        .Include(s => s.Movie)
        .Include(s => s.Room)
            .ThenInclude(r => r.Cinema)
        .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
}

    public async Task<bool> HasOverlappingShowtimeAsync(Guid roomId, DateTime startTime, DateTime endTime, Guid? excludeShowtimeId = null, CancellationToken cancellationToken = default)
        => await dbContext.Showtimes.AnyAsync(showtime =>
            showtime.RoomId == roomId &&
            showtime.Status != "CANCELLED" &&
            (!excludeShowtimeId.HasValue || showtime.Id != excludeShowtimeId.Value) &&
            showtime.StartTime < endTime &&
            showtime.EndTime > startTime, cancellationToken);

    public async Task<bool> HasBookingSeatsAsync(Guid showtimeId, CancellationToken cancellationToken = default)
        => await dbContext.BookingSeats.AnyAsync(bookingSeat => bookingSeat.ShowtimeId == showtimeId, cancellationToken);

    public async Task AddAsync(Showtime showtime, CancellationToken cancellationToken = default)
        => await dbContext.Showtimes.AddAsync(showtime, cancellationToken);

    public void Update(Showtime showtime) => dbContext.Showtimes.Update(showtime);

    public void Delete(Showtime showtime) => dbContext.Showtimes.Remove(showtime);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
