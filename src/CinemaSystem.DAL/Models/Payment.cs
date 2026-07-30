using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Payment
{
    public Guid Id { get; set; }

    public Guid? BookingId { get; set; }

    public string? GatewayTxnId { get; set; }

    public string Gateway { get; set; } = null!;

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public string? IpnSignature { get; set; }

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? GatewayRequestAt { get; set; }

    public string? IdempotencyKeyHash { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public Guid? FnbOrderId { get; set; }

    public virtual FnbOrder? FnbOrder { get; set; }

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual ICollection<WalletTransaction> WalletTransactions { get; set; } = new List<WalletTransaction>();
}
