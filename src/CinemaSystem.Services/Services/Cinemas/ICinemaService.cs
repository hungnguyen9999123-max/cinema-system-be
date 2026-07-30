using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.Cinemas;
using CinemaSystem.Common.Enums;

namespace CinemaSystem.Services.Services.Cinemas;

public interface ICinemaService
{
    Task<PagedResult<CinemaResponse>> SearchAsync(CinemaSearchRequest request, CancellationToken cancellationToken = default);
    Task<CinemaResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CinemaResponse> CreateAsync(CreateCinemaRequest request, CancellationToken cancellationToken = default);
    Task<CinemaResponse?> UpdateAsync(Guid id, UpdateCinemaRequest request, CancellationToken cancellationToken = default);
    Task<DeleteCinemaResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
