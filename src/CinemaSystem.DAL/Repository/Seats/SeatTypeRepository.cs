using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Seats;

public sealed class SeatTypeRepository(CinemaDbContext dbContext) : ISeatTypeRepository
{
    public IQueryable<SeatType> Query() => dbContext.SeatTypes.AsQueryable();

    public async Task<SeatType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await dbContext.SeatTypes.FirstOrDefaultAsync(seatType => seatType.Name == name, cancellationToken);
}
