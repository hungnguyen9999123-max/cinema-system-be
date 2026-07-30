using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class PasswordResetToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? UsedAt { get; set; }

    public string? UsedByIp { get; set; }

    public bool IsUsed { get; set; }

    public virtual User User { get; set; } = null!;
}
