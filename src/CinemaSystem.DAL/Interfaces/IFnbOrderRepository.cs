using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IFnbOrderRepository
{
    IQueryable<FnbOrder> Query();

    Task<FnbOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<FnbOrder?> GetByIdWithDetailsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(FnbOrder order, CancellationToken cancellationToken = default);

    void Update(FnbOrder order);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IFnbOrderDetailRepository
{
    Task AddRangeAsync(IEnumerable<FnbOrderDetail> details, CancellationToken cancellationToken = default);
}
