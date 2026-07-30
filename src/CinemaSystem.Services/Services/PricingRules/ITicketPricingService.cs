using CinemaSystem.Common.DTOs.PricingRules;

namespace CinemaSystem.Services.Services.PricingRules;

public interface ITicketPricingService
{
    Task<TicketPriceResponse> CalculateUnitPriceAsync(
        CalculateTicketPriceRequest request,
        CancellationToken cancellationToken = default);
}
