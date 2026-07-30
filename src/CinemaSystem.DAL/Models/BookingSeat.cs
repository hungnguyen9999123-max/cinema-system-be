using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class BookingSeat
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid SeatId { get; set; }

    public Guid ShowtimeId { get; set; }

    public Guid PricingRuleId { get; set; }

    public Guid AudienceTypeId { get; set; }

    public decimal BasePriceSnap { get; set; }

    public decimal SeatMultSnap { get; set; }

    public decimal AudienceMultSnap { get; set; }

    public decimal TimeMultSnap { get; set; }

    public decimal UnitPrice { get; set; }

    public string SeatStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public virtual AudienceType AudienceType { get; set; } = null!;

    public virtual Booking Booking { get; set; } = null!;

    public virtual Booking BookingNavigation { get; set; } = null!;

    public virtual PricingRule PricingRule { get; set; } = null!;

    public virtual Seat Seat { get; set; } = null!;

    public virtual Showtime Showtime { get; set; } = null!;

    public virtual Ticket? TicketBookingSeat { get; set; }

    public virtual ICollection<Ticket> TicketBookingSeatNavigations { get; set; } = new List<Ticket>();
}
