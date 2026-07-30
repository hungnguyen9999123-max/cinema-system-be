using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class FnbItem
{
    public Guid Id { get; set; }

    public Guid CreatedBy { get; set; }

    public string Name { get; set; } = null!;

    public string Category { get; set; } = null!;

    public string? Description { get; set; }

    public decimal Price { get; set; }

    public string? ImageUrl { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    public string? ImagePublicId { get; set; }

    public virtual User CreatedByNavigation { get; set; } = null!;

    public virtual ICollection<FnbOrderDetail> FnbOrderDetails { get; set; } = new List<FnbOrderDetail>();
}
