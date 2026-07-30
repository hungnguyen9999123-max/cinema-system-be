using CinemaSystem.Common.DTOs.Bookings;
using FluentValidation;

namespace CinemaSystem.API.Validators.Bookings;

public class CreateBookingRequestValidator : AbstractValidator<CreateBookingRequestDto>
{
    public CreateBookingRequestValidator()
    {
        RuleFor(x => x.ShowtimeId)
            .NotEmpty().WithMessage("ShowtimeId is required.");

        RuleFor(x => x.AudienceTypeId)
            .NotEmpty().WithMessage("AudienceTypeId is required.");

        RuleFor(x => x.SeatIds)
            .NotEmpty().WithMessage("At least one seat must be selected.")
            .Must(x => x.Count > 0).WithMessage("SeatIds cannot be empty.");

        RuleFor(x => x.PromotionCode)
            .MaximumLength(50)
            .When(x => !string.IsNullOrWhiteSpace(x.PromotionCode));

        // F&B items are now created together with the booking in
        // BookingService.CreateBookingAsync via ProcessFnbItemsAsync.
        // Each item must reference a valid F&B id and a positive quantity.
        RuleForEach(x => x.FnbItems!)
            .ChildRules(item =>
            {
                item.RuleFor(i => i.ItemId)
                    .NotEmpty().WithMessage("F&B item id is required.");
                item.RuleFor(i => i.Quantity)
                    .GreaterThan(0).WithMessage("F&B quantity must be greater than zero.");
            })
            .When(x => x.FnbItems is { Count: > 0 });

    }
}
