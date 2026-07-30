namespace CinemaSystem.DAL.Models;

public partial class Wallet
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = null!;

    public virtual User User { get; set; } = null!;
    public virtual ICollection<WalletTransaction> Transactions { get; set; } = new List<WalletTransaction>();
    public virtual ICollection<WalletTopUp> TopUps { get; set; } = new List<WalletTopUp>();
    public virtual ICollection<WithdrawalRequest> WithdrawalRequests { get; set; } = new List<WithdrawalRequest>();
}
