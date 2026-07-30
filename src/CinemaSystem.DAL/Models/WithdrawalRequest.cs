namespace CinemaSystem.DAL.Models;

public partial class WithdrawalRequest
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid RequestedBy { get; set; }
    public Guid? ProcessedBy { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public string BankName { get; set; } = null!;
    public string BankAccountNumber { get; set; } = null!;
    public string AccountHolder { get; set; } = null!;
    public string? Note { get; set; }
    public string? TransferReference { get; set; }
    public string? FailureReason { get; set; }
    public string? IdempotencyKeyHash { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public virtual Wallet Wallet { get; set; } = null!;
    public virtual User RequestedByNavigation { get; set; } = null!;
    public virtual User? ProcessedByNavigation { get; set; }
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
