using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.QrTickets;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.QrTickets;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public sealed class QrTicketsController(IQrTicketService qrTicketService) : ControllerBase
{
    [HttpPost("bookings/{bookingId:guid}/tickets/generate")]
    [ProducesResponseType<ApiResponse<BookingTicketsResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BookingTicketsResponseDto>>> GenerateTickets(
        Guid bookingId,
        [FromQuery] GenerateQrRequestDto request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Ok(ApiResponse<BookingTicketsResponseDto>.Fail(QrTicketMessages.UserIdClaimMissingOrInvalid));
        }

        var result = await qrTicketService.GenerateTicketsForBookingAsync(
            bookingId,
            customerId.Value,
            request,
            cancellationToken);
        return Ok(result);
    }

    [HttpGet("bookings/{bookingId:guid}/tickets")]
    [ProducesResponseType<ApiResponse<BookingTicketsResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BookingTicketsResponseDto>>> GetTicketsByBooking(
        Guid bookingId,
        [FromQuery] GenerateQrRequestDto request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Ok(ApiResponse<BookingTicketsResponseDto>.Fail(QrTicketMessages.UserIdClaimMissingOrInvalid));
        }

        var result = await qrTicketService.GetQrByBookingAsync(bookingId, customerId.Value, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("tickets/{ticketId:guid}/qr")]
    [ProducesResponseType<ApiResponse<GenerateQrResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<GenerateQrResponseDto>>> GetTicketQr(
        Guid ticketId,
        [FromQuery] GenerateQrRequestDto request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Ok(ApiResponse<GenerateQrResponseDto>.Fail(QrTicketMessages.UserIdClaimMissingOrInvalid));
        }

        var result = await qrTicketService.GenerateQrAsync(ticketId, customerId.Value, request, cancellationToken);
        return Ok(result);
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
