namespace CinemaSystem.DAL.Models;

public partial class WalletTransaction
{
    public Guid Id { get; set; }
    public Guid WalletId { get; set; }
    public Guid? RefundId { get; set; }
    public Guid? PaymentId { get; set; }
    public Guid? WalletTopUpId { get; set; }
    public Guid? WithdrawalRequestId { get; set; }
    public string Type { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal BalanceAfter { get; set; }
    public string Description { get; set; } = null!;
    public DateTime CreatedAt { get; set; }

    public virtual Wallet Wallet { get; set; } = null!;
    public virtual Refund? Refund { get; set; }
    public virtual Payment? Payment { get; set; }
    public virtual WalletTopUp? WalletTopUp { get; set; }
    public virtual WithdrawalRequest? WithdrawalRequest { get; set; }
}
