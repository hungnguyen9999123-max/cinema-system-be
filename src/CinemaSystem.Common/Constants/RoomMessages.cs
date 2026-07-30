namespace CinemaSystem.Common.Constants;

public static class RoomMessages
{
    public const string RoomsRetrievedSuccessfully = "Rooms retrieved successfully.";
    public const string RoomRetrievedSuccessfully = "Room retrieved successfully.";
    public const string RoomCreatedSuccessfully = "Room created successfully.";
    public const string RoomUpdatedSuccessfully = "Room updated successfully.";
    public const string RoomDeletedSuccessfully = "Room deleted successfully.";
    public const string SeatLayoutRetrievedSuccessfully = "Seat layout retrieved successfully.";
    public const string SeatLayoutGeneratedSuccessfully = "Seat layout generated successfully.";
    public const string SeatCreatedSuccessfully = "Seat created successfully.";
    public const string SeatUpdatedSuccessfully = "Seat updated successfully.";
    public const string SeatDeletedSuccessfully = "Seat deleted successfully.";
    public const string SeatDisabledBecauseHasBookingHistory = "Seat disabled because it has booking history.";
    public const string RoomNotFound = "Room not found.";
    public const string SeatNotFound = "Seat not found.";
    public const string CinemaNotFound = "Cinema not found.";
    public const string RoomNameAlreadyExists = "Room name already exists in this cinema.";
    public const string RoomCannotBeDeletedBecauseHasActiveShowtimes = "Room cannot be deleted because it still has active showtimes.";
    public const string RoomCannotBeDeletedBecauseSeatsHaveBookingHistory = "Room cannot be deleted because some seats already have booking history.";
    public const string RoomAlreadyHasSeats = "Room already has seats. Set ReplaceExisting=true to regenerate layout.";
    public const string SeatLabelsAlreadyHaveBookingHistory = "Some seats already have booking history and cannot be replaced.";
    public const string SeatLabelAlreadyExists = "Seat label already exists in this room.";
    public const string SeatTypeNotFoundOrInactive = "Seat type not found or inactive.";
    public const string DefaultSeatTypeNotFoundOrInactive = "Default seat type not found or inactive.";
    public const string OverrideSeatTypeNotFoundOrInactive = "Override seat type not found or inactive.";
    public const string SeatRowLetterInvalid = "Row letter must be a single letter from A to Z.";
    public const string SeatLayoutOverrideRangeInvalid = "Seat layout override range is invalid.";
    public const string SeatLayoutOverrideColumnInvalid = "Seat layout override columns are invalid.";
    public const string SeatTypeNotAvailable = "Seat type is not available.";
    public const string RoomCinemaMappingInvalid = "Room cinema mapping is invalid.";
}
