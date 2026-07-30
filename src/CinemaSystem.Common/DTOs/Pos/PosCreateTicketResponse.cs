using System.Text.Json.Serialization;

namespace CinemaSystem.Common.DTOs.Pos;

public sealed record PosCreateTicketResponse
{
    [JsonPropertyName("isVnpay")]
    public bool IsVnpay { get; init; }

    [JsonPropertyName("cash")]
    public PosBookingResponse? Cash { get; init; }

    [JsonPropertyName("vnpay")]
    public PosVnpayResponse? Vnpay { get; init; }
}