using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface ISeatTypeRepository
{
    IQueryable<SeatType> Query();

    Task<SeatType?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
}
