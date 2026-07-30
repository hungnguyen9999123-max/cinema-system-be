using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Repository.Fnb;

public sealed class FnbItemRepository(CinemaDbContext dbContext) : IFnbItemRepository
{
    public IQueryable<FnbItem> Query() => dbContext.FnbItems.AsQueryable();

    public async Task<FnbItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.FnbItems.FindAsync([id], cancellationToken);

    public async Task AddAsync(FnbItem item, CancellationToken cancellationToken = default)
        => await dbContext.FnbItems.AddAsync(item, cancellationToken);

    public void Update(FnbItem item) => dbContext.FnbItems.Update(item);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
