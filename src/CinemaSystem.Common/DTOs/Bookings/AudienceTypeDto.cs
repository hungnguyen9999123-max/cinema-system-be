namespace CinemaSystem.Common.DTOs.Bookings;

public class AudienceTypeDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public decimal AudienceMultiplier { get; set; }
    public string? Description { get; set; }
}
