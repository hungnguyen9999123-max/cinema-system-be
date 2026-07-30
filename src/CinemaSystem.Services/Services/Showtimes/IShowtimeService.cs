using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.Showtimes;
using CinemaSystem.Common.Enums;

namespace CinemaSystem.Services.Services.Showtimes;

public interface IShowtimeService
{
    Task<PagedResult<ShowtimeResponse>> SearchAsync(ShowtimeSearchRequest request, CancellationToken cancellationToken = default);
    Task<ShowtimeResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ShowtimeResponse> CreateAsync(CreateShowtimeRequest request, Guid createdByUserId, CancellationToken cancellationToken = default);
    Task<ShowtimeResponse?> UpdateAsync(Guid id, UpdateShowtimeRequest request, CancellationToken cancellationToken = default);
    Task<DeleteShowtimeResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<int> SyncShowtimeStatusesAsync(DateTime currentTime, CancellationToken cancellationToken = default);
}
