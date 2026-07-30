using System.Security.Claims;
using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.AdminUsers;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Services.Services.AdminUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

/// <summary>
/// Provides administrator-only APIs for managing user accounts.
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public sealed class AdminUsersController(IAdminUserService adminUserService) : ControllerBase
{
    /// <summary>
    /// Gets a filtered, paged list of users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<UserResponseDto>>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PagedResult<UserResponseDto>>>> GetUsers(
        [FromQuery] UserQueryRequestDto request,
        CancellationToken cancellationToken)
    {
        var users = await adminUserService.GetUsersAsync(request, cancellationToken);
        return Ok(ApiResponse<PagedResult<UserResponseDto>>.Success(users, "Users retrieved successfully."));
    }

    /// <summary>
    /// Gets detailed information for a user.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailResponseDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await adminUserService.GetByIdAsync(id, cancellationToken);
        return user is null
            ? NotFound(ApiResponse<UserDetailResponseDto>.Fail("User was not found."))
            : Ok(ApiResponse<UserDetailResponseDto>.Success(user, "User retrieved successfully."));
    }

    /// <summary>
    /// Updates a user's permitted profile fields.
    /// </summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailResponseDto>>> Update(
        Guid id,
        [FromBody] UpdateUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await adminUserService.UpdateAsync(id, request, cancellationToken);
        return user is null
            ? NotFound(ApiResponse<UserDetailResponseDto>.Fail("User was not found."))
            : Ok(ApiResponse<UserDetailResponseDto>.Success(user, "User updated successfully."));
    }

    /// <summary>
    /// Changes a user's role.
    /// </summary>
    [HttpPatch("{id:guid}/role")]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailResponseDto>>> ChangeRole(
        Guid id,
        [FromBody] ChangeRoleRequestDto request,
        CancellationToken cancellationToken)
    {
        var user = await adminUserService.ChangeRoleAsync(id, request, cancellationToken);
        return user is null
            ? NotFound(ApiResponse<UserDetailResponseDto>.Fail("User was not found."))
            : Ok(ApiResponse<UserDetailResponseDto>.Success(user, "User role changed successfully."));
    }

    /// <summary>
    /// Locks a user account for the requested number of days.
    /// </summary>
    [HttpPatch("{id:guid}/lock")]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailResponseDto>>> Lock(
        Guid id,
        [FromBody] LockUserRequestDto request,
        CancellationToken cancellationToken)
    {
        var currentAdminId = GetCurrentUserId();
        if (currentAdminId is null)
        {
            return Unauthorized(ApiResponse<UserDetailResponseDto>.Fail("Unauthorized access."));
        }

        var user = await adminUserService.LockAsync(id, currentAdminId.Value, request, cancellationToken);
        return user is null
            ? NotFound(ApiResponse<UserDetailResponseDto>.Fail("User was not found."))
            : Ok(ApiResponse<UserDetailResponseDto>.Success(user, "User account locked successfully."));
    }

    /// <summary>
    /// Unlocks a user account and clears failed-login attempts.
    /// </summary>
    [HttpPatch("{id:guid}/unlock")]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailResponseDto>>> Unlock(
        Guid id,
        CancellationToken cancellationToken)
    {
        var user = await adminUserService.UnlockAsync(id, cancellationToken);
        return user is null
            ? NotFound(ApiResponse<UserDetailResponseDto>.Fail("User was not found."))
            : Ok(ApiResponse<UserDetailResponseDto>.Success(user, "User account unlocked successfully."));
    }

    /// <summary>
    /// Disables a user account without deleting its data.
    /// </summary>
    [HttpPatch("{id:guid}/disable")]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<UserDetailResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDetailResponseDto>>> Disable(
        Guid id,
        CancellationToken cancellationToken)
    {
        var currentAdminId = GetCurrentUserId();
        if (currentAdminId is null)
        {
            return Unauthorized(ApiResponse<UserDetailResponseDto>.Fail("Unauthorized access."));
        }

        var user = await adminUserService.DisableAsync(id, currentAdminId.Value, cancellationToken);
        return user is null
            ? NotFound(ApiResponse<UserDetailResponseDto>.Fail("User was not found."))
            : Ok(ApiResponse<UserDetailResponseDto>.Success(user, "User account disabled successfully."));
    }

    private Guid? GetCurrentUserId()
    {
        var rawUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(rawUserId, out var userId) ? userId : null;
    }
}
