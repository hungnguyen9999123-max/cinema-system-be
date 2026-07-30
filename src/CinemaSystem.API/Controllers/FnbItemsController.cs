using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Fnb;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.Enums;
using CinemaSystem.Services.Services.Fnb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api/fnb-items")]
public sealed class FnbItemsController(IFnbItemService fnbItemService) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<PagedResult<FnbItemResponse>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<FnbItemResponse>>>> Search(
        [FromQuery] FnbItemSearchRequest request,
        CancellationToken cancellationToken)
    {
        var items = await fnbItemService.SearchAsync(
            request,
            activeOnly: ShouldViewActiveOnly(),
            cancellationToken);

        return Ok(ApiResponse<PagedResult<FnbItemResponse>>.Success(items, FnbMessages.RetrievedSuccess));
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<ApiResponse<FnbItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<FnbItemResponse>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await fnbItemService.GetByIdAsync(
            id,
            activeOnly: ShouldViewActiveOnly(),
            cancellationToken);

        return item is null
            ? NotFound(ApiResponse<FnbItemResponse>.Fail(FnbMessages.NotFound))
            : Ok(ApiResponse<FnbItemResponse>.Success(item, FnbMessages.DetailRetrievedSuccess));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType<ApiResponse<FnbItemResponse>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<FnbItemResponse>>> Create(
        [FromBody] CreateFnbItemRequest request,
        CancellationToken cancellationToken)
    {
        var createdBy = GetCurrentUserId();
        if (createdBy == Guid.Empty)
        {
            return BadRequest(ApiResponse<FnbItemResponse>.Fail(CommonMessages.InvalidToken));
        }

        var item = await fnbItemService.CreateAsync(request, createdBy, cancellationToken);

        return CreatedAtAction(
            nameof(GetById),
            new { id = item.Id },
            ApiResponse<FnbItemResponse>.Success(item, FnbMessages.CreatedSuccess));
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType<ApiResponse<FnbItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<FnbItemResponse>>> Update(
        Guid id,
        [FromBody] UpdateFnbItemRequest request,
        CancellationToken cancellationToken)
    {
        var item = await fnbItemService.UpdateAsync(id, request, cancellationToken);
        return item is null
            ? NotFound(ApiResponse<FnbItemResponse>.Fail(FnbMessages.NotFound))
            : Ok(ApiResponse<FnbItemResponse>.Success(item, FnbMessages.UpdatedSuccess));
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = "Admin,Manager")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<object?>>> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await fnbItemService.DeleteAsync(id, cancellationToken);
        return result switch
        {
            DeleteFnbItemResult.NotFound => NotFound(ApiResponse<object?>.Fail(FnbMessages.NotFound)),
            DeleteFnbItemResult.Deleted => Ok(ApiResponse<object?>.Success(null, FnbMessages.DeletedSuccess)),
            _ => Ok(ApiResponse<object?>.Success(null, FnbMessages.DeletedSuccess))
        };
    }

    private bool ShouldViewActiveOnly()
        => !User.IsInRole(UserRole.Admin.ToString()) &&
           !User.IsInRole(UserRole.Manager.ToString());

    private Guid GetCurrentUserId()
    {
        var userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier);

        return Guid.TryParse(userId, out var parsedUserId)
            ? parsedUserId
            : Guid.Empty;
    }
}
