using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IRoomRepository
{
    IQueryable<Room> Query();

    Task<Room?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Room room, CancellationToken cancellationToken = default);

    void Update(Room room);

    void Delete(Room room);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
