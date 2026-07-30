using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Promotions;

/// <summary>
/// Entity Framework Core implementation of promotion usage persistence.
/// </summary>
public sealed class PromotionUsageRepository(CinemaDbContext dbContext) : IPromotionUsageRepository
{
    public IQueryable<PromotionUsage> Query() => dbContext.PromotionUsages.AsQueryable();

    public async Task<PromotionUsage?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.PromotionUsages
            .Include(x => x.Promotion)
            .Include(x => x.Customer)
            .Include(x => x.Booking)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<List<PromotionUsage>> GetByPromotionIdAsync(Guid promotionId, CancellationToken cancellationToken = default)
        => await dbContext.PromotionUsages
            .Include(x => x.Promotion)
            .Include(x => x.Customer)
            .Include(x => x.Booking)
            .Where(x => x.PromotionId == promotionId)
            .OrderByDescending(x => x.UsedAt)
            .ToListAsync(cancellationToken);

    public async Task<bool> ExistsForBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
        => await dbContext.PromotionUsages.AnyAsync(x => x.BookingId == bookingId, cancellationToken);

    public async Task<int> CountByPromotionIdAsync(Guid promotionId, CancellationToken cancellationToken = default)
        => await dbContext.PromotionUsages.CountAsync(x => x.PromotionId == promotionId, cancellationToken);

    public async Task<int> CountAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.PromotionUsages.CountAsync(cancellationToken);

    public async Task<decimal> SumDiscountByPromotionIdAsync(Guid promotionId, CancellationToken cancellationToken = default)
    {
        var bookingIds = dbContext.PromotionUsages
            .Where(x => x.PromotionId == promotionId)
            .Select(x => x.BookingId);

        return await dbContext.Bookings
            .Where(b => bookingIds.Contains(b.Id))
            .SumAsync(b => (decimal?)(b.TotalAmount - b.FinalAmount), cancellationToken) ?? 0m;
    }

    public async Task AddAsync(PromotionUsage promotionUsage, CancellationToken cancellationToken = default)
        => await dbContext.PromotionUsages.AddAsync(promotionUsage, cancellationToken);
}
