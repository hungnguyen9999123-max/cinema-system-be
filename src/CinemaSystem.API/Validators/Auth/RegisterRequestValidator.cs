using CinemaSystem.Common.DTOs.Auth;
using CinemaSystem.DAL.Interfaces;
using FluentValidation;

namespace CinemaSystem.API.Validators.Auth;

public sealed class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator(IUserRepository userRepository)
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("FullName is required.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email is invalid.");
            // .MustAsync(async (email, cancellationToken) =>
            // {
            //     var existingUser = await userRepository.GetByEmailAsync(email.Trim());
            //     return existingUser is null;
            // })
            // .WithMessage("Email already exists.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("ConfirmPassword is required.")
            .Equal(x => x.Password).WithMessage("Password and ConfirmPassword must match.");
    }
}
