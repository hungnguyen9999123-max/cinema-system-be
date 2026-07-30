namespace CinemaSystem.DAL.Models;

public partial class WalletTopUp
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid RequestedBy { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = null!;
    public string? GatewayTxnId { get; set; }
    public string? ResponseCode { get; set; }
    public string? TransactionStatus { get; set; }
    public string IdempotencyKeyHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public virtual Wallet Wallet { get; set; } = null!;
    public virtual User RequestedByNavigation { get; set; } = null!;
    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
