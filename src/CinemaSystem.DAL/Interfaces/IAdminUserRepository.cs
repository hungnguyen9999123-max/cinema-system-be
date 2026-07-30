using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

/// <summary>
/// Provides persistence operations used by the administrative user-management module.
/// </summary>
public interface IAdminUserRepository
{
    /// <summary>
    /// Gets a page of users matching the supplied filters and the total match count.
    /// </summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        string? role,
        string? status,
        bool? isEmailVerified,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a tracked user by identifier for an administrative operation.
    /// </summary>
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a user as modified in the current persistence context.
    /// </summary>
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes to the database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
