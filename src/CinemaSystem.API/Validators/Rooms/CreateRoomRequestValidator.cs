using CinemaSystem.Common.DTOs.Rooms;
using FluentValidation;

namespace CinemaSystem.API.Validators.Rooms;

public sealed class CreateRoomRequestValidator : AbstractValidator<CreateRoomRequest>
{
    public CreateRoomRequestValidator()
    {
        RuleFor(x => x.CinemaId).NotEmpty();
        RuleFor(x => x.Name).NotEmpty();
        RuleFor(x => x.RoomType).NotEmpty();
    }
}
