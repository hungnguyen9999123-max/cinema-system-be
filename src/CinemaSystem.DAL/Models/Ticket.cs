using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class Ticket
{
    public Guid Id { get; set; }

    public Guid BookingId { get; set; }

    public Guid BookingSeatId { get; set; }

    public string QrCode { get; set; } = null!;

    public string QrPayload { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime GeneratedAt { get; set; }

    public DateTime ExpiredAt { get; set; }

    public DateTime? ScannedAt { get; set; }

    public Guid? ScannedBy { get; set; }

    [System.ComponentModel.DataAnnotations.Timestamp]
    public byte[] RowVersion { get; set; } = null!;

    public virtual Booking Booking { get; set; } = null!;

    public virtual BookingSeat BookingSeat { get; set; } = null!;

    public virtual BookingSeat BookingSeatNavigation { get; set; } = null!;

    public virtual User? ScannedByNavigation { get; set; }
}
