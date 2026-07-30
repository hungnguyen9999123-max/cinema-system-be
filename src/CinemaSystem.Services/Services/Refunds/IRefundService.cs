using CinemaSystem.Common.DTOs.Refunds;

namespace CinemaSystem.Services.Services.Refunds;

public interface IRefundService
{
    RefundPolicyDto GetPolicy();
    Task<RefundResponseDto> CreateAsync(Guid customerId, string idempotencyKey, CreateRefundRequestDto request, CancellationToken cancellationToken = default);
    Task<RefundPagedResultDto> GetMineAsync(Guid customerId, RefundListQueryRequest request, CancellationToken cancellationToken = default);
    Task<RefundPagedResultDto> GetForOperationsAsync(RefundListQueryRequest request, CancellationToken cancellationToken = default);
    Task<RefundResponseDto> ApproveAsync(Guid refundId, Guid managerId, string? note, CancellationToken cancellationToken = default);
    Task<RefundResponseDto> RejectAsync(Guid refundId, Guid managerId, RefundDecisionRequestDto request, CancellationToken cancellationToken = default);
}
