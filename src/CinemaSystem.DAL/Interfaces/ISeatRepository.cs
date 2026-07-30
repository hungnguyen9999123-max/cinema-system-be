using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface ISeatRepository
{
    IQueryable<Seat> Query();

    Task<Seat?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<Seat>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    Task<List<Seat>> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default);

    Task<bool> ExistsLabelInRoomAsync(Guid roomId, string seatLabel, Guid? excludeId = null, CancellationToken cancellationToken = default);

    Task<bool> HasBookingHistoryAsync(Guid seatId, CancellationToken cancellationToken = default);

    Task<List<string>> GetBookedSeatLabelsAsync(Guid roomId, CancellationToken cancellationToken = default);

    Task AddAsync(Seat seat, CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Seat> seats, CancellationToken cancellationToken = default);

    void Update(Seat seat);

    void Delete(Seat seat);

    void DeleteRange(IEnumerable<Seat> seats);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
