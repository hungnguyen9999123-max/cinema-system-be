namespace CinemaSystem.Common.Constants;

/// <summary>
/// Centralized promotion feature messages.
/// </summary>
public static class PromotionMessages
{
    public const string RetrievedSuccessfully = "Promotions retrieved successfully.";
    public const string RetrievedDetailSuccessfully = "Promotion retrieved successfully.";
    public const string CreatedSuccessfully = "Promotion created successfully.";
    public const string UpdatedSuccessfully = "Promotion updated successfully.";
    public const string DeletedSuccessfully = "Promotion deleted successfully.";
    public const string ActivatedSuccessfully = "Promotion activated successfully.";
    public const string DeactivatedSuccessfully = "Promotion deactivated successfully.";
    public const string StatisticsRetrievedSuccessfully = "Promotion statistics retrieved successfully.";
    public const string UsagesRetrievedSuccessfully = "Promotion usages retrieved successfully.";
    public const string ValidationSucceeded = "Promotion is valid.";

    public const string NotFound = "Promotion not found.";
    public const string UsageNotFound = "Promotion usage not found.";
    public const string CodeRequired = "Promo code is required.";
    public const string CodeAlreadyExists = "Promo code already exists.";
    public const string NameRequired = "Promotion name is required.";
    public const string DiscountTypeRequired = "Discount type is required.";
    public const string InvalidDiscountType = "Discount type must be PERCENTAGE or FIXED_AMOUNT.";
    public const string InvalidDiscountValue = "Discount value must be greater than zero.";
    public const string InvalidPercentageDiscount = "Percentage discount cannot exceed 100.";
    public const string InvalidDateRange = "Valid from date must be on or before valid to date.";
    public const string InvalidUsageLimit = "Usage limit must be greater than zero.";
    public const string InvalidMinOrderAmount = "Minimum order amount must be greater than or equal to zero.";
    public const string CannotDeleteBecauseUsed = "Promotion cannot be deleted because it has already been used.";
    public const string CannotDeleteBecauseReferenced = "Promotion cannot be deleted because it is referenced by bookings.";
    public const string Inactive = "Promotion is inactive.";
    public const string Expired = "Promotion is expired.";
    public const string NotStarted = "Promotion is not yet valid.";
    public const string UsageLimitExceeded = "Promotion usage limit has been reached.";
    public const string MinimumOrderNotMet = "Booking amount does not meet the minimum order amount.";
    public const string PromotionCodeNotFound = "Promotion code not found.";
    public const string InvalidBookingAmount = "Booking amount must be greater than zero.";
    public const string AlreadyApplied = "Promotion has already been applied to this booking.";
}
