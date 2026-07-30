using System;
using System.Collections.Generic;

namespace CinemaSystem.DAL.Models;

public partial class EmailVerificationToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string TokenHash { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? VerifiedAt { get; set; }

    public string? VerifiedByIp { get; set; }

    public bool IsVerified { get; set; }

    public virtual User User { get; set; } = null!;
}
