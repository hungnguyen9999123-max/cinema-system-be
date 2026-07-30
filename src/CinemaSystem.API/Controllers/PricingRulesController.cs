using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.PricingRules;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.Services.Services.PricingRules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/pricing-rules")]
public sealed class PricingRulesController(IPricingRuleService pricingRuleService) : ControllerBase
{
    [HttpGet("by-cinema/{cinemaId:guid}")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<PricingRuleResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PricingRuleResponse>>>> GetByCinemaId(
        Guid cinemaId,
        CancellationToken cancellationToken)
    {
        var rules = await pricingRuleService.GetByCinemaIdAsync(cinemaId, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PricingRuleResponse>>.Success(
            rules,
            PricingRuleMessages.RetrievedSuccessfully));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<PricingRuleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PricingRuleResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var rule = await pricingRuleService.GetByIdAsync(id, cancellationToken);
        return rule is null
            ? NotFound(ApiResponse<PricingRuleResponse>.Fail(PricingRuleMessages.NotFound))
            : Ok(ApiResponse<PricingRuleResponse>.Success(rule, PricingRuleMessages.RetrievedDetailSuccessfully));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType<ApiResponse<PricingRuleResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<PricingRuleResponse>>> Create(
        [FromQuery] Guid cinemaId,
        [FromBody] CreatePricingRuleRequest request,
        CancellationToken cancellationToken)
    {
        if (cinemaId == Guid.Empty)
        {
            return BadRequest(ApiResponse<PricingRuleResponse>.Fail(PricingRuleMessages.CinemaIdRequired));
        }

        var rule = await pricingRuleService.CreateAsync(cinemaId, request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = rule.Id },
            ApiResponse<PricingRuleResponse>.Success(rule, PricingRuleMessages.CreatedSuccessfully));
    }

    [HttpPost("{cinemaId:guid}/defaults")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object>>> RegenerateDefaults(
        Guid cinemaId,
        CancellationToken cancellationToken)
    {
        try
        {
            await pricingRuleService.RegenerateDefaultPricingRulesAsync(cinemaId, cancellationToken);
            return Ok(ApiResponse<object>.Success(new { cinemaId }, PricingRuleMessages.RegeneratedSuccessfully));
        }
        catch (InvalidOperationException ex) when (ex.Message == PricingRuleMessages.CinemaNotFound)
        {
            return NotFound(ApiResponse<object>.Fail(PricingRuleMessages.CinemaNotFound));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType<ApiResponse<PricingRuleResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PricingRuleResponse>>> Update(
        Guid id,
        [FromBody] UpdatePricingRuleRequest request,
        CancellationToken cancellationToken)
    {
        var rule = await pricingRuleService.UpdateAsync(id, request, cancellationToken);
        return rule is null
            ? NotFound(ApiResponse<PricingRuleResponse>.Fail(PricingRuleMessages.NotFound))
            : Ok(ApiResponse<PricingRuleResponse>.Success(rule, PricingRuleMessages.UpdatedSuccessfully));
    }
}