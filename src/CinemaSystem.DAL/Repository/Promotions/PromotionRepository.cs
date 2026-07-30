using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Promotions;

/// <summary>
/// Entity Framework Core implementation of promotion persistence.
/// </summary>
public sealed class PromotionRepository(CinemaDbContext dbContext) : IPromotionRepository
{
    public IQueryable<Promotion> Query() => dbContext.Promotions.AsQueryable();

    public async Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Promotions
            .Include(p => p.CreatedByNavigation)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    public async Task<Promotion?> GetByCodeAsync(string promoCode, CancellationToken cancellationToken = default)
        => await dbContext.Promotions
            .Include(p => p.CreatedByNavigation)
            .FirstOrDefaultAsync(p => p.PromoCode == promoCode, cancellationToken);

    public async Task<bool> CodeExistsAsync(string promoCode, Guid? excludedPromotionId = null, CancellationToken cancellationToken = default)
        => await dbContext.Promotions.AnyAsync(p =>
            p.PromoCode == promoCode &&
            (!excludedPromotionId.HasValue || p.Id != excludedPromotionId.Value), cancellationToken);

    public async Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default)
        => await dbContext.Promotions.AddAsync(promotion, cancellationToken);

    public void Update(Promotion promotion) => dbContext.Promotions.Update(promotion);

    public void Delete(Promotion promotion) => dbContext.Promotions.Remove(promotion);
}
