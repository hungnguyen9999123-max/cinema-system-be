using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Seats;

public sealed class SeatRepository(CinemaDbContext dbContext) : ISeatRepository
{
    public IQueryable<Seat> Query() => dbContext.Seats.AsQueryable();

    public async Task<Seat?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Seats.FindAsync([id], cancellationToken);

    public async Task<List<Seat>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idSet = ids.Distinct().ToList();
        if (idSet.Count == 0)
        {
            return new List<Seat>();
        }

        return await dbContext.Seats
            .Include(s => s.SeatType)
            .Where(seat => idSet.Contains(seat.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Seat>> GetByRoomIdAsync(Guid roomId, CancellationToken cancellationToken = default)
        => await dbContext.Seats
            .Include(s => s.SeatType)
            .Where(seat => seat.RoomId == roomId)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsLabelInRoomAsync(Guid roomId, string seatLabel, Guid? excludeId = null, CancellationToken cancellationToken = default)
        => await dbContext.Seats.AnyAsync(seat =>
            seat.RoomId == roomId &&
            seat.SeatLabel == seatLabel &&
            (!excludeId.HasValue || seat.Id != excludeId.Value), cancellationToken);

    public async Task<bool> HasBookingHistoryAsync(Guid seatId, CancellationToken cancellationToken = default)
        => await dbContext.BookingSeats.AnyAsync(bookingSeat => bookingSeat.SeatId == seatId, cancellationToken);

    public async Task<List<string>> GetBookedSeatLabelsAsync(Guid roomId, CancellationToken cancellationToken = default)
        => await dbContext.Seats
            .Where(seat => seat.RoomId == roomId)
            .Where(seat => seat.BookingSeats.Any())
            .Select(seat => seat.SeatLabel)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Seat seat, CancellationToken cancellationToken = default)
        => await dbContext.Seats.AddAsync(seat, cancellationToken);

    public async Task AddRangeAsync(IEnumerable<Seat> seats, CancellationToken cancellationToken = default)
        => await dbContext.Seats.AddRangeAsync(seats, cancellationToken);

    public void Update(Seat seat) => dbContext.Seats.Update(seat);

    public void Delete(Seat seat) => dbContext.Seats.Remove(seat);

    public void DeleteRange(IEnumerable<Seat> seats) => dbContext.Seats.RemoveRange(seats);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
