using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Showtimes;
using FluentValidation;

namespace CinemaSystem.API.Validators.Showtimes;

public sealed class CreateShowtimeRequestValidator : AbstractValidator<CreateShowtimeRequest>
{
    public CreateShowtimeRequestValidator()
    {
        RuleFor(x => x.MovieId).NotEmpty();
        RuleFor(x => x.RoomId).NotEmpty();

        RuleFor(x => x.StartTime)
            .NotEmpty()
            .Must(startTime => startTime >= DateTime.Now)
            .WithMessage(ShowtimeMessages.ShowtimeStartTimeCannotBeInPast);
    }
}
