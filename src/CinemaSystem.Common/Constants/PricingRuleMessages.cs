namespace CinemaSystem.Common.Constants;

public static class PricingRuleMessages
{
    public const string RetrievedSuccessfully = "Pricing rules retrieved successfully.";
    public const string RetrievedDetailSuccessfully = "Pricing rule retrieved successfully.";
    public const string CreatedSuccessfully = "Pricing rule created successfully.";
    public const string UpdatedSuccessfully = "Pricing rule updated successfully.";
    public const string RegeneratedSuccessfully = "Default pricing rules regenerated successfully.";
    public const string DeletedSuccessfully = "Pricing rule deleted successfully.";
    public const string NotFound = "Pricing rule not found.";
    public const string CinemaNotFound = "Cinema not found.";
    public const string InvalidRoomTypeId = "Invalid room type id.";
    public const string InvalidTimeSlotId = "Invalid time slot id.";
    public const string InvalidBasePrice = "Base price must be greater than zero.";
    public const string InvalidTimeMultiplier = "Time multiplier must be greater than zero.";
    public const string InvalidEffectiveDateRange = "Effective from date must be on or before effective to date.";
    public const string ActiveRuleAlreadyExists = "An active pricing rule already exists for this cinema, room type, time slot, and date range.";
    public const string CannotDeleteBecauseHasBookings = "Pricing rule cannot be deleted because it is referenced by bookings.";
    public const string PriceCalculatedSuccessfully = "Ticket price calculated successfully.";
    public const string ShowtimeNotFound = "Showtime not found.";
    public const string SeatNotFound = "Seat not found.";
    public const string AudienceTypeNotFound = "Audience type not found or inactive.";
    public const string NoApplicablePricingRule = "No active pricing rule found for this showtime.";
    public const string SeatRoomMismatch = "Seat does not belong to the showtime room.";
    public const string DefaultRulesAlreadyExist = "Default pricing rules already exist for this cinema.";
    public const string DefaultRulesGeneratedSuccessfully = "Default pricing rules generated successfully.";
    public const string CinemaIdRequired = "Cinema id is required.";
    public const string SeatIdsRequired = "At least one seat id is required.";
    public const string DuplicateSeatIds = "Duplicate seat ids are not allowed.";
    public const string SeatNotFoundInRoom = "One or more seats do not belong to the showtime room.";
}
