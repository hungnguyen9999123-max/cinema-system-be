using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.PricingRules;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.PricingRules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/ticket-pricing")]
public sealed class TicketPricingController(ITicketPricingService ticketPricingService) : ControllerBase
{
    [HttpPost("calculate")]
    [ProducesResponseType<ApiResponse<TicketPriceResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<TicketPriceResponse>>> Calculate(
        [FromBody] CalculateTicketPriceRequest request,
        CancellationToken cancellationToken)
    {
        var price = await ticketPricingService.CalculateUnitPriceAsync(request, cancellationToken);
        return Ok(ApiResponse<TicketPriceResponse>.Success(price, PricingRuleMessages.PriceCalculatedSuccessfully));
    }
}
