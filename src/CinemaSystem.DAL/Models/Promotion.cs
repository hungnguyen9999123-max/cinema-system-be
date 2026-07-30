using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Promotion
{
    public Guid Id { get; set; }

    public Guid CreatedBy { get; set; }

    public string PromoCode { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string DiscountType { get; set; } = null!;

    public decimal DiscountValue { get; set; }

    public decimal? MinOrderAmt { get; set; }

    public DateOnly ValidFrom { get; set; }

    public DateOnly ValidTo { get; set; }

    public int? UsageLimit { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<PromotionUsage> PromotionUsages { get; set; } = new List<PromotionUsage>();
}
