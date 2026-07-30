using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.DAL.Repository.Users;

/// <summary>
/// Entity Framework Core implementation of administrative user persistence operations.
/// </summary>
public sealed class AdminUserRepository(CinemaDbContext context) : IAdminUserRepository
{
    /// <inheritdoc />
    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        string? keyword,
        string? role,
        string? status,
        bool? isEmailVerified,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<User> query = context.Users.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(keyword))
        {
            var normalizedKeyword = keyword.Trim();
            query = query.Where(user =>
                user.Email.Contains(normalizedKeyword) ||
                user.FullName.Contains(normalizedKeyword));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(user => user.Role == role);
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(user => user.Status == status);
        }

        if (isEmailVerified.HasValue)
        {
            query = query.Where(user => user.IsEmailVerified == isEmailVerified.Value);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(user => user.CreatedAt)
            .ThenBy(user => user.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (users, totalCount);
    }

    /// <inheritdoc />
    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return context.Users.SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Update(user);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return context.SaveChangesAsync(cancellationToken);
    }
}
