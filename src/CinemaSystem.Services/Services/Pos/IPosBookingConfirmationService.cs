using CinemaSystem.Common.DTOs.Bookings;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.Services.Services.Pos;

/// <summary>
/// Handles the "instant ticket" path used by POS staff when a customer pays
/// cash at the counter — flips the pre-created PENDING payment to PAID,
/// confirms the booking and generates the QR tickets in a single transaction.
/// Online customer flow keeps using the existing VNPay IPN handler.
/// </summary>
public interface IPosBookingConfirmationService
{
    Task<IReadOnlyList<BookingTicketDto>> ConfirmCashPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default);
}
