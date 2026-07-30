using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? AvatarUrl { get; set; }

    public string Role { get; set; } = null!;

    public string Status { get; set; } = null!;

    public bool IsEmailVerified { get; set; }

    public DateTime? LastLogin { get; set; }

    public byte FailedLoginCount { get; set; }

    public DateTime? LockedUntil { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? Provider { get; set; }

    public string? GoogleId { get; set; }

    public virtual ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual EmailVerificationToken? EmailVerificationToken { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<FnbItem> FnbItems { get; set; } = new List<FnbItem>();

    public virtual ICollection<FnbOrder> FnbOrders { get; set; } = new List<FnbOrder>();

    public virtual ICollection<Movie> Movies { get; set; } = new List<Movie>();

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual PasswordResetToken? PasswordResetToken { get; set; }

    public virtual ICollection<PromotionUsage> PromotionUsages { get; set; } = new List<PromotionUsage>();

    public virtual ICollection<Promotion> Promotions { get; set; } = new List<Promotion>();

    public virtual RefreshToken? RefreshToken { get; set; }

    public virtual ICollection<Refund> Refunds { get; set; } = new List<Refund>();

    public virtual ICollection<Refund> RequestedRefunds { get; set; } = new List<Refund>();

    public virtual ICollection<Showtime> Showtimes { get; set; } = new List<Showtime>();

    public virtual ICollection<StaffAssignment> StaffAssignments { get; set; } = new List<StaffAssignment>();

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();

    public virtual Wallet? Wallet { get; set; }

    public virtual ICollection<WalletTopUp> WalletTopUps { get; set; } = new List<WalletTopUp>();

    public virtual ICollection<WithdrawalRequest> WithdrawalRequests { get; set; } = new List<WithdrawalRequest>();
}
