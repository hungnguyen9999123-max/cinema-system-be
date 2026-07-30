using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace CinemaSystem.DAL.Repository.Wallets;

public sealed class WalletRepository(CinemaDbContext dbContext) : IWalletRepository
{
    public Task<Wallet?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.Wallets.FirstOrDefaultAsync(wallet => wallet.UserId == userId, cancellationToken);

    public async Task<Wallet> GetOrCreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var wallet = await GetByUserIdAsync(userId, cancellationToken);
        if (wallet is not null) return wallet;

        var now = DateTime.UtcNow;
        wallet = new Wallet
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Balance = 0m,
            CreatedAt = now,
            UpdatedAt = now
        };
        await dbContext.Wallets.AddAsync(wallet, cancellationToken);
        return wallet;
    }

    public async Task<(Guid WalletId, decimal BalanceAfter)?> TryDebitAsync(
        Guid userId,
        decimal amount,
        DateTime updatedAt,
        CancellationToken cancellationToken = default)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));

        var connection = (SqlConnection)dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose) await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction() as SqlTransaction;
            command.CommandText = """
                UPDATE dbo.WALLETS WITH (UPDLOCK, ROWLOCK)
                SET balance = balance - @amount,
                    updated_at = @updatedAt
                OUTPUT INSERTED.id, INSERTED.balance
                WHERE user_id = @userId
                  AND balance >= @amount;
                """;
            command.Parameters.Add(new SqlParameter("@userId", System.Data.SqlDbType.UniqueIdentifier) { Value = userId });
            command.Parameters.Add(new SqlParameter("@amount", System.Data.SqlDbType.Decimal)
            {
                Precision = 18,
                Scale = 2,
                Value = amount
            });
            command.Parameters.Add(new SqlParameter("@updatedAt", System.Data.SqlDbType.DateTime2) { Value = updatedAt });

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) return null;
            return (reader.GetGuid(0), reader.GetDecimal(1));
        }
        finally
        {
            if (shouldClose) await connection.CloseAsync();
        }
    }

    public Task<bool> HasRefundCreditAsync(Guid refundId, CancellationToken cancellationToken = default) =>
        dbContext.WalletTransactions.AnyAsync(transaction => transaction.RefundId == refundId, cancellationToken);

    public Task<bool> HasTopUpCreditAsync(Guid topUpId, CancellationToken cancellationToken = default) =>
        dbContext.WalletTransactions.AnyAsync(transaction => transaction.WalletTopUpId == topUpId, cancellationToken);

    public Task<bool> HasBookingPaymentDebitAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
        dbContext.WalletTransactions.AnyAsync(transaction => transaction.PaymentId == paymentId, cancellationToken);

    public Task AddTransactionAsync(WalletTransaction transaction, CancellationToken cancellationToken = default) =>
        dbContext.WalletTransactions.AddAsync(transaction, cancellationToken).AsTask();

    public async Task<(IReadOnlyList<WalletTransaction> Items, int TotalCount)> GetTransactionsAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = dbContext.WalletTransactions.AsNoTracking().Where(transaction => transaction.Wallet.UserId == userId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(transaction => transaction.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<WithdrawalRequest?> GetWithdrawalByIdAsync(Guid withdrawalId, CancellationToken cancellationToken = default) =>
        WithdrawalDetailsQuery().FirstOrDefaultAsync(withdrawal => withdrawal.Id == withdrawalId, cancellationToken);

    public Task<WithdrawalRequest?> GetWithdrawalByIdempotencyKeyAsync(Guid userId, string keyHash, CancellationToken cancellationToken = default) =>
        WithdrawalDetailsQuery().FirstOrDefaultAsync(withdrawal => withdrawal.RequestedBy == userId && withdrawal.IdempotencyKeyHash == keyHash, cancellationToken);

    public Task AddWithdrawalAsync(WithdrawalRequest withdrawal, CancellationToken cancellationToken = default) =>
        dbContext.WithdrawalRequests.AddAsync(withdrawal, cancellationToken).AsTask();

    public Task<WalletTopUp?> GetTopUpByIdAsync(Guid topUpId, CancellationToken cancellationToken = default) =>
        TopUpDetailsQuery().FirstOrDefaultAsync(topUp => topUp.Id == topUpId, cancellationToken);

    public Task<WalletTopUp?> GetTopUpByIdempotencyKeyAsync(Guid userId, string keyHash, CancellationToken cancellationToken = default) =>
        TopUpDetailsQuery().FirstOrDefaultAsync(topUp => topUp.RequestedBy == userId && topUp.IdempotencyKeyHash == keyHash, cancellationToken);

    public Task<WalletTopUp?> GetTopUpByGatewayTxnIdAsync(string gatewayTxnId, CancellationToken cancellationToken = default) =>
        TopUpDetailsQuery().FirstOrDefaultAsync(topUp => topUp.GatewayTxnId == gatewayTxnId, cancellationToken);

    public Task AddTopUpAsync(WalletTopUp topUp, CancellationToken cancellationToken = default) =>
        dbContext.WalletTopUps.AddAsync(topUp, cancellationToken).AsTask();

    public Task<int> ExpirePendingTopUpsAsync(Guid userId, DateTime now, CancellationToken cancellationToken = default) =>
        dbContext.WalletTopUps
            .Where(topUp => topUp.RequestedBy == userId && topUp.Status == "PENDING" && topUp.ExpiresAt <= now)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(topUp => topUp.Status, "EXPIRED")
                .SetProperty(topUp => topUp.CompletedAt, topUp => topUp.ExpiresAt), cancellationToken);

    public async Task<(IReadOnlyList<WalletTopUp> Items, int TotalCount)> GetTopUpsForCustomerAsync(Guid userId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = TopUpDetailsQuery().AsNoTracking().Where(topUp => topUp.RequestedBy == userId);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(topUp => topUp.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<(IReadOnlyList<WithdrawalRequest> Items, int TotalCount)> GetWithdrawalsForCustomerAsync(Guid userId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = WithdrawalDetailsQuery().AsNoTracking().Where(withdrawal => withdrawal.RequestedBy == userId);
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(withdrawal => withdrawal.Status == status.Trim().ToUpperInvariant());
        return await PageWithdrawalsAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<(IReadOnlyList<WithdrawalRequest> Items, int TotalCount)> GetWithdrawalsForOperationsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var query = WithdrawalDetailsQuery().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status)) query = query.Where(withdrawal => withdrawal.Status == status.Trim().ToUpperInvariant());
        return await PageWithdrawalsAsync(query, page, pageSize, cancellationToken);
    }

    public void Update(Wallet wallet)
    {
        // New wallets are already tracked as Added. Marking them Modified would
        // turn the insert into an update with a null row-version predicate.
        if (dbContext.Entry(wallet).State == EntityState.Detached)
            dbContext.Wallets.Update(wallet);
    }

    public void Update(WithdrawalRequest withdrawal)
    {
        if (dbContext.Entry(withdrawal).State == EntityState.Detached)
            dbContext.WithdrawalRequests.Update(withdrawal);
    }

    public void Update(WalletTopUp topUp)
    {
        if (dbContext.Entry(topUp).State == EntityState.Detached)
            dbContext.WalletTopUps.Update(topUp);
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<WithdrawalRequest> WithdrawalDetailsQuery() =>
        dbContext.WithdrawalRequests
            .Include(withdrawal => withdrawal.Wallet)
            .Include(withdrawal => withdrawal.RequestedByNavigation);

    private IQueryable<WalletTopUp> TopUpDetailsQuery() =>
        dbContext.WalletTopUps
            .Include(topUp => topUp.Wallet)
            .Include(topUp => topUp.RequestedByNavigation);

    private static async Task<(IReadOnlyList<WithdrawalRequest> Items, int TotalCount)> PageWithdrawalsAsync(
        IQueryable<WithdrawalRequest> query,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.OrderByDescending(withdrawal => withdrawal.RequestedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        return (items, total);
    }
}
