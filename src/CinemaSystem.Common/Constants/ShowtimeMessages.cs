namespace CinemaSystem.Common.Constants;

public static class ShowtimeMessages
{
    public const string ShowtimesRetrievedSuccessfully = "Showtimes retrieved successfully.";
    public const string ShowtimeRetrievedSuccessfully = "Showtime retrieved successfully.";
    public const string ShowtimeCreatedSuccessfully = "Showtime created successfully.";
    public const string ShowtimeUpdatedSuccessfully = "Showtime updated successfully.";
    public const string ShowtimeDeletedSuccessfully = "Showtime deleted successfully.";
    public const string ShowtimeCancelledBecauseHasBookingHistory = "Showtime cancelled because it has booking history.";
    public const string ShowtimeNotFound = "Showtime not found.";
    public const string MovieNotFound = "Movie not found.";
    public const string RoomNotFound = "Room not found.";
    public const string EndTimeMustBeGreaterThanStartTime = "EndTime must be greater than StartTime.";
    public const string ShowtimeOverlap = "Showtime overlaps with an existing showtime in the same room.";
    public const string ShowtimeGapTooShort = "There must be at least a 15-minute gap between showtimes in the same room.";
    public const string ShowtimeStartTimeCannotBeInPast = "Showtime start time cannot be in the past.";
    public const string UserIdClaimMissingOrInvalid = "User id claim is missing or invalid.";
}

