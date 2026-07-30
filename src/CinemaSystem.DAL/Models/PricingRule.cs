using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class PricingRule
{
    public Guid Id { get; set; }

    public Guid CinemaId { get; set; }

    public int RoomTypeId { get; set; }

    public int TimeSlotId { get; set; }

    public decimal BasePrice { get; set; }

    public decimal TimeMultiplier { get; set; }

    public DateOnly EffectiveFrom { get; set; }

    public DateOnly EffectiveTo { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<BookingSeat> BookingSeats { get; set; } = new List<BookingSeat>();

    public virtual Cinema Cinema { get; set; } = null!;
}
