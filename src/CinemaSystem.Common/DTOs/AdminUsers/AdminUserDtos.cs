namespace CinemaSystem.Common.DTOs.AdminUsers;

/// <summary>
/// Represents a user in an administrative list response.
/// Password hashes and other credentials are deliberately excluded.
/// </summary>
public sealed class UserResponseDto
{
    /// <summary>Gets or initializes the user's identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or initializes the user's email address.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets or initializes the user's full name.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Gets or initializes the user's phone number.</summary>
    public string? Phone { get; init; }

    /// <summary>Gets or initializes the URL of the user's avatar.</summary>
    public string? AvatarUrl { get; init; }

    /// <summary>Gets or initializes the user's role.</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Gets or initializes the account status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets or initializes whether the user's email has been verified.</summary>
    public bool IsEmailVerified { get; init; }

    /// <summary>Gets or initializes the last successful login time in UTC.</summary>
    public DateTime? LastLogin { get; init; }

    /// <summary>Gets or initializes the account creation time in UTC.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Gets or initializes the most recent update time in UTC.</summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Represents detailed administrative information for one user.
/// Password hashes and external-provider identifiers are deliberately excluded.
/// </summary>
public sealed class UserDetailResponseDto
{
    /// <summary>Gets or initializes the user's identifier.</summary>
    public Guid Id { get; init; }

    /// <summary>Gets or initializes the user's email address.</summary>
    public string Email { get; init; } = string.Empty;

    /// <summary>Gets or initializes the user's full name.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Gets or initializes the user's phone number.</summary>
    public string? Phone { get; init; }

    /// <summary>Gets or initializes the URL of the user's avatar.</summary>
    public string? AvatarUrl { get; init; }

    /// <summary>Gets or initializes the user's role.</summary>
    public string Role { get; init; } = string.Empty;

    /// <summary>Gets or initializes the account status.</summary>
    public string Status { get; init; } = string.Empty;

    /// <summary>Gets or initializes whether the user's email has been verified.</summary>
    public bool IsEmailVerified { get; init; }

    /// <summary>Gets or initializes the last successful login time in UTC.</summary>
    public DateTime? LastLogin { get; init; }

    /// <summary>Gets or initializes the failed-login counter.</summary>
    public byte FailedLoginCount { get; init; }

    /// <summary>Gets or initializes the UTC time at which the account lock expires.</summary>
    public DateTime? LockedUntil { get; init; }

    /// <summary>Gets or initializes the account provider, such as LOCAL or GOOGLE.</summary>
    public string? Provider { get; init; }

    /// <summary>Gets or initializes the account creation time in UTC.</summary>
    public DateTime CreatedAt { get; init; }

    /// <summary>Gets or initializes the most recent update time in UTC.</summary>
    public DateTime UpdatedAt { get; init; }
}

/// <summary>
/// Defines filtering and pagination options for the administrative user list.
/// </summary>
public sealed class UserQueryRequestDto
{
    /// <summary>Gets or initializes an email or full-name search term.</summary>
    public string? Keyword { get; init; }

    /// <summary>Gets or initializes the role to filter by.</summary>
    public string? Role { get; init; }

    /// <summary>Gets or initializes the account status to filter by.</summary>
    public string? Status { get; init; }

    /// <summary>Gets or initializes whether to filter by email-verification state.</summary>
    public bool? IsEmailVerified { get; init; }

    /// <summary>Gets or initializes the one-based result page number.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Gets or initializes the maximum number of items to return.</summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Defines editable user-profile fields for an administrator.
/// Email and credentials cannot be changed through this request.
/// </summary>
public sealed class UpdateUserRequestDto
{
    /// <summary>Gets or initializes the user's full name.</summary>
    public string FullName { get; init; } = string.Empty;

    /// <summary>Gets or initializes the user's phone number, or <see langword="null"/> to clear it.</summary>
    public string? Phone { get; init; }

    /// <summary>Gets or initializes the user's avatar URL, or <see langword="null"/> to clear it.</summary>
    public string? AvatarUrl { get; init; }
}

/// <summary>
/// Defines the new role assigned to a user.
/// </summary>
public sealed class ChangeRoleRequestDto
{
    /// <summary>Gets or initializes the new role.</summary>
    public string Role { get; init; } = string.Empty;
}

/// <summary>
/// Defines how long an account should remain locked.
/// </summary>
public sealed class LockUserRequestDto
{
    /// <summary>Gets or initializes the number of days for which to lock the account.</summary>
    public int Days { get; init; }
}
