using CinemaSystem.Common.DTOs.Auth;
using FluentValidation;

namespace CinemaSystem.API.Validators.Auth;

// public class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
// {
//     public ResetPasswordRequestDtoValidator()
//     {
//         RuleFor(x => x.Token)
//             .NotEmpty().WithMessage("Token is required.");

//         RuleFor(x => x.NewPassword)
//             .NotEmpty().WithMessage("New password is required.")
//             .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
//             .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
//             .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
//             .Matches("[0-9]").WithMessage("Password must contain at least one number.")
//             .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character.");

//         RuleFor(x => x.ConfirmPassword)
//             .NotEmpty().WithMessage("Confirm password is required.")
//             .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
//     }
// }
public class ResetPasswordRequestDtoValidator : AbstractValidator<ResetPasswordRequestDto>
{
    public ResetPasswordRequestDtoValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Token is required.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Confirm password is required.")
            .Equal(x => x.NewPassword).WithMessage("Passwords do not match.");
    }
}
