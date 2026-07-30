using CinemaSystem.Common.DTOs.Rooms;
using CinemaSystem.Common.Constants;
using FluentValidation;

namespace CinemaSystem.API.Validators.Rooms;

public sealed class GenerateSeatLayoutRequestValidator : AbstractValidator<GenerateSeatLayoutRequest>
{
    public GenerateSeatLayoutRequestValidator()
    {
        RuleFor(x => x.Rows).InclusiveBetween(1, 26);
        RuleFor(x => x.SeatsPerRow).InclusiveBetween(1, 50);

        RuleFor(x => x.DefaultSeatTypeName)
            .NotEmpty();

        When(x => x.Overrides is not null, () =>
        {
            RuleForEach(x => x.Overrides!).ChildRules(overrideRule =>
            {
                overrideRule.RuleFor(x => x.RowFrom)
                    .NotEmpty()
                    .Length(1)
                    .Matches("^[A-Za-z]$")
                    .WithMessage(RoomMessages.SeatRowLetterInvalid);

                overrideRule.RuleFor(x => x.RowTo)
                    .NotEmpty()
                    .Length(1)
                    .Matches("^[A-Za-z]$")
                    .WithMessage(RoomMessages.SeatRowLetterInvalid);

                overrideRule.RuleFor(x => x.ColFrom).GreaterThan(0);
                overrideRule.RuleFor(x => x.ColTo).GreaterThan(0);
                overrideRule.RuleFor(x => x.SeatTypeName)
                    .NotEmpty();
                overrideRule.RuleFor(x => x.Status)
                    .Must(status => status is "ACTIVE" or "DISABLED")
                    .When(x => !string.IsNullOrWhiteSpace(x.Status))
                    .WithMessage("Override status must be ACTIVE or DISABLED.");
            });
        });

        RuleFor(x => x).Custom((request, context) =>
        {
            if (request.Overrides is null)
            {
                return;
            }

            foreach (var overrideValue in request.Overrides)
            {
                var rowFrom = char.ToUpperInvariant(overrideValue.RowFrom[0]);
                var rowTo = char.ToUpperInvariant(overrideValue.RowTo[0]);

                if (rowFrom > rowTo || rowFrom < 'A' || rowTo > (char)('A' + request.Rows - 1))
                {
                    context.AddFailure(nameof(SeatRangeOverride.RowFrom), RoomMessages.SeatLayoutOverrideRangeInvalid);
                }

                if (overrideValue.ColFrom > overrideValue.ColTo || overrideValue.ColTo > request.SeatsPerRow)
                {
                    context.AddFailure(nameof(SeatRangeOverride.ColFrom), RoomMessages.SeatLayoutOverrideColumnInvalid);
                }
            }
        });
    }
}
