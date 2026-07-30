using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Cinemas;

public sealed class CinemaRepository(CinemaDbContext dbContext) : ICinemaRepository
{
    public IQueryable<Cinema> Query() => dbContext.Cinemas.AsQueryable();

    public async Task<Cinema?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Cinemas.FindAsync([id], cancellationToken);

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Cinemas.AnyAsync(cinema => cinema.Id == id, cancellationToken);

    public async Task AddAsync(Cinema cinema, CancellationToken cancellationToken = default)
        => await dbContext.Cinemas.AddAsync(cinema, cancellationToken);

    public void Update(Cinema cinema) => dbContext.Cinemas.Update(cinema);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
