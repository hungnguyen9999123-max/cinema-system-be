using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Bookings;
using CinemaSystem.Common.Helpers;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.Services.Services.Promotions;
using CinemaSystem.Services.Services.QrTickets;
using Microsoft.Extensions.Logging;
using QRCoder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.Services.Services.Pos;

public class PosBookingConfirmationService : IPosBookingConfirmationService
{
    private const string PaymentPending = "PENDING";
    private const string PaymentPaid = "SUCCESS";
    private const string BookingConfirmed = "CONFIRMED";
    private const string BookingExpired = "EXPIRED";
    private const string SeatHeld = "HELD";
    private const string SeatBooked = "BOOKED";
    private const string GatewayCash = "CASH";

    private readonly IPaymentRepository _paymentRepository;
    private readonly IBookingRepository _bookingRepository;
    private readonly IQrTicketService _qrTicketService;
    private readonly IPromotionService _promotionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PosBookingConfirmationService> _logger;

    public PosBookingConfirmationService(
        IPaymentRepository paymentRepository,
        IBookingRepository bookingRepository,
        IQrTicketService qrTicketService,
        IPromotionService promotionService,
        IUnitOfWork unitOfWork,
        ILogger<PosBookingConfirmationService> logger)
    {
        _paymentRepository = paymentRepository;
        _bookingRepository = bookingRepository;
        _qrTicketService = qrTicketService;
        _promotionService = promotionService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<IReadOnlyList<BookingTicketDto>> ConfirmCashPaymentAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var payment = await _paymentRepository.GetByIdAsync(paymentId, cancellationToken);
            if (payment is null)
            {
                throw new KeyNotFoundException(PosMessages.BookingNotFound);
            }

            if (!string.Equals(payment.Gateway, GatewayCash, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "ConfirmCashPaymentAsync is only valid for CASH payments.");
            }

            if (!string.Equals(payment.Status, PaymentPending, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(QrTicketMessages.PaymentNotPending);
            }

            var booking = payment.Booking;
            if (booking is null)
            {
                throw new KeyNotFoundException(QrTicketMessages.BookingNotFound);
            }

            if (string.Equals(booking.Status, BookingConfirmed, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(QrTicketMessages.BookingAlreadyConfirmed);
            }

            if (string.Equals(booking.Status, BookingExpired, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(QrTicketMessages.BookingExpired);
            }

            var now = DateTime.UtcNow;
            payment.Status = PaymentPaid;
            payment.PaidAt = now;
            _paymentRepository.Update(payment);

            booking.Status = BookingConfirmed;
            foreach (var bookingSeat in booking.BookingSeatBookings)
            {
                if (string.Equals(bookingSeat.SeatStatus, SeatHeld, StringComparison.OrdinalIgnoreCase))
                {
                    bookingSeat.SeatStatus = SeatBooked;
                }
            }

            if (booking.PromotionId.HasValue)
            {
                await _promotionService.RecordUsageAsync(
                    booking.Id, booking.CustomerId, booking.PromotionId.Value, cancellationToken);
            }

            // IQrTicketService.GenerateTicketsForBookingAsync needs the booking
            // graph with Showtime + Tickets + BookingSeatBookings already loaded.
            // Re-fetch via the dedicated ticket-generation helper so we keep
            // using the existing pipeline without duplicating it.
            var bookingForGeneration = await _bookingRepository
                .GetByIdForTicketGenerationAsync(booking.Id, cancellationToken);
            if (bookingForGeneration is null)
            {
                throw new KeyNotFoundException(QrTicketMessages.BookingNotFound);
            }

            var ticketExpiredAt = bookingForGeneration.Showtime is null
                ? now.AddYears(1)
                : CinemaTime.ToUtc(bookingForGeneration.Showtime.EndTime);

            var generationResult = await _qrTicketService.GenerateTicketsForBookingAsync(
                bookingForGeneration, ticketExpiredAt, cancellationToken);

            if (!generationResult.IsSuccess)
            {
                throw new InvalidOperationException(generationResult.Message ?? PosMessages.TicketGenerationFailed);
            }

            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            var tickets = bookingForGeneration.Tickets.ToList();
            _logger.LogInformation(
                "POS confirmed booking {BookingRef}, generated {TicketCount} ticket(s).",
                booking.BookingRef, tickets.Count);

            return tickets.Select(t => new BookingTicketDto(
                t.Id,
                t.BookingSeatId,
                t.BookingSeat?.SeatId ?? Guid.Empty,
                t.BookingSeat?.Seat?.SeatLabel ?? string.Empty,
                t.QrCode,
                RenderQrDataUrl(t.QrCode))).ToList();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static string RenderQrDataUrl(string token)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q);
        using var qrCode = new PngByteQRCode(data);
        var pngBytes = qrCode.GetGraphic(20);
        return $"data:image/png;base64,{Convert.ToBase64String(pngBytes)}";
    }
}
