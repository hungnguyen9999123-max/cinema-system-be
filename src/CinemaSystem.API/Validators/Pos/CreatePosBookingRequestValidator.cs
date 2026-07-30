using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Pos;
using FluentValidation;

namespace CinemaSystem.API.Validators.Pos;

public sealed class CreatePosBookingRequestValidator : AbstractValidator<CreatePosBookingRequest>
{
    public CreatePosBookingRequestValidator()
    {
        RuleFor(x => x.ShowtimeId)
            .NotEmpty()
            .WithMessage(PosMessages.ShowtimeIdRequired);

        RuleFor(x => x.SeatIds)
            .NotNull()
            .WithMessage(PosMessages.SeatIdsRequired)
            .Must(ids => ids is { Count: > 0 })
            .WithMessage(PosMessages.NoSeatsSelected);

        RuleFor(x => x.AudienceTypeId)
            .NotEmpty()
            .WithMessage(PosMessages.AudienceTypeIdRequired);

        RuleFor(x => x.Gateway)
            .NotEmpty()
            .WithMessage(PosMessages.PaymentMethodRequired)
            .Must(gateway =>
                string.Equals(gateway, PosMessages.GatewayCash, StringComparison.OrdinalIgnoreCase)
                || string.Equals(gateway, PosMessages.GatewayVnPay, StringComparison.OrdinalIgnoreCase))
            .WithMessage(PosMessages.InvalidPaymentMethod);
    }
}
