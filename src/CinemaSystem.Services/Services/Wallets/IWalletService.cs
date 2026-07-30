using CinemaSystem.Common.DTOs.Wallets;

namespace CinemaSystem.Services.Services.Wallets;

public interface IWalletService
{
    Task<WalletSummaryDto> GetMineAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<WithdrawalResponseDto> CreateWithdrawalAsync(Guid customerId, string idempotencyKey, CreateWithdrawalRequestDto request, CancellationToken cancellationToken = default);
    Task<WithdrawalPagedResultDto> GetMineWithdrawalsAsync(Guid customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<WithdrawalPagedResultDto> GetOperationsWithdrawalsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<WithdrawalResponseDto> CompleteWithdrawalAsync(Guid withdrawalId, Guid managerId, WithdrawalDecisionDto request, CancellationToken cancellationToken = default);
    Task<WithdrawalResponseDto> RejectWithdrawalAsync(Guid withdrawalId, Guid managerId, WithdrawalDecisionDto request, CancellationToken cancellationToken = default);
}
