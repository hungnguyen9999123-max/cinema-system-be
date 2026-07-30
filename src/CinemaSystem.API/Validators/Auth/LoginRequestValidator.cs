using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Auth;
using FluentValidation;

namespace CinemaSystem.API.Validators.Auth;

public sealed class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage(CommonMessages.Required)
            .EmailAddress().WithMessage(CommonMessages.InvalidEmail);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(CommonMessages.Required);
    }
}
