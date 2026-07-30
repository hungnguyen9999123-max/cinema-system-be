using CinemaSystem.Common.DTOs.Rooms;
using CinemaSystem.Common.Enums;

namespace CinemaSystem.Services.Services.Rooms;

public interface ISeatService
{
    Task<SeatLayoutResponse?> GetLayoutAsync(Guid roomId, CancellationToken cancellationToken = default);
    Task<SeatLayoutResponse> GenerateLayoutAsync(Guid roomId, GenerateSeatLayoutRequest request, CancellationToken cancellationToken = default);
    Task<SeatResponse> CreateSeatAsync(Guid roomId, CreateSeatRequest request, CancellationToken cancellationToken = default);
    Task<SeatResponse?> UpdateSeatAsync(Guid seatId, UpdateSeatRequest request, CancellationToken cancellationToken = default);
    Task<DeleteSeatResult> DeleteSeatAsync(Guid seatId, CancellationToken cancellationToken = default);
}
