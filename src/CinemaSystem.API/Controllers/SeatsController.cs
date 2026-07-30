using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Rooms;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.Enums;
using CinemaSystem.Services.Services.Rooms;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/seats")]
public sealed class SeatsController(ISeatService seatService) : ControllerBase
{
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResponse<SeatResponse>>> Update(Guid id, [FromBody] UpdateSeatRequest request, CancellationToken cancellationToken)
    {
        var seat = await seatService.UpdateSeatAsync(id, request, cancellationToken);
        return seat is null
            ? NotFound(ApiResponse<SeatResponse>.Fail(RoomMessages.SeatNotFound))
            : Ok(ApiResponse<SeatResponse>.Success(seat, RoomMessages.SeatUpdatedSuccessfully));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await seatService.DeleteSeatAsync(id, cancellationToken);
        return result switch
        {
            DeleteSeatResult.NotFound => NotFound(ApiResponse<object?>.Fail(RoomMessages.SeatNotFound)),
            DeleteSeatResult.Disabled => Ok(ApiResponse<object?>.Success(null, RoomMessages.SeatDisabledBecauseHasBookingHistory)),
            DeleteSeatResult.Deleted => Ok(ApiResponse<object?>.Success(null, RoomMessages.SeatDeletedSuccessfully)),
            _ => Ok(ApiResponse<object?>.Success(null, RoomMessages.SeatDeletedSuccessfully))
        };
    }
}
