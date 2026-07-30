using CinemaSystem.Common.DTOs.QrTickets;
using FluentValidation;

namespace CinemaSystem.API.Validators.QrTickets;

public class CheckInHistorySearchRequestValidator : AbstractValidator<CheckInHistorySearchRequest>
{
    public CheckInHistorySearchRequestValidator()
    {
        RuleFor(request => request.Page)
            .GreaterThanOrEqualTo(1);

        RuleFor(request => request.PageSize)
            .InclusiveBetween(1, 100);

        RuleFor(request => request)
            .Must(request => !request.From.HasValue || !request.To.HasValue || request.From <= request.To)
            .WithMessage("From must be less than or equal to To.");
    }
}
