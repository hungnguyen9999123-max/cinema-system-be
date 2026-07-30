using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

/// <summary>
/// Reads and writes promotion records.
/// </summary>
public interface IPromotionRepository
{
    IQueryable<Promotion> Query();

    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Promotion?> GetByCodeAsync(string promoCode, CancellationToken cancellationToken = default);

    Task<bool> CodeExistsAsync(string promoCode, Guid? excludedPromotionId = null, CancellationToken cancellationToken = default);

    Task AddAsync(Promotion promotion, CancellationToken cancellationToken = default);

    void Update(Promotion promotion);

    void Delete(Promotion promotion);
}
