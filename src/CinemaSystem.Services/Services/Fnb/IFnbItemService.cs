using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.Fnb;
using CinemaSystem.Common.Enums;

namespace CinemaSystem.Services.Services.Fnb;

public interface IFnbItemService
{
    Task<PagedResult<FnbItemResponse>> SearchAsync(
        FnbItemSearchRequest request,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<FnbItemResponse?> GetByIdAsync(
        Guid id,
        bool activeOnly,
        CancellationToken cancellationToken = default);

    Task<FnbItemResponse> CreateAsync(
        CreateFnbItemRequest request,
        Guid createdBy,
        CancellationToken cancellationToken = default);

    Task<FnbItemResponse?> UpdateAsync(
        Guid id,
        UpdateFnbItemRequest request,
        CancellationToken cancellationToken = default);

    Task<DeleteFnbItemResult> DeleteAsync(
        Guid id,
        CancellationToken cancellationToken = default);
}
