using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Auth;
using FluentValidation;

namespace CinemaSystem.API.Validators.Auth;

public sealed class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithMessage(CommonMessages.Required);
    }
}
