using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.AdminUsers;

namespace CinemaSystem.Services.Services.AdminUsers;

/// <summary>
/// Provides administrative operations for managing user accounts.
/// </summary>
public interface IAdminUserService
{
    /// <summary>Gets a filtered, paged list of users.</summary>
    Task<PagedResult<UserResponseDto>> GetUsersAsync(
        UserQueryRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Gets detailed information for one user.</summary>
    Task<UserDetailResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Updates a user's permitted profile fields.</summary>
    Task<UserDetailResponseDto?> UpdateAsync(
        Guid id,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Changes a user's role.</summary>
    Task<UserDetailResponseDto?> ChangeRoleAsync(
        Guid id,
        ChangeRoleRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Locks a user's account for the requested number of days.</summary>
    Task<UserDetailResponseDto?> LockAsync(
        Guid id,
        Guid currentAdminId,
        LockUserRequestDto request,
        CancellationToken cancellationToken = default);

    /// <summary>Unlocks a user's account and clears the failed-login count.</summary>
    Task<UserDetailResponseDto?> UnlockAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Disables a user's account without deleting its data.</summary>
    Task<UserDetailResponseDto?> DisableAsync(
        Guid id,
        Guid currentAdminId,
        CancellationToken cancellationToken = default);
}
