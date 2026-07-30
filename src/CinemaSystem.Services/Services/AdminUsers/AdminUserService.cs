using AutoMapper;
using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.AdminUsers;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;

namespace CinemaSystem.Services.Services.AdminUsers;

/// <summary>
/// Implements administrative business rules for user-account management.
/// </summary>
public sealed class AdminUserService(
    IAdminUserRepository adminUserRepository,
    IMapper mapper) : IAdminUserService
{
    private const string ActiveStatus = "ACTIVE";
    private const string LockedStatus = "LOCKED";
    private const string DisabledStatus = "DISABLED";

    /// <inheritdoc />
    public async Task<PagedResult<UserResponseDto>> GetUsersAsync(
        UserQueryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var role = NormalizeRole(request.Role);
        var status = NormalizeStatus(request.Status);
        var (users, totalCount) = await adminUserRepository.GetPagedAsync(
            request.Keyword,
            role,
            status,
            request.IsEmailVerified,
            request.Page,
            request.PageSize,
            cancellationToken);

        var totalPages = totalCount == 0
            ? 0
            : (int)Math.Ceiling(totalCount / (double)request.PageSize);

        return new PagedResult<UserResponseDto>(
            mapper.Map<List<UserResponseDto>>(users),
            request.Page,
            request.PageSize,
            totalCount,
            totalPages);
    }

    /// <inheritdoc />
    public async Task<UserDetailResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await adminUserRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? null : mapper.Map<UserDetailResponseDto>(user);
    }

    /// <inheritdoc />
    public async Task<UserDetailResponseDto?> UpdateAsync(
        Guid id,
        UpdateUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await adminUserRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        mapper.Map(request, user);
        user.UpdatedAt = DateTime.UtcNow;
        await SaveUserAsync(user, cancellationToken);
        return mapper.Map<UserDetailResponseDto>(user);
    }

    /// <inheritdoc />
    public async Task<UserDetailResponseDto?> ChangeRoleAsync(
        Guid id,
        ChangeRoleRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var user = await adminUserRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.Role = NormalizeRole(request.Role)!;
        user.UpdatedAt = DateTime.UtcNow;
        await SaveUserAsync(user, cancellationToken);
        return mapper.Map<UserDetailResponseDto>(user);
    }

    /// <inheritdoc />
    public async Task<UserDetailResponseDto?> LockAsync(
        Guid id,
        Guid currentAdminId,
        LockUserRequestDto request,
        CancellationToken cancellationToken = default)
    {
        EnsureNotSelf(id, currentAdminId, "lock");

        var user = await adminUserRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        user.Status = LockedStatus;
        user.LockedUntil = now.AddDays(request.Days);
        user.UpdatedAt = now;
        await SaveUserAsync(user, cancellationToken);
        return mapper.Map<UserDetailResponseDto>(user);
    }

    /// <inheritdoc />
    public async Task<UserDetailResponseDto?> UnlockAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var user = await adminUserRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.Status = ActiveStatus;
        user.LockedUntil = null;
        user.FailedLoginCount = 0;
        user.UpdatedAt = DateTime.UtcNow;
        await SaveUserAsync(user, cancellationToken);
        return mapper.Map<UserDetailResponseDto>(user);
    }

    /// <inheritdoc />
    public async Task<UserDetailResponseDto?> DisableAsync(
        Guid id,
        Guid currentAdminId,
        CancellationToken cancellationToken = default)
    {
        EnsureNotSelf(id, currentAdminId, "disable");

        var user = await adminUserRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return null;
        }

        user.Status = DisabledStatus;
        user.UpdatedAt = DateTime.UtcNow;
        await SaveUserAsync(user, cancellationToken);
        return mapper.Map<UserDetailResponseDto>(user);
    }

    private async Task SaveUserAsync(User user, CancellationToken cancellationToken)
    {
        await adminUserRepository.UpdateAsync(user, cancellationToken);
        await adminUserRepository.SaveChangesAsync(cancellationToken);
    }

    private static string? NormalizeRole(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        if (!Enum.TryParse<UserRole>(role.Trim(), true, out var parsedRole))
        {
            throw new InvalidOperationException("The role is invalid.");
        }

        return parsedRole.ToString();
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return null;
        }

        var normalizedStatus = status.Trim().ToUpperInvariant();
        return normalizedStatus is ActiveStatus or LockedStatus or DisabledStatus
            ? normalizedStatus
            : throw new InvalidOperationException("The status is invalid.");
    }

    private static void EnsureNotSelf(Guid targetUserId, Guid currentAdminId, string action)
    {
        if (targetUserId == currentAdminId)
        {
            throw new ForbiddenAccessException($"Administrators cannot {action} their own account.");
        }
    }
}
