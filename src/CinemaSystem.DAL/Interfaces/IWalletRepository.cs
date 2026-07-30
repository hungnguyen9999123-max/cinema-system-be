using CinemaSystem.DAL.Models;

namespace CinemaSystem.DAL.Interfaces;

public interface IWalletRepository
{
    Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<Wallet> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default);
    /// <summary>
    /// Atomically reserves a wallet balance for checkout. A null result means
    /// that the wallet does not exist or has insufficient available balance.
    /// </summary>
    Task<(Guid WalletId, decimal BalanceAfter)?> TryDebitAsync(Guid userId, decimal amount, DateTime updatedAt, CancellationToken cancellationToken = default);
    Task<bool> HasRefundCreditAsync(Guid refundId, CancellationToken cancellationToken = default);
    Task<bool> HasTopUpCreditAsync(Guid topUpId, CancellationToken cancellationToken = default);
    Task<bool> HasBookingPaymentDebitAsync(Guid paymentId, CancellationToken cancellationToken = default);
    Task AddTransactionAsync(WalletTransaction transaction, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<WalletTransaction> Items, int TotalCount)> GetTransactionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<WithdrawalRequest?> GetWithdrawalByIdAsync(Guid withdrawalId, CancellationToken cancellationToken = default);
    Task<WithdrawalRequest?> GetWithdrawalByIdempotencyKeyAsync(Guid userId, string keyHash, CancellationToken cancellationToken = default);
    Task AddWithdrawalAsync(WithdrawalRequest withdrawal, CancellationToken cancellationToken = default);
    Task<WalletTopUp?> GetTopUpByIdAsync(Guid topUpId, CancellationToken cancellationToken = default);
    Task<WalletTopUp?> GetTopUpByIdempotencyKeyAsync(Guid userId, string keyHash, CancellationToken cancellationToken = default);
    Task<WalletTopUp?> GetTopUpByGatewayTxnIdAsync(string gatewayTxnId, CancellationToken cancellationToken = default);
    Task AddTopUpAsync(WalletTopUp topUp, CancellationToken cancellationToken = default);
    Task<int> ExpirePendingTopUpsAsync(Guid userId, DateTime now, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<WalletTopUp> Items, int TotalCount)> GetTopUpsForCustomerAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<WithdrawalRequest> Items, int TotalCount)> GetWithdrawalsForCustomerAsync(Guid userId, string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<(IReadOnlyList<WithdrawalRequest> Items, int TotalCount)> GetWithdrawalsForOperationsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default);
    void Update(Wallet wallet);
    void Update(WithdrawalRequest withdrawal);
    void Update(WalletTopUp topUp);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
