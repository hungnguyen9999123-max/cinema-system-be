using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Auth;
using FluentValidation;

namespace CinemaSystem.API.Validators.Auth;

public sealed class VerifyEmailRequestValidator : AbstractValidator<VerifyEmailRequestDto>
{
    public VerifyEmailRequestValidator()
    {
        RuleFor(x => x.Token)
            .NotEmpty().WithMessage(CommonMessages.Required);
    }
}
