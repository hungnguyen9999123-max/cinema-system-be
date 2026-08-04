using System.Security.Claims;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Promotions;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.Promotions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

/// <summary>
/// Manages promotional campaigns and promo-code validation.
/// </summary>
[ApiController]
[Route("api/promotions")]
public sealed class PromotionsController(IPromotionService promotionService) : ControllerBase
{
    [HttpGet]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<PromotionResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PromotionResponse>>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] bool? isActive,
        CancellationToken cancellationToken)
    {
        var promotions = await promotionService.GetAllAsync(search, isActive, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PromotionResponse>>.Success(
            promotions,
            PromotionMessages.RetrievedSuccessfully));
    }

    [HttpGet("public")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<IReadOnlyList<PromotionResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PromotionResponse>>>> GetPublic(
        [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        var promotions = await promotionService.GetAllAsync(search, true, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PromotionResponse>>.Success(
            promotions,
            PromotionMessages.RetrievedSuccessfully));
    }

    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType<ApiResponse<PromotionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PromotionResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var promotion = await promotionService.GetByIdAsync(id, cancellationToken);
        return promotion is null
            ? NotFound(ApiResponse<PromotionResponse>.Fail(PromotionMessages.NotFound))
            : Ok(ApiResponse<PromotionResponse>.Success(promotion, PromotionMessages.RetrievedDetailSuccessfully));
    }

    [HttpPost]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType<ApiResponse<PromotionResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PromotionResponse>>> Create(
        [FromBody] CreatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var createdBy = GetCurrentUserId();
        if (createdBy is null)
        {
            return Unauthorized(ApiResponse<PromotionResponse>.Fail("Unauthorized access."));
        }

        var promotion = await promotionService.CreateAsync(createdBy.Value, request, cancellationToken);
        return Ok(ApiResponse<PromotionResponse>.Success(promotion, PromotionMessages.CreatedSuccessfully));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType<ApiResponse<PromotionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PromotionResponse>>> Update(
        Guid id,
        [FromBody] UpdatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        var promotion = await promotionService.UpdateAsync(id, request, cancellationToken);
        return promotion is null
            ? NotFound(ApiResponse<PromotionResponse>.Fail(PromotionMessages.NotFound))
            : Ok(ApiResponse<PromotionResponse>.Success(promotion, PromotionMessages.UpdatedSuccessfully));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await promotionService.DeleteAsync(id, cancellationToken);
        return deleted
            ? NoContent()
            : NotFound(ApiResponse<object?>.Fail(PromotionMessages.NotFound));
    }

    [HttpPatch("{id:guid}/activate")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType<ApiResponse<PromotionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PromotionResponse>>> Activate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var promotion = await promotionService.ActivateAsync(id, cancellationToken);
        return promotion is null
            ? NotFound(ApiResponse<PromotionResponse>.Fail(PromotionMessages.NotFound))
            : Ok(ApiResponse<PromotionResponse>.Success(promotion, PromotionMessages.ActivatedSuccessfully));
    }

    [HttpPatch("{id:guid}/deactivate")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType<ApiResponse<PromotionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PromotionResponse>>> Deactivate(
        Guid id,
        CancellationToken cancellationToken)
    {
        var promotion = await promotionService.DeactivateAsync(id, cancellationToken);
        return promotion is null
            ? NotFound(ApiResponse<PromotionResponse>.Fail(PromotionMessages.NotFound))
            : Ok(ApiResponse<PromotionResponse>.Success(promotion, PromotionMessages.DeactivatedSuccessfully));
    }

    [HttpGet("statistics")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType<ApiResponse<PromotionStatisticsResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PromotionStatisticsResponse>>> GetStatistics(CancellationToken cancellationToken)
    {
        var statistics = await promotionService.GetStatisticsAsync(cancellationToken);
        return Ok(ApiResponse<PromotionStatisticsResponse>.Success(
            statistics,
            PromotionMessages.StatisticsRetrievedSuccessfully));
    }

    [HttpGet("{id:guid}/usages")]
    [Authorize(Roles = "Manager")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<PromotionUsageResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PromotionUsageResponse>>>> GetUsages(
        Guid id,
        CancellationToken cancellationToken)
    {
        var usages = await promotionService.GetUsagesAsync(id, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<PromotionUsageResponse>>.Success(
            usages,
            PromotionMessages.UsagesRetrievedSuccessfully));
    }

    [HttpPost("validate")]
    [Authorize(Roles = "Customer,Staff")]
    [ProducesResponseType<ApiResponse<ValidatePromotionResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<ValidatePromotionResponse>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<ValidatePromotionResponse>>(StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ValidatePromotionResponse>>> Validate(
        [FromBody] ValidatePromotionRequest request,
        CancellationToken cancellationToken)
    {
        Guid? customerId = null;

        // Chỉ lấy CustomerId nếu là Customer
        if (User.IsInRole("Customer"))
        {
            customerId = GetCurrentUserId();
        }

        var response = await promotionService.ValidateAsync(
            customerId,
            request,
            cancellationToken);

        return Ok(
            ApiResponse<ValidatePromotionResponse>.Success(
                response,
                response.Message));
    }

    // [HttpPost("validate")]
    // [Authorize]
    // [ProducesResponseType<ApiResponse<ValidatePromotionResponse>>(StatusCodes.Status200OK)]
    // [ProducesResponseType<ApiResponse<ValidatePromotionResponse>>(StatusCodes.Status400BadRequest)]
    // [ProducesResponseType<ApiResponse<ValidatePromotionResponse>>(StatusCodes.Status403Forbidden)]
    // public async Task<ActionResult<ApiResponse<ValidatePromotionResponse>>> Validate(
    //     [FromBody] ValidatePromotionRequest request,
    //     CancellationToken cancellationToken)
    // {
    //     if (!User.IsInRole("Customer"))
    //     {
    //         return Forbid();
    //     }

    //     var customerId = GetCurrentUserId();
    //     var response = await promotionService.ValidateAsync(customerId, request, cancellationToken);
    //     return Ok(ApiResponse<ValidatePromotionResponse>.Success(response, response.Message));
    // }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
