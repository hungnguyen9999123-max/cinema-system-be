using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
[Route("api/rooms")]
public sealed class RoomsController(IRoomService roomService, ISeatService seatService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<PagedResult<RoomResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<RoomResponse>>>> Search([FromQuery] RoomSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResult<RoomResponse>>.Success(
            await roomService.SearchAsync(request, cancellationToken),
            RoomMessages.RoomsRetrievedSuccessfully));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<RoomResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<RoomResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var room = await roomService.GetByIdAsync(id, cancellationToken);
        return room is null
            ? NotFound(ApiResponse<RoomResponse>.Fail(RoomMessages.RoomNotFound))
            : Ok(ApiResponse<RoomResponse>.Success(room, RoomMessages.RoomRetrievedSuccessfully));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResponse<RoomResponse>>> Create([FromBody] CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await roomService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = room.Id }, ApiResponse<RoomResponse>.Success(room, RoomMessages.RoomCreatedSuccessfully));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResponse<RoomResponse>>> Update(Guid id, [FromBody] UpdateRoomRequest request, CancellationToken cancellationToken)
    {
        var room = await roomService.UpdateAsync(id, request, cancellationToken);
        return room is null
            ? NotFound(ApiResponse<RoomResponse>.Fail(RoomMessages.RoomNotFound))
            : Ok(ApiResponse<RoomResponse>.Success(room, RoomMessages.RoomUpdatedSuccessfully));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await roomService.DeleteAsync(id, cancellationToken);
        return result switch
        {
            DeleteRoomResult.NotFound => NotFound(ApiResponse<object?>.Fail(RoomMessages.RoomNotFound)),
            DeleteRoomResult.Deleted => Ok(ApiResponse<object?>.Success(null, RoomMessages.RoomDeletedSuccessfully)),
            _ => Ok(ApiResponse<object?>.Success(null, RoomMessages.RoomDeletedSuccessfully))
        };
    }

    [HttpGet("{id:guid}/seat-layout")]
    [ProducesResponseType<ApiResponse<SeatLayoutResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SeatLayoutResponse>>> GetSeatLayout(Guid id, CancellationToken cancellationToken)
    {
        var layout = await seatService.GetLayoutAsync(id, cancellationToken);
        return layout is null
            ? NotFound(ApiResponse<SeatLayoutResponse>.Fail(RoomMessages.RoomNotFound))
            : Ok(ApiResponse<SeatLayoutResponse>.Success(layout, RoomMessages.SeatLayoutRetrievedSuccessfully));
    }

    [HttpPost("{id:guid}/seat-layout")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResponse<SeatLayoutResponse>>> GenerateSeatLayout(Guid id, [FromBody] GenerateSeatLayoutRequest request, CancellationToken cancellationToken)
    {
        var layout = await seatService.GenerateLayoutAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SeatLayoutResponse>.Success(layout, RoomMessages.SeatLayoutGeneratedSuccessfully));
    }

    [HttpPost("{id:guid}/seats")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResponse<SeatResponse>>> CreateSeat(Guid id, [FromBody] CreateSeatRequest request, CancellationToken cancellationToken)
    {
        var seat = await seatService.CreateSeatAsync(id, request, cancellationToken);
        return Ok(ApiResponse<SeatResponse>.Success(seat, RoomMessages.SeatCreatedSuccessfully));
    }
}
