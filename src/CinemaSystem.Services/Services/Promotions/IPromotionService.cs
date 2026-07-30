using CinemaSystem.Common.DTOs.Promotions;

namespace CinemaSystem.Services.Services.Promotions;

/// <summary>
/// Business operations for managing promotions and validating promo codes.
/// </summary>
public interface IPromotionService
{
    Task<IReadOnlyList<PromotionResponse>> GetAllAsync(string? search, bool? isActive, CancellationToken cancellationToken = default);

    Task<PromotionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PromotionResponse> CreateAsync(Guid createdBy, CreatePromotionRequest request, CancellationToken cancellationToken = default);

    Task<PromotionResponse?> UpdateAsync(Guid id, UpdatePromotionRequest request, CancellationToken cancellationToken = default);

    Task<PromotionResponse?> ActivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PromotionResponse?> DeactivateAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PromotionStatisticsResponse> GetStatisticsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PromotionUsageResponse>> GetUsagesAsync(Guid promotionId, CancellationToken cancellationToken = default);

    Task<ValidatePromotionResponse> ValidateAsync(ValidatePromotionRequest request, CancellationToken cancellationToken = default);

    Task<ValidatePromotionResponse> ValidateAsync(Guid? customerId, ValidatePromotionRequest request, CancellationToken cancellationToken = default);

    Task RecordUsageAsync(Guid bookingId, Guid customerId, Guid promotionId, CancellationToken cancellationToken = default);
}
