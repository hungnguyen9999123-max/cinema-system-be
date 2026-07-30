namespace CinemaSystem.Common.DTOs.QrTickets;

public sealed record GenerateQrRequestDto
{
    public string Format { get; init; } = "BASE64";
}
