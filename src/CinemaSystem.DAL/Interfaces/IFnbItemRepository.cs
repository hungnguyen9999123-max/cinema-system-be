using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IFnbItemRepository
{
    IQueryable<FnbItem> Query();

    Task<FnbItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(FnbItem item, CancellationToken cancellationToken = default);

    void Update(FnbItem item);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
