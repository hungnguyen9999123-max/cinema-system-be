using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Promotions;
using FluentValidation;

namespace CinemaSystem.API.Validators.Promotions;

/// <summary>
/// Validates promotion creation requests.
/// </summary>
public sealed class CreatePromotionRequestValidator : AbstractValidator<CreatePromotionRequest>
{
    public CreatePromotionRequestValidator()
    {
        // RuleFor(x => x.PromoCode)
        //     .NotEmpty().WithMessage(PromotionMessages.CodeRequired)
        //     .MaximumLength(50)
        //     .MustAsync(async (promoCode, cancellationToken) =>
        //         !await promotionRepository.CodeExistsAsync(promoCode.Trim().ToUpperInvariant(), null, cancellationToken))
        //     .WithMessage(PromotionMessages.CodeAlreadyExists);
        RuleFor(x => x.PromoCode)
    .NotEmpty().WithMessage(PromotionMessages.CodeRequired)
    .MaximumLength(50);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(PromotionMessages.NameRequired)
            .MaximumLength(100);

        RuleFor(x => x.DiscountType)
            .NotEmpty().WithMessage(PromotionMessages.DiscountTypeRequired)
            .Must(type => IsValidDiscountType(type))
            .WithMessage(PromotionMessages.InvalidDiscountType);

        RuleFor(x => x.DiscountValue)
            .GreaterThan(0).WithMessage(PromotionMessages.InvalidDiscountValue)
            .Must((request, value) => request.DiscountType.Equals("PERCENTAGE", StringComparison.OrdinalIgnoreCase)
                ? value <= 100
                : true)
            .WithMessage(PromotionMessages.InvalidPercentageDiscount);

        RuleFor(x => x.MinOrderAmt)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MinOrderAmt.HasValue)
            .WithMessage(PromotionMessages.InvalidMinOrderAmount);

        RuleFor(x => x.ValidTo)
            .Must((request, validTo) => request.ValidFrom <= validTo)
            .WithMessage(PromotionMessages.InvalidDateRange);

        RuleFor(x => x.UsageLimit)
            .GreaterThan(0)
            .When(x => x.UsageLimit.HasValue)
            .WithMessage(PromotionMessages.InvalidUsageLimit);
    }

    private static bool IsValidDiscountType(string discountType)
    {
        return discountType.Equals("PERCENTAGE", StringComparison.OrdinalIgnoreCase)
            || discountType.Equals("FIXED_AMOUNT", StringComparison.OrdinalIgnoreCase)
            || discountType.Equals("AMOUNT", StringComparison.OrdinalIgnoreCase);
    }
}
