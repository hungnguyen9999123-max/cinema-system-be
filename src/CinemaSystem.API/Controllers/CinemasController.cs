using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Cinemas;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.Enums;
using CinemaSystem.Services.Services.Cinemas;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Authorize]
[Route("api/cinemas")]
public sealed class CinemasController(ICinemaService cinemaService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<PagedResult<CinemaResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<CinemaResponse>>>> Search(
        [FromQuery] CinemaSearchRequest request,
        CancellationToken cancellationToken)
    {
        return Ok(ApiResponse<PagedResult<CinemaResponse>>.Success(
            await cinemaService.SearchAsync(request, cancellationToken),
            CinemaMessages.RetrievedSuccess));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<CinemaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CinemaResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var cinema = await cinemaService.GetByIdAsync(id, cancellationToken);
        return cinema is null
            ? NotFound(ApiResponse<CinemaResponse>.Fail(CinemaMessages.NotFound))
            : Ok(ApiResponse<CinemaResponse>.Success(cinema, CinemaMessages.DetailRetrievedSuccess));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ProducesResponseType<ApiResponse<CinemaResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CinemaResponse>>> Create(
        [FromBody] CreateCinemaRequest request,
        CancellationToken cancellationToken)
    {
        var cinema = await cinemaService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = cinema.Id },
            ApiResponse<CinemaResponse>.Success(cinema, CinemaMessages.CreatedSuccess));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ProducesResponseType<ApiResponse<CinemaResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<CinemaResponse>>> Update(
        Guid id,
        [FromBody] UpdateCinemaRequest request,
        CancellationToken cancellationToken)
    {
        var cinema = await cinemaService.UpdateAsync(id, request, cancellationToken);
        return cinema is null
            ? NotFound(ApiResponse<CinemaResponse>.Fail(CinemaMessages.NotFound))
            : Ok(ApiResponse<CinemaResponse>.Success(cinema, CinemaMessages.UpdatedSuccess));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await cinemaService.DeleteAsync(id, cancellationToken);
        return result switch
        {
            DeleteCinemaResult.NotFound => NotFound(ApiResponse<object?>.Fail(CinemaMessages.NotFound)),
            DeleteCinemaResult.Deleted => Ok(ApiResponse<object?>.Success(null, CinemaMessages.DeletedSuccess)),
            _ => Ok(ApiResponse<object?>.Success(null, CinemaMessages.DeletedSuccess))
        };
    }
}
