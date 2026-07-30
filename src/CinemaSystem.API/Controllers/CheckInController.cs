using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Fnb;
using CinemaSystem.Common.DTOs.QrTickets;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.Fnb;
using CinemaSystem.Services.Services.QrTickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager,Staff")]
[Route("api/checkin")]
public sealed class CheckInController(
    IQrTicketService qrTicketService,
    IFnbOrderService fnbOrderService) : ControllerBase
{
    [HttpPost("validate")]
    [ProducesResponseType<ApiResponse<VerifyQrResponseDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<VerifyQrResponseDto>>> Validate(
        [FromBody] VerifyQrRequestDto request,
        CancellationToken cancellationToken)
    {
        var result = await qrTicketService.ValidateQrAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPost("verify")]
    [ProducesResponseType<ApiResponse<VerifyQrResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<VerifyQrResponseDto>>> Verify(
        [FromBody] VerifyQrRequestDto request,
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        if (staffId is null)
        {
            return Ok(ApiResponse<VerifyQrResponseDto>.Fail(QrTicketMessages.UserIdClaimMissingOrInvalid));
        }

        var result = await qrTicketService.CheckInAsync(request, staffId.Value, cancellationToken);
        return Ok(result);
    }

    [HttpGet("history")]
    [ProducesResponseType<ApiResponse<PagedResult<CheckInHistoryItemDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CheckInHistoryItemDto>>>> GetHistory(
        [FromQuery] CheckInHistorySearchRequest request,
        CancellationToken cancellationToken)
    {
        var result = await qrTicketService.GetCheckInHistoryAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<CheckInHistoryItemDto>>.Success(
            result,
            QrTicketMessages.CheckInHistoryRetrievedSuccessfully));
    }

    [HttpGet("fnb-orders/{orderId:guid}")]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FnbOrderResponse>>> GetFnbOrder(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var order = await fnbOrderService.GetByIdAsync(orderId, cancellationToken);
        if (order is null)
        {
            throw new KeyNotFoundException("Fnb order not found.");
        }

        return Ok(ApiResponse<FnbOrderResponse>.Success(order, "Fnb order retrieved successfully."));
    }

    [HttpPut("fnb-orders/{orderId:guid}/fulfill")]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<FnbOrderResponse>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<FnbOrderResponse>>> FulfillFnbOrder(
        Guid orderId,
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        if (staffId is null)
        {
            return Ok(ApiResponse<FnbOrderResponse>.Fail(QrTicketMessages.UserIdClaimMissingOrInvalid));
        }

        var updated = await fnbOrderService.UpdateStatusAsync(
            orderId,
            new UpdateFnbOrderStatusRequest { Status = "SERVED" },
            cancellationToken);

        if (updated is null)
        {
            throw new KeyNotFoundException("Fnb order not found.");
        }

        return Ok(ApiResponse<FnbOrderResponse>.Success(updated, "Fnb order marked as served."));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(raw, out var userId))
        {
            return null;
        }

        return userId;
    }
}
