using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Cinemas;
using FluentValidation;

namespace CinemaSystem.API.Validators.Cinemas;

public sealed class CreateCinemaRequestValidator : AbstractValidator<CreateCinemaRequest>
{
    private static readonly string[] ValidStatuses = ["ACTIVE", "INACTIVE"];

    public CreateCinemaRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(CinemaMessages.Required)
            .MaximumLength(150).WithMessage(CinemaMessages.MaxLengthExceeded);

        RuleFor(x => x.Address)
            .NotEmpty().WithMessage(CinemaMessages.Required);

        RuleFor(x => x.City)
            .NotEmpty().WithMessage(CinemaMessages.Required)
            .MaximumLength(100).WithMessage(CinemaMessages.MaxLengthExceeded);

        RuleFor(x => x.Phone)
            .MaximumLength(20).WithMessage(CinemaMessages.MaxLengthExceeded)
            .When(x => !string.IsNullOrWhiteSpace(x.Phone));

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage(CinemaMessages.Required)
            .Must(status => ValidStatuses.Contains(status.Trim().ToUpperInvariant()))
            .WithMessage(CinemaMessages.InvalidStatus);
    }
}
