using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Rooms;

public sealed class RoomRepository(CinemaDbContext dbContext) : IRoomRepository
{
    public IQueryable<Room> Query() => dbContext.Rooms.AsQueryable();

    public async Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Rooms.FindAsync([id], cancellationToken);

    public async Task AddAsync(Room room, CancellationToken cancellationToken = default)
        => await dbContext.Rooms.AddAsync(room, cancellationToken);

    public void Update(Room room) => dbContext.Rooms.Update(room);

    public void Delete(Room room) => dbContext.Rooms.Remove(room);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
