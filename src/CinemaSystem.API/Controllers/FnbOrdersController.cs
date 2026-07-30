using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Fnb;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.Fnb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api/fnb-orders")]
public sealed class FnbOrdersController(IFnbOrderService fnbOrderService) : ControllerBase
{
    [HttpGet]
    [Authorize]
    [ProducesResponseType<ApiResponse<PagedResult<FnbOrderResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<FnbOrderResponse>>>> Search(
        [FromQuery] FnbOrderSearchRequest request,
        CancellationToken cancellationToken)
    {
        var orders = await fnbOrderService.SearchAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<FnbOrderResponse>>.Success(orders, FnbOrderMessages.RetrievedSuccess));
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FnbOrderResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var order = await fnbOrderService.GetByIdAsync(id, cancellationToken);
        return order is null
            ? NotFound(ApiResponse<FnbOrderResponse>.Fail(FnbOrderMessages.NotFound))
            : Ok(ApiResponse<FnbOrderResponse>.Success(order, FnbOrderMessages.DetailRetrievedSuccess));
    }

    [HttpPost]
    [Authorize]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<FnbOrderResponse>>> Create(
        [FromBody] CreateFnbOrderRequest request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId == Guid.Empty)
        {
            return BadRequest(ApiResponse<FnbOrderResponse>.Fail(CommonMessages.InvalidToken));
        }

        var order = await fnbOrderService.CreateAsync(request, customerId, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.Id },
            ApiResponse<FnbOrderResponse>.Success(order, FnbOrderMessages.CreatedSuccess));
    }

    [HttpPost("counter")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<FnbOrderResponse>>> CreateCounterOrder(
        [FromBody] CreateFnbCounterOrderRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        if (staffId == Guid.Empty)
        {
            return BadRequest(ApiResponse<FnbOrderResponse>.Fail(CommonMessages.InvalidToken));
        }

        var order = await fnbOrderService.CreateCounterOrderAsync(request, staffId, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.Id },
            ApiResponse<FnbOrderResponse>.Success(order, FnbOrderMessages.CounterOrderCreatedSuccess));
    }

    [HttpPost("for-booking")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<FnbOrderResponse>>> CreateForBooking(
        [FromBody] CreateFnbOrderForCounterRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        if (staffId == Guid.Empty)
        {
            return BadRequest(ApiResponse<FnbOrderResponse>.Fail(CommonMessages.InvalidToken));
        }

        var order = await fnbOrderService.CreateForCounterAsync(request, staffId, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = order.Id },
            ApiResponse<FnbOrderResponse>.Success(order, FnbOrderMessages.CounterOrderCreatedSuccess));
    }

    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<FnbOrderResponse>>> UpdateStatus(
        Guid id,
        [FromBody] UpdateFnbOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        var order = await fnbOrderService.UpdateStatusAsync(id, request, cancellationToken);
        return order is null
            ? NotFound(ApiResponse<FnbOrderResponse>.Fail(FnbOrderMessages.NotFound))
            : Ok(ApiResponse<FnbOrderResponse>.Success(order, FnbOrderMessages.UpdatedSuccess));
    }

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userId, out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;
    }
}
