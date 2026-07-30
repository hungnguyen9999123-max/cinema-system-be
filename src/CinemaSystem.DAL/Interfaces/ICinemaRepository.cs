using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface ICinemaRepository
{
    IQueryable<Cinema> Query();

    Task<Cinema?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(Cinema cinema, CancellationToken cancellationToken = default);

    void Update(Cinema cinema);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
