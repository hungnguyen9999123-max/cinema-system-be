using System.ComponentModel.DataAnnotations;
using CinemaSystem.Common.Constants;

namespace CinemaSystem.Common.DTOs.PricingRules;

public sealed record PricingRuleResponse(
    Guid Id,
    Guid CinemaId,
    string CinemaName,
    int RoomTypeId,
    int TimeSlotId,
    decimal BasePrice,
    decimal TimeMultiplier,
    DateOnly EffectiveFrom,
    DateOnly EffectiveTo,
    bool IsActive,
    DateTime CreatedAt);

public sealed class UpdatePricingRuleRequest
{
    [Range(0.01, double.MaxValue, ErrorMessage = PricingRuleMessages.InvalidBasePrice)]
    public decimal BasePrice { get; init; }

    [Range(0.01, double.MaxValue, ErrorMessage = PricingRuleMessages.InvalidTimeMultiplier)]
    public decimal TimeMultiplier { get; init; }

    public bool IsActive { get; init; } = true;
}

public sealed class CreatePricingRuleRequest
{
    [Range(1, int.MaxValue, ErrorMessage = PricingRuleMessages.InvalidRoomTypeId)]
    public int RoomTypeId { get; init; }

    [Range(1, int.MaxValue, ErrorMessage = PricingRuleMessages.InvalidTimeSlotId)]
    public int TimeSlotId { get; init; }

    [Range(0.01, double.MaxValue, ErrorMessage = PricingRuleMessages.InvalidBasePrice)]
    public decimal BasePrice { get; init; }

    [Range(0.01, double.MaxValue, ErrorMessage = PricingRuleMessages.InvalidTimeMultiplier)]
    public decimal TimeMultiplier { get; init; }

    public DateOnly EffectiveFrom { get; init; }

    public DateOnly EffectiveTo { get; init; } = PricingRuleDefaults.DefaultEffectiveTo;

    public bool IsActive { get; init; } = true;
}

public sealed class CalculateTicketPriceRequest
{
    [Required]
    public Guid ShowtimeId { get; init; }

    [Required]
    [MinLength(1, ErrorMessage = PricingRuleMessages.SeatIdsRequired)]
    public List<Guid> SeatIds { get; init; } = new();

    [Required]
    public Guid AudienceTypeId { get; init; }

    public Guid? CinemaId { get; init; }

    public int? RoomTypeId { get; init; }
}

public sealed record SeatPriceItem(
    Guid SeatId,
    decimal UnitPrice);

public sealed record TicketPriceResponse(
    Guid ShowtimeId,
    Guid AudienceTypeId,
    Guid PricingRuleId,
    decimal BasePrice,
    decimal TimeMultiplier,
    IReadOnlyList<SeatPriceItem> Seats,
    decimal TotalPrice);
