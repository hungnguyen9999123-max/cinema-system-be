using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class FnbOrder
{
    public Guid Id { get; set; }

    public Guid? BookingId { get; set; }

    public Guid CustomerId { get; set; }

    public Guid? StaffId { get; set; }

    public decimal TotalAmount { get; set; }

    public string OrderStatus { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public string? PaymentMethod { get; set; }

    public virtual Booking? Booking { get; set; }

    public virtual User Customer { get; set; } = null!;

    public virtual User? Staff { get; set; }

    public virtual ICollection<FnbOrderDetail> FnbOrderDetails { get; set; } = new List<FnbOrderDetail>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
