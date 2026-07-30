using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Auth;
using FluentValidation;

namespace CinemaSystem.API.Validators.Auth;

public sealed class GoogleLoginRequestValidator : AbstractValidator<GoogleLoginRequest>
{
    public GoogleLoginRequestValidator()
    {
        RuleFor(x => x.IdToken)
            .NotEmpty()
            .WithMessage(CommonMessages.Required);
    }
}
