using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Booking
{
    public Guid Id { get; set; }

    public Guid CustomerId { get; set; }

    public Guid ShowtimeId { get; set; }

    public Guid? PromotionId { get; set; }

    public string BookingRef { get; set; } = null!;

    public decimal TotalAmount { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal FinalAmount { get; set; }

    public string Status { get; set; } = null!;

    public DateTime BookedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public virtual ICollection<BookingSeat> BookingSeatBookingNavigations { get; set; } = new List<BookingSeat>();

    public virtual ICollection<BookingSeat> BookingSeatBookings { get; set; } = new List<BookingSeat>();

    public virtual User Customer { get; set; } = null!;

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<FnbOrder> FnbOrders { get; set; } = new List<FnbOrder>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual Promotion? Promotion { get; set; }

    public virtual ICollection<PromotionUsage> PromotionUsages { get; set; } = new List<PromotionUsage>();

    public virtual Showtime Showtime { get; set; } = null!;

    public virtual ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
}
