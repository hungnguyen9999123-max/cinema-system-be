using System.ComponentModel.DataAnnotations;

namespace CinemaSystem.Common.DTOs.QrTickets;

public sealed record VerifyQrRequestDto
{
    [Required]
    public string Token { get; init; } = null!;
}
