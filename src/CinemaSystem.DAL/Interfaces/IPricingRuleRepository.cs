using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IPricingRuleRepository
{
    Task<PricingRule?> GetActivePricingRuleAsync(Guid cinemaId, string roomType, string timeSlot, DateTime date, CancellationToken cancellationToken = default);

    IQueryable<PricingRule> Query();

    Task<PricingRule?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(PricingRule pricingRule, CancellationToken cancellationToken = default);

    void Update(PricingRule pricingRule);

    void Delete(PricingRule pricingRule);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
