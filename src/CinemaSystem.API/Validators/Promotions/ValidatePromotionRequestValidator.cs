using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Promotions;
using FluentValidation;

namespace CinemaSystem.API.Validators.Promotions;

/// <summary>
/// Validates promotion application requests.
/// </summary>
public sealed class ValidatePromotionRequestValidator : AbstractValidator<ValidatePromotionRequest>
{
    public ValidatePromotionRequestValidator()
    {
        RuleFor(x => x.PromoCode)
            .NotEmpty().WithMessage(PromotionMessages.CodeRequired)
            .MaximumLength(50);

        RuleFor(x => x.BookingAmount)
            .GreaterThan(0).WithMessage(PromotionMessages.InvalidBookingAmount);
    }
}
