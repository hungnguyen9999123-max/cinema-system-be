using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class PromotionUsage
{
    public Guid Id { get; set; }

    public Guid PromotionId { get; set; }

    public Guid? CustomerId { get; set; }

    public Guid BookingId { get; set; }

    public DateTime UsedAt { get; set; }

    public virtual Booking Booking { get; set; } = null!;

    public virtual User? Customer { get; set; }

    public virtual Promotion Promotion { get; set; } = null!;
}
