namespace CinemaSystem.Common.Constants;

public static class QrTicketMessages
{
    public const string TicketsRetrievedSuccessfully = "Tickets retrieved successfully.";
    public const string TicketsGeneratedSuccessfully = "Tickets generated successfully.";
    public const string QrRetrievedSuccessfully = "QR code retrieved successfully.";
    public const string QrValidatedSuccessfully = "QR code validated successfully.";
    public const string CheckInSuccessful = "Check-in successful.";
    public const string CheckInHistoryRetrievedSuccessfully = "Check-in history retrieved successfully.";

    public const string TicketNotFound = "Ticket not found.";
    public const string BookingNotFound = "Booking not found.";
    public const string BookingCancelled = "Cannot generate tickets for a cancelled booking.";
    public const string BookingExpired = "Cannot generate tickets for an expired booking.";
    public const string BookingPending = "Cannot generate tickets before payment is completed.";
    public const string BookingNotConfirmed = "Booking is not confirmed.";
    public const string BookingNotOwnedByCustomer = "You do not have access to this booking.";
    public const string PaymentNotCompleted = "Payment has not been completed for this booking.";
    public const string TicketAlreadyUsed = "This ticket has already been checked in.";
    public const string TicketExpired = "This ticket has expired.";
    public const string TicketCancelled = "This ticket has been cancelled.";
    public const string TicketNotValid = "This ticket is not valid for check-in.";
    public const string CheckInWindowNotOpen = "Check-in is not available at this time.";
    public const string InvalidToken = "Invalid QR token.";
    public const string UserIdClaimMissingOrInvalid = "User id claim is missing or invalid.";
    public const string PaymentNotPending = "Payment is not pending; cannot mark as paid.";
    public const string BookingAlreadyConfirmed = "Booking is already confirmed.";
}
