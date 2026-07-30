using CinemaSystem.Common.DTOs.PricingRules;

namespace CinemaSystem.Services.Services.PricingRules;

public interface IPricingRuleService
{
    Task GenerateDefaultPricingRulesAsync(Guid cinemaId, CancellationToken cancellationToken = default);

    Task RegenerateDefaultPricingRulesAsync(Guid cinemaId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PricingRuleResponse>> GetByCinemaIdAsync(
        Guid cinemaId,
        CancellationToken cancellationToken = default);

    Task<PricingRuleResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PricingRuleResponse?> UpdateAsync(
        Guid id,
        UpdatePricingRuleRequest request,
        CancellationToken cancellationToken = default);

    Task<PricingRuleResponse> CreateAsync(
        Guid cinemaId,
        CreatePricingRuleRequest request,
        CancellationToken cancellationToken = default);
}
