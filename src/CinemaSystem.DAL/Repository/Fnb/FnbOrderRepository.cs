using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Fnb;

public sealed class FnbOrderRepository(CinemaDbContext dbContext) : IFnbOrderRepository
{
    public IQueryable<FnbOrder> Query() => dbContext.FnbOrders.AsQueryable();

    public async Task<FnbOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.FnbOrders.FindAsync([id], cancellationToken);

    public async Task<FnbOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.FnbOrders
            .Include(o => o.FnbOrderDetails)
            .ThenInclude(d => d.Item)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    public async Task AddAsync(FnbOrder order, CancellationToken cancellationToken = default)
        => await dbContext.FnbOrders.AddAsync(order, cancellationToken);

    public void Update(FnbOrder order) => dbContext.FnbOrders.Update(order);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}

public sealed class FnbOrderDetailRepository(CinemaDbContext dbContext) : IFnbOrderDetailRepository
{
    public async Task AddRangeAsync(IEnumerable<FnbOrderDetail> details, CancellationToken cancellationToken = default)
        => await dbContext.FnbOrderDetails.AddRangeAsync(details, cancellationToken);
}
