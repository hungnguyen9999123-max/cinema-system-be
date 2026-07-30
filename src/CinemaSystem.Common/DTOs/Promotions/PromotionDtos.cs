using System.ComponentModel.DataAnnotations;

namespace CinemaSystem.Common.DTOs.Promotions;

/// <summary>
/// Represents a promotion returned to API consumers.
/// </summary>
public sealed record PromotionResponse(
    Guid Id,
    Guid CreatedBy,
    string PromoCode,
    string Name,
    string DiscountType,
    decimal DiscountValue,
    decimal? MinOrderAmt,
    DateOnly ValidFrom,
    DateOnly ValidTo,
    int? UsageLimit,
    bool IsActive,
    DateTime CreatedAt);

/// <summary>
/// Request payload for creating a promotion.
/// </summary>
public sealed class CreatePromotionRequest
{
    [Required]
    public string PromoCode { get; init; } = string.Empty;

    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string DiscountType { get; init; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal DiscountValue { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? MinOrderAmt { get; init; }

    [Required]
    public DateOnly ValidFrom { get; init; }

    [Required]
    public DateOnly ValidTo { get; init; }

    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; init; }

    public bool IsActive { get; init; } = true;
}

/// <summary>
/// Request payload for updating a promotion.
/// </summary>
public sealed class UpdatePromotionRequest
{
    [Required]
    public string PromoCode { get; init; } = string.Empty;

    [Required]
    public string Name { get; init; } = string.Empty;

    [Required]
    public string DiscountType { get; init; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal DiscountValue { get; init; }

    [Range(0, double.MaxValue)]
    public decimal? MinOrderAmt { get; init; }

    [Required]
    public DateOnly ValidFrom { get; init; }

    [Required]
    public DateOnly ValidTo { get; init; }

    [Range(1, int.MaxValue)]
    public int? UsageLimit { get; init; }

    public bool IsActive { get; init; }
}

/// <summary>
/// Request payload used to validate a promo code for a booking amount.
/// </summary>
public sealed class ValidatePromotionRequest
{
    [Required]
    public string PromoCode { get; init; } = string.Empty;

    [Range(0.01, double.MaxValue)]
    public decimal BookingAmount { get; init; }
}

/// <summary>
/// Validation result returned to customers before booking confirmation.
/// </summary>
public sealed record ValidatePromotionResponse(
    bool IsValid,
    Guid? PromotionId,
    decimal DiscountAmount,
    decimal FinalAmount,
    string Message);

/// <summary>
/// Represents a single promotion usage record.
/// </summary>
public sealed record PromotionUsageResponse(
    Guid Id,
    Guid PromotionId,
    string PromoCode,
    string PromotionName,
    Guid? CustomerId,
    Guid BookingId,
    DateTime UsedAt);

/// <summary>
/// Aggregate usage statistics for a promotion.
/// </summary>
public sealed record PromotionUsageStatisticsItem(
    Guid PromotionId,
    string PromoCode,
    string PromotionName,
    int UsageCount,
    decimal TotalDiscountAmount);

/// <summary>
/// Aggregate revenue statistics for a promotion.
/// </summary>
public sealed record PromotionRevenueStatisticsItem(
    Guid PromotionId,
    string PromoCode,
    string PromotionName,
    int UsageCount,
    decimal Revenue,
    decimal TotalDiscountAmount);

/// <summary>
/// Summary statistics for the promotion module.
/// </summary>
public sealed class PromotionStatisticsResponse
{
    public int TotalPromotions { get; init; }

    public int ActivePromotions { get; init; }

    public int ExpiredPromotions { get; init; }

    public int TotalUsages { get; init; }

    public decimal TotalDiscountAmount { get; init; }

    public IReadOnlyList<PromotionUsageStatisticsItem> MostUsedPromotions { get; init; }
        = Array.Empty<PromotionUsageStatisticsItem>();

    public IReadOnlyList<PromotionRevenueStatisticsItem> RevenueByPromotion { get; init; }
        = Array.Empty<PromotionRevenueStatisticsItem>();
}
