using CinemaSystem.Common.DTOs.Rooms;
using FluentValidation;

namespace CinemaSystem.API.Validators.Rooms;

public sealed class UpdateSeatRequestValidator : AbstractValidator<UpdateSeatRequest>
{
    public UpdateSeatRequestValidator()
    {
        When(x => !string.IsNullOrWhiteSpace(x.SeatTypeName), () =>
        {
            RuleFor(x => x.SeatTypeName!)
                .NotEmpty();
        });
    }
}
