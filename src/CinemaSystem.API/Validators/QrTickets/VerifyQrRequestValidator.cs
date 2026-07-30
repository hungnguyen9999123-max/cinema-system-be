using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.QrTickets;
using FluentValidation;

namespace CinemaSystem.API.Validators.QrTickets;

public class VerifyQrRequestValidator : AbstractValidator<VerifyQrRequestDto>
{
    public VerifyQrRequestValidator()
    {
        RuleFor(request => request.Token)
            .NotEmpty()
            .WithMessage(QrTicketMessages.InvalidToken)
            .MinimumLength(16)
            .MaximumLength(128);
    }
}
