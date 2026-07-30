using CinemaSystem.Common.DTOs.Rooms;
using CinemaSystem.Common.Constants;
using FluentValidation;

namespace CinemaSystem.API.Validators.Rooms;

public sealed class CreateSeatRequestValidator : AbstractValidator<CreateSeatRequest>
{
    public CreateSeatRequestValidator()
    {
        RuleFor(x => x.RowLetter)
            .NotEmpty()
            .Length(1)
            .Matches("^[A-Za-z]$")
            .WithMessage(RoomMessages.SeatRowLetterInvalid);

        RuleFor(x => x.ColNumber)
            .GreaterThan((byte)0);

        RuleFor(x => x.SeatTypeName)
            .NotEmpty();
    }
}
