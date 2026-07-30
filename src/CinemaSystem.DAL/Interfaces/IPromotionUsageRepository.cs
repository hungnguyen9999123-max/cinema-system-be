using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

/// <summary>
/// Reads and writes promotion usage records.
/// </summary>
public interface IPromotionUsageRepository
{
    IQueryable<PromotionUsage> Query();

    Task<PromotionUsage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<List<PromotionUsage>> GetByPromotionIdAsync(Guid promotionId, CancellationToken cancellationToken = default);

    Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default);

    Task<int> CountByPromotionIdAsync(Guid promotionId, CancellationToken cancellationToken = default);

    Task<int> CountAllAsync(CancellationToken cancellationToken = default);

    Task<decimal> SumDiscountByPromotionIdAsync(Guid promotionId, CancellationToken cancellationToken = default);

    Task AddAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default);
}
