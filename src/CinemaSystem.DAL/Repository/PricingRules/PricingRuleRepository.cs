using CinemaSystem.Common.Helpers;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.PricingRules;

public sealed class PricingRuleRepository(CinemaDbContext dbContext) : IPricingRuleRepository
{
    public async Task<PricingRule?> GetActivePricingRuleAsync(
        Guid cinemaId,
        string roomType,
        string timeSlot,
        DateTime date,
        CancellationToken cancellationToken = default)
    {
        var roomTypeId = (int)PricingKindMapper.FromLegacyRoomType(roomType);
        var timeSlotId = (int)PricingKindMapper.FromLegacyTimeSlot(timeSlot);
        var showDate = DateOnly.FromDateTime(date);

        return await dbContext.PricingRules
            .Where(rule =>
                rule.CinemaId == cinemaId &&
                rule.RoomTypeId == roomTypeId &&
                rule.TimeSlotId == timeSlotId &&
                rule.IsActive &&
                rule.EffectiveFrom <= showDate &&
                rule.EffectiveTo >= showDate)
            .OrderByDescending(rule => rule.EffectiveFrom)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public IQueryable<PricingRule> Query() => dbContext.PricingRules.AsQueryable();

    public async Task<PricingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.PricingRules.FindAsync([id], cancellationToken);

    public async Task AddAsync(PricingRule pricingRule, CancellationToken cancellationToken = default)
        => await dbContext.PricingRules.AddAsync(pricingRule, cancellationToken);

    public void Update(PricingRule pricingRule) => dbContext.PricingRules.Update(pricingRule);

    public void Delete(PricingRule pricingRule) => dbContext.PricingRules.Remove(pricingRule);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => dbContext.SaveChangesAsync(cancellationToken);
}
