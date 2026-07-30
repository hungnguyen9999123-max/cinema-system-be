using CinemaSystem.Common.DTOs.Payments;
using FluentValidation;
using System;

namespace CinemaSystem.API.Validators.Payments;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequestDto>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(request => request.BookingId)
            .NotEmpty()
            .WithMessage("BookingId is required.");

        RuleFor(request => request.Gateway)
            .MaximumLength(20)
            .Must(gateway => string.IsNullOrWhiteSpace(gateway) ||
                gateway.Trim().Equals("VNPAY", StringComparison.OrdinalIgnoreCase) ||
                gateway.Trim().Equals("WALLET", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Gateway must be VNPAY or WALLET.");
    }
}
