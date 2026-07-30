using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class FnbOrderDetail
{
    public Guid Id { get; set; }

    public Guid FnbOrderId { get; set; }

    public Guid ItemId { get; set; }

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal Subtotal { get; set; }

    public virtual FnbOrder FnbOrder { get; set; } = null!;

    public virtual FnbItem Item { get; set; } = null!;
}
