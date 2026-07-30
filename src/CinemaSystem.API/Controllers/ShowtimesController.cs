using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.DTOs.Showtimes;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.Common.Enums;
using CinemaSystem.Services.Services.Showtimes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/showtimes")]
public sealed class ShowtimesController(IShowtimeService showtimeService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<PagedResult<ShowtimeResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<ShowtimeResponse>>>> Search([FromQuery] ShowtimeSearchRequest request, CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResult<ShowtimeResponse>>.Success(
            await showtimeService.SearchAsync(request, cancellationToken),
            ShowtimeMessages.ShowtimesRetrievedSuccessfully));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<ShowtimeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ShowtimeResponse>>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var showtime = await showtimeService.GetByIdAsync(id, cancellationToken);
        return showtime is null
            ? NotFound(ApiResponse<ShowtimeResponse>.Fail(ShowtimeMessages.ShowtimeNotFound))
            : Ok(ApiResponse<ShowtimeResponse>.Success(showtime, ShowtimeMessages.ShowtimeRetrievedSuccessfully));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ProducesResponseType<ApiResponse<ShowtimeResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<ShowtimeResponse>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<ShowtimeResponse>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ShowtimeResponse>>> Create([FromBody] CreateShowtimeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var createdBy = GetCurrentUserId();
            var showtime = await showtimeService.CreateAsync(request, createdBy, cancellationToken);
            return CreatedAtAction(nameof(GetById), new { id = showtime.Id }, ApiResponse<ShowtimeResponse>.Success(showtime, ShowtimeMessages.ShowtimeCreatedSuccessfully));
        }
        catch (BusinessConflictException ex)
        {
            return Conflict(ApiResponse<ShowtimeResponse>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ShowtimeResponse>.Fail(ex.Message));
        }
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ProducesResponseType<ApiResponse<ShowtimeResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<ShowtimeResponse>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<ShowtimeResponse>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<ShowtimeResponse>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<ShowtimeResponse>>> Update(Guid id, [FromBody] UpdateShowtimeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var showtime = await showtimeService.UpdateAsync(id, request, cancellationToken);
            return showtime is null
                ? NotFound(ApiResponse<ShowtimeResponse>.Fail(ShowtimeMessages.ShowtimeNotFound))
                : Ok(ApiResponse<ShowtimeResponse>.Success(showtime, ShowtimeMessages.ShowtimeUpdatedSuccessfully));
        }
        catch (BusinessConflictException ex)
        {
            return Conflict(ApiResponse<ShowtimeResponse>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<ShowtimeResponse>.Fail(ex.Message));
        }
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(Guid id, CancellationToken cancellationToken)
    {
        var result = await showtimeService.DeleteAsync(id, cancellationToken);
        return result switch
        {
            DeleteShowtimeResult.NotFound => NotFound(ApiResponse<object?>.Fail(ShowtimeMessages.ShowtimeNotFound)),
            DeleteShowtimeResult.Cancelled => Ok(ApiResponse<object?>.Success(null, ShowtimeMessages.ShowtimeCancelledBecauseHasBookingHistory)),
            DeleteShowtimeResult.Deleted => Ok(ApiResponse<object?>.Success(null, ShowtimeMessages.ShowtimeDeletedSuccessfully)),
            _ => Ok(ApiResponse<object?>.Success(null, ShowtimeMessages.ShowtimeDeletedSuccessfully))
        };
    }

    private Guid GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (!Guid.TryParse(raw, out var userId))
        {
            throw new UnauthorizedAccessException(ShowtimeMessages.UserIdClaimMissingOrInvalid);
        }

        return userId;
    }
}

