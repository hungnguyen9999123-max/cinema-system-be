using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IShowtimeRepository
{
    IQueryable<Showtime> Query();

    Task<Showtime?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> HasOverlappingShowtimeAsync(Guid roomId, DateTime startTime, DateTime endTime, Guid? excludeShowtimeId = null, CancellationToken cancellationToken = default);

    Task<bool> HasBookingSeatsAsync(Guid showtimeId, CancellationToken cancellationToken = default);

    Task AddAsync(Showtime showtime, CancellationToken cancellationToken = default);

    void Update(Showtime showtime);

    void Delete(Showtime showtime);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
