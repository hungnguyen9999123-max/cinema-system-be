using CinemaSystem.Common.DTOs.AdminUsers;
using CinemaSystem.Common.Enums;
using FluentValidation;

namespace CinemaSystem.API.Validators.AdminUsers;

/// <summary>
/// Validates administrative user-list query parameters.
/// </summary>
public sealed class UserQueryRequestDtoValidator : AbstractValidator<UserQueryRequestDto>
{
    /// <summary>
    /// Initializes validation rules for the user-list query.
    /// </summary>
    public UserQueryRequestDtoValidator()
    {
        RuleFor(request => request.Keyword)
            .MaximumLength(255);

        RuleFor(request => request.Role)
            .Must(BeAValidRole)
            .When(request => !string.IsNullOrWhiteSpace(request.Role))
            .WithMessage("Role must be one of: Customer, Staff, Manager, or Admin.");

        RuleFor(request => request.Status)
            .Must(BeAValidStatus)
            .When(request => !string.IsNullOrWhiteSpace(request.Status))
            .WithMessage("Status must be one of: ACTIVE, LOCKED, or DISABLED.");

        RuleFor(request => request.Page)
            .InclusiveBetween(1, 100000);

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100);
    }

    private static bool BeAValidRole(string? role)
    {
        return !string.IsNullOrWhiteSpace(role) &&
               Enum.TryParse<UserRole>(role.Trim(), true, out _);
    }

    private static bool BeAValidStatus(string? status)
    {
        return status?.Trim().ToUpperInvariant() is "ACTIVE" or "LOCKED" or "DISABLED";
    }
}

/// <summary>
/// Validates administrative profile-update requests.
/// </summary>
public sealed class UpdateUserRequestDtoValidator : AbstractValidator<UpdateUserRequestDto>
{
    /// <summary>
    /// Initializes validation rules for a user profile update.
    /// </summary>
    public UpdateUserRequestDtoValidator()
    {
        RuleFor(request => request.FullName)
            .NotEmpty()
            .Must(fullName => !string.IsNullOrWhiteSpace(fullName))
            .MaximumLength(100);

        RuleFor(request => request.Phone)
            .MaximumLength(20)
            .Matches(@"^\+?[0-9\s\-()]{7,20}$")
            .When(request => !string.IsNullOrWhiteSpace(request.Phone))
            .WithMessage("Phone must be a valid phone number.");

        RuleFor(request => request.AvatarUrl)
            .MaximumLength(500)
            .Must(BeAnHttpUrl)
            .When(request => !string.IsNullOrWhiteSpace(request.AvatarUrl))
            .WithMessage("AvatarUrl must be an absolute HTTP or HTTPS URL.");
    }

    private static bool BeAnHttpUrl(string? avatarUrl)
    {
        return Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}

/// <summary>
/// Validates administrative role-change requests.
/// </summary>
public sealed class ChangeRoleRequestDtoValidator : AbstractValidator<ChangeRoleRequestDto>
{
    /// <summary>
    /// Initializes validation rules for a role change.
    /// </summary>
    public ChangeRoleRequestDtoValidator()
    {
        RuleFor(request => request.Role)
            .NotEmpty()
            .Must(role => !string.IsNullOrWhiteSpace(role) &&
                          Enum.TryParse<UserRole>(role.Trim(), true, out _))
            .WithMessage("Role must be one of: Customer, Staff, Manager, or Admin.");
    }
}

/// <summary>
/// Validates administrative account-lock requests.
/// </summary>
public sealed class LockUserRequestDtoValidator : AbstractValidator<LockUserRequestDto>
{
    /// <summary>
    /// Initializes validation rules for an account lock.
    /// </summary>
    public LockUserRequestDtoValidator()
    {
        RuleFor(request => request.Days)
            .InclusiveBetween(1, 365)
            .WithMessage("Days must be between 1 and 365.");
    }
}
