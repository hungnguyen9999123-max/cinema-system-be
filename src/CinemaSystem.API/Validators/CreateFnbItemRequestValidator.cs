using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Fnb;
using FluentValidation;

namespace CinemaSystem.API.Validators.Fnb;

public sealed class CreateFnbItemRequestValidator : AbstractValidator<CreateFnbItemRequest>
{
    private static readonly string[] ValidTypes = ["COMBO", "FOOD", "DRINK"];
    private static readonly string[] ValidStatuses = ["ACTIVE", "INACTIVE"];

    public CreateFnbItemRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(FnbMessages.Required)
            .MaximumLength(100).WithMessage(FnbMessages.MaxLengthExceeded);

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage(FnbMessages.Required)
            .Must(type => !string.IsNullOrWhiteSpace(type) && ValidTypes.Contains(type.Trim().ToUpperInvariant()))
            .WithMessage(FnbMessages.InvalidType);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage(FnbMessages.InvalidPrice);

        RuleFor(x => x.ImageUrl)
            .MaximumLength(500).WithMessage(FnbMessages.MaxLengthExceeded)
            .Must(BeValidUrl).WithMessage(FnbMessages.InvalidImageUrl)
            .When(x => !string.IsNullOrWhiteSpace(x.ImageUrl));

        RuleFor(x => x.ImagePublicId)
            .MaximumLength(255).WithMessage(FnbMessages.MaxLengthExceeded)
            .When(x => !string.IsNullOrWhiteSpace(x.ImagePublicId));

        RuleFor(x => x.Status)
            .NotEmpty().WithMessage(FnbMessages.Required)
            .Must(status => !string.IsNullOrWhiteSpace(status) && ValidStatuses.Contains(status.Trim().ToUpperInvariant()))
            .WithMessage(FnbMessages.InvalidStatus);
    }

    private static bool BeValidUrl(string? imageUrl)
        => Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) &&
           (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
}
