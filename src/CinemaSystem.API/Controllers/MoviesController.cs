using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Movies;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.Enums;
using CinemaSystem.Services.Services.Movies;
using CinemaSystem.Services.Services.Recommendations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api/movies")]
public sealed class MoviesController(
    IMovieService movieService,
    IRecommendationService recommendationService) : ControllerBase
{
    [HttpGet("recommendations")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<RecommendationResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<RecommendationResponse>>> GetRecommendations(
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var userId = GetUserIdIfLoggedIn();
        var response = await recommendationService.GetRecommendationsAsync(userId, limit, cancellationToken);
        return Ok(ApiResponse<RecommendationResponse>.Success(response, CommonMessages.Retrieved));
    }

    private Guid? GetUserIdIfLoggedIn()
    {
        var raw = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out var parsed) ? parsed : null;
    }

    [HttpGet("search")]
    [ProducesResponseType<ApiResponse<IReadOnlyList<MovieSearchResponse>>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<MovieSearchResponse>>>> SearchByTitle(
        [FromQuery] string? keyword,
        CancellationToken cancellationToken)
    {
        var movies = await movieService.SearchByTitleAsync(keyword, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<MovieSearchResponse>>.Success(
            movies,
            CommonMessages.Retrieved));
    }

    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<MovieResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<MovieResponse>>>> Search(
        [FromQuery] MovieSearchRequest request,
        CancellationToken cancellationToken)
    {
        if (request.ReleaseFrom > request.ReleaseTo)
        {
            return BadRequest(ApiResponse<PagedResult<MovieResponse>>.Fail(CommonMessages.EndDateAfterStartDate));
        }

        return Ok(ApiResponse<PagedResult<MovieResponse>>.Success(
            await movieService.SearchAsync(request, cancellationToken),
            CommonMessages.Retrieved));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<MovieResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovieResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var movie = await movieService.GetByIdAsync(id, cancellationToken);
        return movie is null
            ? NotFound(ApiResponse<MovieResponse>.Fail(CommonMessages.NotFound))
            : Ok(ApiResponse<MovieResponse>.Success(movie, CommonMessages.Retrieved));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ProducesResponseType<ApiResponse<MovieResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<MovieResponse>>> Create(
        CreateMovieRequest request,
        CancellationToken cancellationToken)
    {
        if (request.CreatedBy == Guid.Empty)
        {
            return BadRequest(ApiResponse<MovieResponse>.Fail(CommonMessages.Required));
        }

        var movie = await movieService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = movie.Id }, ApiResponse<MovieResponse>.Success(movie, CommonMessages.Created));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager,Staff")]
    [ProducesResponseType<ApiResponse<MovieResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<MovieResponse>>> Update(
        Guid id,
        UpdateMovieRequest request,
        CancellationToken cancellationToken)
    {
        var movie = await movieService.UpdateAsync(id, request, cancellationToken);
        return movie is null
            ? NotFound(ApiResponse<MovieResponse>.Fail(CommonMessages.NotFound))
            : Ok(ApiResponse<MovieResponse>.Success(movie, CommonMessages.Updated));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await movieService.DeleteAsync(id, cancellationToken);
        if (result == DeleteMovieResult.NotFound)
        {
            return NotFound(ApiResponse<object?>.Fail(CommonMessages.NotFound));
        }

        if (result == DeleteMovieResult.HasShowtimes)
        {
            return Conflict(ApiResponse<object?>.Fail(CommonMessages.CannotDelete));
        }

        return Ok(ApiResponse<object?>.Success(null, CommonMessages.Deleted));
    }
}
