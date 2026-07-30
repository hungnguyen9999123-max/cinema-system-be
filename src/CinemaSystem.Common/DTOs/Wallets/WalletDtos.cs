namespace CinemaSystem.Common.DTOs.Wallets;

public sealed class WalletSummaryDto
{
    public Guid WalletId { get; set; }
    public decimal AvailableBalance { get; set; }
    public IReadOnlyList<WalletTransactionDto> Transactions { get; set; } = Array.Empty<WalletTransactionDto>();
}

public sealed class WalletTransactionDto
{
    public Guid TransactionId { get; set; }
    public string Type { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public sealed class CreateWalletTopUpRequestDto
{
    public decimal Amount { get; set; }
}

public sealed class WalletTopUpResponseDto
{
    public Guid TopUpId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? GatewayTxnId { get; set; }
    public string? PaymentUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public sealed class WalletTopUpPagedResultDto
{
    public IReadOnlyList<WalletTopUpResponseDto> Items { get; set; } = Array.Empty<WalletTopUpResponseDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}

public sealed class CreateWithdrawalRequestDto
{
    public decimal Amount { get; set; }
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string? Note { get; set; }
}

public sealed class WithdrawalDecisionDto
{
    public string? TransferReference { get; set; }
    public string? InternalNote { get; set; }
}

public sealed class WithdrawalResponseDto
{
    public Guid WithdrawalId { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;
    public string AccountHolder { get; set; } = string.Empty;
    public string? Note { get; set; }
    public string? TransferReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
}

public sealed class WithdrawalPagedResultDto
{
    public IReadOnlyList<WithdrawalResponseDto> Items { get; set; } = Array.Empty<WithdrawalResponseDto>();
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
}
