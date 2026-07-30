using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.Rooms;
using CinemaSystem.Common.Enums;

namespace CinemaSystem.Services.Services.Rooms;

public interface IRoomService
{
    Task<PagedResult<RoomResponse>> SearchAsync(RoomSearchRequest request, CancellationToken cancellationToken = default);
    Task<RoomResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoomResponse> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken = default);
    Task<RoomResponse?> UpdateAsync(Guid id, UpdateRoomRequest request, CancellationToken cancellationToken = default);
    Task<DeleteRoomResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
