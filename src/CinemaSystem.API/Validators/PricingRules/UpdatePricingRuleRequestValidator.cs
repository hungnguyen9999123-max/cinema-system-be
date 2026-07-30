using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.PricingRules;
using FluentValidation;

namespace CinemaSystem.API.Validators.PricingRules;

public sealed class UpdatePricingRuleRequestValidator : AbstractValidator<UpdatePricingRuleRequest>
{
    public UpdatePricingRuleRequestValidator()
    {
        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage(PricingRuleMessages.InvalidBasePrice);

        RuleFor(x => x.TimeMultiplier)
            .GreaterThan(0).WithMessage(PricingRuleMessages.InvalidTimeMultiplier);
    }
}
