using System.Security.Cryptography;
using System.Text;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Wallets;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;

namespace CinemaSystem.Services.Services.Wallets;

public sealed class WalletService(IWalletRepository walletRepository, IUnitOfWork unitOfWork) : IWalletService
{
    public async Task<WalletSummaryDto> GetMineAsync(Guid customerId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var wallet = await walletRepository.GetByUserIdAsync(customerId, cancellationToken);
        var (transactions, _) = await walletRepository.GetTransactionsAsync(customerId, page, pageSize, cancellationToken);
        return new WalletSummaryDto
        {
            WalletId = wallet?.Id ?? Guid.Empty,
            AvailableBalance = wallet?.Balance ?? 0m,
            Transactions = transactions.Select(ToTransactionResponse).ToArray()
        };
    }

    public async Task<WithdrawalResponseDto> CreateWithdrawalAsync(Guid customerId, string idempotencyKey, CreateWithdrawalRequestDto request, CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty) throw new UnauthorizedAccessException("Bạn cần đăng nhập để rút tiền từ ví.");
        if (request.Amount <= 0m) throw new InvalidOperationException("Số tiền rút phải lớn hơn 0.");

        var bankName = Required(request.BankName, "Ngân hàng");
        var accountNumber = Required(request.BankAccountNumber, "Số tài khoản").Replace(" ", string.Empty, StringComparison.Ordinal);
        var accountHolder = Required(request.AccountHolder, "Tên chủ tài khoản");
        if (accountNumber.Length < 6 || accountNumber.Length > 64 || !accountNumber.All(char.IsLetterOrDigit))
            throw new InvalidOperationException("Số tài khoản không hợp lệ.");
        if (!Guid.TryParse(idempotencyKey, out _)) throw new InvalidOperationException("Idempotency-Key phải là UUID.");

        var keyHash = Hash(idempotencyKey);
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var existing = await walletRepository.GetWithdrawalByIdempotencyKeyAsync(customerId, keyHash, cancellationToken);
            if (existing is not null)
            {
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                return ToWithdrawalResponse(existing);
            }

            var wallet = await walletRepository.GetOrCreateAsync(customerId, cancellationToken);
            if (wallet.Balance < request.Amount)
                throw new BusinessConflictException("Số dư ví không đủ để tạo yêu cầu rút tiền.");

            var now = DateTime.UtcNow;
            wallet.Balance -= request.Amount;
            wallet.UpdatedAt = now;
            var withdrawal = new WithdrawalRequest
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Wallet = wallet,
                RequestedBy = customerId,
                Amount = decimal.Round(request.Amount, 2),
                Status = WithdrawalStatus.Pending,
                BankName = Trim(bankName, 100),
                BankAccountNumber = accountNumber,
                AccountHolder = Trim(accountHolder, 120),
                Note = Optional(request.Note, 500),
                IdempotencyKeyHash = keyHash,
                RequestedAt = now,
                UpdatedAt = now
            };
            await walletRepository.AddWithdrawalAsync(withdrawal, cancellationToken);
            await walletRepository.AddTransactionAsync(new WalletTransaction
            {
                Id = Guid.NewGuid(),
                WalletId = wallet.Id,
                Wallet = wallet,
                WithdrawalRequestId = withdrawal.Id,
                WithdrawalRequest = withdrawal,
                Type = WalletTransactionType.WithdrawalHold,
                Amount = -withdrawal.Amount,
                BalanceAfter = wallet.Balance,
                Description = "Giữ số dư cho yêu cầu rút tiền",
                CreatedAt = now
            }, cancellationToken);
            walletRepository.Update(wallet);
            await walletRepository.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return ToWithdrawalResponse(withdrawal);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<WithdrawalPagedResultDto> GetMineWithdrawalsAsync(Guid customerId, string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, total) = await walletRepository.GetWithdrawalsForCustomerAsync(customerId, status, page, pageSize, cancellationToken);
        return ToPage(items, total, page, pageSize);
    }

    public async Task<WithdrawalPagedResultDto> GetOperationsWithdrawalsAsync(string? status, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (items, total) = await walletRepository.GetWithdrawalsForOperationsAsync(status, page, pageSize, cancellationToken);
        return ToPage(items, total, page, pageSize);
    }

    public Task<WithdrawalResponseDto> CompleteWithdrawalAsync(Guid withdrawalId, Guid managerId, WithdrawalDecisionDto request, CancellationToken cancellationToken = default) =>
        DecideAsync(withdrawalId, managerId, request, complete: true, cancellationToken);

    public Task<WithdrawalResponseDto> RejectWithdrawalAsync(Guid withdrawalId, Guid managerId, WithdrawalDecisionDto request, CancellationToken cancellationToken = default) =>
        DecideAsync(withdrawalId, managerId, request, complete: false, cancellationToken);

    private async Task<WithdrawalResponseDto> DecideAsync(Guid withdrawalId, Guid managerId, WithdrawalDecisionDto request, bool complete, CancellationToken cancellationToken)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var withdrawal = await walletRepository.GetWithdrawalByIdAsync(withdrawalId, cancellationToken)
                ?? throw new KeyNotFoundException("Không tìm thấy yêu cầu rút tiền.");
            if (withdrawal.Status != WithdrawalStatus.Pending)
                throw new BusinessConflictException("Yêu cầu rút tiền này đã được xử lý.");

            var now = DateTime.UtcNow;
            withdrawal.ProcessedBy = managerId;
            withdrawal.ProcessedAt = now;
            withdrawal.UpdatedAt = now;
            if (complete)
            {
                withdrawal.TransferReference = Required(request.TransferReference, "Mã giao dịch chuyển khoản");
                withdrawal.Status = WithdrawalStatus.Completed;
            }
            else
            {
                var wallet = withdrawal.Wallet;
                wallet.Balance += withdrawal.Amount;
                wallet.UpdatedAt = now;
                withdrawal.Status = WithdrawalStatus.Rejected;
                withdrawal.FailureReason = Optional(request.InternalNote, 500) ?? "Manager từ chối yêu cầu rút tiền.";
                await walletRepository.AddTransactionAsync(new WalletTransaction
                {
                    Id = Guid.NewGuid(),
                    WalletId = wallet.Id,
                    Wallet = wallet,
                    WithdrawalRequestId = withdrawal.Id,
                    WithdrawalRequest = withdrawal,
                    Type = WalletTransactionType.WithdrawalReversal,
                    Amount = withdrawal.Amount,
                    BalanceAfter = wallet.Balance,
                    Description = "Hoàn số dư do yêu cầu rút tiền bị từ chối",
                    CreatedAt = now
                }, cancellationToken);
                walletRepository.Update(wallet);
            }

            walletRepository.Update(withdrawal);
            await walletRepository.SaveChangesAsync(cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);
            return ToWithdrawalResponse(withdrawal);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static WalletTransactionDto ToTransactionResponse(WalletTransaction transaction) => new()
    {
        TransactionId = transaction.Id,
        Type = transaction.Type,
        Amount = transaction.Amount,
        BalanceAfter = transaction.BalanceAfter,
        Description = transaction.Description,
        CreatedAt = transaction.CreatedAt
    };

    private static WithdrawalResponseDto ToWithdrawalResponse(WithdrawalRequest withdrawal) => new()
    {
        WithdrawalId = withdrawal.Id,
        Amount = withdrawal.Amount,
        Status = withdrawal.Status,
        BankName = withdrawal.BankName,
        BankAccountNumber = withdrawal.BankAccountNumber,
        AccountHolder = withdrawal.AccountHolder,
        Note = withdrawal.Note,
        TransferReference = withdrawal.TransferReference,
        FailureReason = withdrawal.FailureReason,
        RequestedAt = withdrawal.RequestedAt,
        ProcessedAt = withdrawal.ProcessedAt,
        CustomerName = withdrawal.RequestedByNavigation?.FullName,
        CustomerEmail = withdrawal.RequestedByNavigation?.Email
    };

    private static WithdrawalPagedResultDto ToPage(IReadOnlyList<WithdrawalRequest> items, int total, int page, int pageSize) => new()
    {
        Items = items.Select(ToWithdrawalResponse).ToArray(),
        Page = Math.Max(1, page),
        PageSize = Math.Clamp(pageSize, 1, 100),
        TotalCount = total
    };

    private static string Required(string? value, string label) =>
        string.IsNullOrWhiteSpace(value) ? throw new InvalidOperationException($"{label} là bắt buộc.") : value.Trim();
    private static string? Optional(string? value, int maxLength) => string.IsNullOrWhiteSpace(value) ? null : Trim(value, maxLength);
    private static string Trim(string value, int maxLength) => value.Trim()[..Math.Min(value.Trim().Length, maxLength)];
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value.Trim()))).ToLowerInvariant();
}
