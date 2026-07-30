using CinemaSystem.Common;
using CinemaSystem.Common.DTOs.Fnb;

namespace CinemaSystem.Services.Services.Fnb;

public interface IFnbOrderService
{
    Task<PagedResult<FnbOrderResponse>> SearchAsync(
        FnbOrderSearchRequest request,
        CancellationToken cancellationToken = default);

    Task<FnbOrderResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default);

    Task<FnbOrderResponse> CreateAsync(
        CreateFnbOrderRequest request,
        Guid customerId,
        CancellationToken cancellationToken = default);

    Task<FnbOrderResponse> CreateCounterOrderAsync(
        CreateFnbCounterOrderRequest request,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<FnbOrderResponse> CreateForCounterAsync(
        CreateFnbOrderForCounterRequest request,
        Guid staffId,
        CancellationToken cancellationToken = default);

    Task<FnbOrderResponse?> UpdateStatusAsync(
        Guid id,
        UpdateFnbOrderStatusRequest request,
        CancellationToken cancellationToken = default);
}
