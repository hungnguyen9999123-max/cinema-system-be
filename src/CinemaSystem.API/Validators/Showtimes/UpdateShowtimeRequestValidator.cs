using CinemaSystem.Common.DTOs.Showtimes;
using CinemaSystem.Common.Constants;
using FluentValidation;

namespace CinemaSystem.API.Validators.Showtimes;

public sealed class UpdateShowtimeRequestValidator : AbstractValidator<UpdateShowtimeRequest>
{
  private static readonly HashSet<string> ValidTimeSlots = new(StringComparer.OrdinalIgnoreCase)
  {
    "MORNING", "AFTERNOON", "EVENING", "MIDNIGHT", "PEAK"
  };

  private static readonly HashSet<string> ValidLanguageTypes = new(StringComparer.OrdinalIgnoreCase)
  {
    "DUBBED", "SUBTITLED"
  };

  private static readonly HashSet<string> ValidStatuses = new(StringComparer.OrdinalIgnoreCase)
  {
    "CANCELLED"
  };

  public UpdateShowtimeRequestValidator()
  {
    RuleFor(x => x.MovieId)
      .Must(value => !value.HasValue || value.Value != Guid.Empty)
      .WithMessage("MovieId must not be empty.");

    RuleFor(x => x.RoomId)
      .Must(value => !value.HasValue || value.Value != Guid.Empty)
      .WithMessage("RoomId must not be empty.");

    RuleFor(x => x.EndTime)
      .Must((request, endTime) => !endTime.HasValue || !request.StartTime.HasValue || endTime.Value > request.StartTime.Value)
      .WithMessage(ShowtimeMessages.EndTimeMustBeGreaterThanStartTime);

    RuleFor(x => x.TimeSlot)
      .Must(value => ValidTimeSlots.Contains(value!))
      .When(x => !string.IsNullOrWhiteSpace(x.TimeSlot))
      .WithMessage("TimeSlot must be one of: MORNING, AFTERNOON, EVENING, MIDNIGHT, PEAK.");

    RuleFor(x => x.LanguageType)
      .Must(value => ValidLanguageTypes.Contains(value!))
      .When(x => !string.IsNullOrWhiteSpace(x.LanguageType))
      .WithMessage("LanguageType must be DUBBED or SUBTITLED.");

    RuleFor(x => x.Status)
      .Must(value => ValidStatuses.Contains(value!))
      .When(x => !string.IsNullOrWhiteSpace(x.Status))
      .WithMessage("Status is auto-managed by showtime time. Only CANCELLED can be requested manually.");
  }
}
