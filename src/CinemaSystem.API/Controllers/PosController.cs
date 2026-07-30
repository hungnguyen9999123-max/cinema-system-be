using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Bookings;
using CinemaSystem.Common.DTOs.Pos;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.Services.Services.Pos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Authorize(Roles = "Admin,Manager,Staff")]
[Route("api/pos")]
public sealed class PosController(
    IPosBookingService posBookingService,
    IPosBookingConfirmationService posConfirmationService,
    IPaymentRepository paymentRepository,
    IBookingRepository bookingRepository) : ControllerBase
{
    [HttpPost("tickets")]
    [ProducesResponseType<ApiResponse<PosCreateTicketResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<PosCreateTicketResponse>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<PosCreateTicketResponse>>> CreateTicket(
        [FromBody] CreatePosBookingRequest request,
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        if (staffId is null)
        {
            return Unauthorized(ApiResponse<PosCreateTicketResponse>.Fail("Unauthorized access."));
        }

        var result = await posBookingService.CreatePosTicketAsync(
            staffId.Value,
            request,
            cancellationToken);

        var message = result.IsVnpay
            ? PosMessages.VnpayPaymentCreated
            : "Ve dang cho xac nhan. Vui long nhan OK de hoan tat va in QR.";

        return Ok(ApiResponse<PosCreateTicketResponse>.Success(result, message));
    }

    /// <summary>
    /// Step 2 of the CASH flow: flips a previously created PENDING payment to
    /// PAID, the booking to CONFIRMED, releases the held seats as BOOKED and
    /// generates the QR tickets. Idempotent on the data side because
    /// <see cref="PosBookingConfirmationService.ConfirmCashPaymentAsync"/>
    /// rejects any payment whose status is not PENDING.
    /// </summary>
    [HttpPost("tickets/{paymentId}/confirm")]
    [ProducesResponseType<ApiResponse<PosCreateTicketResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<PosCreateTicketResponse>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<ActionResult<ApiResponse<PosCreateTicketResponse>>> ConfirmCashTicket(
        Guid paymentId,
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        if (staffId is null)
        {
            return Unauthorized(ApiResponse<PosCreateTicketResponse>.Fail("Unauthorized access."));
        }

        IReadOnlyList<BookingTicketDto> tickets;
        try
        {
            tickets = await posConfirmationService
                .ConfirmCashPaymentAsync(paymentId, cancellationToken);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(ApiResponse<PosCreateTicketResponse>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex) when (IsGoneMessage(ex.Message))
        {
            return StatusCode(StatusCodes.Status410Gone,
                ApiResponse<PosCreateTicketResponse>.Fail(ex.Message));
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ApiResponse<PosCreateTicketResponse>.Fail(ex.Message));
        }

        var payment = await paymentRepository.GetByIdAsync(paymentId, cancellationToken);
        var booking = payment?.Booking;

        var response = new PosBookingResponse
        {
            BookingId = booking?.Id ?? Guid.Empty,
            BookingRef = booking?.BookingRef ?? string.Empty,
            MovieTitle = booking?.Showtime?.Movie?.Title ?? string.Empty,
            CinemaName = booking?.Showtime?.Room?.Cinema?.Name ?? string.Empty,
            RoomName = booking?.Showtime?.Room?.Name ?? string.Empty,
            ShowtimeStart = booking?.Showtime?.StartTime ?? DateTime.MinValue,
            ShowtimeEnd = booking?.Showtime?.EndTime ?? DateTime.MinValue,
            PaymentGateway = PosMessages.GatewayCash,
            Tickets = tickets.Select(t => new PosTicketItem
            {
                TicketId = t.TicketId,
                SeatLabel = t.SeatLabel,
                QrImageBase64 = t.QrImageBase64,
                Token = t.Token
            }).ToList(),
            SeatLabels = tickets
                .Select(t => t.SeatLabel)
                .Where(label => !string.IsNullOrEmpty(label))
                .OrderBy(label => label, StringComparer.Ordinal)
                .ToList(),
            TotalAmount = payment?.Amount ?? 0,
            DiscountAmount = 0,
            FinalAmount = payment?.Amount ?? 0,
            IsPendingConfirmation = false
        };

        return Ok(ApiResponse<PosCreateTicketResponse>.Success(
            new PosCreateTicketResponse
            {
                IsVnpay = false,
                Cash = response
            },
            PosMessages.TicketCreated));
    }

    /// <summary>
    /// Looks up any booking (online or POS) by its <c>bookingRef</c>. Used by
    /// staff when a customer returns to the counter holding a printed ticket
    /// or when they need to inspect a sale from earlier in the day.
    /// </summary>
    [HttpGet("tickets/by-ref/{bookingRef}")]
    [ProducesResponseType<ApiResponse<PosBookingLookupResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PosBookingLookupResponse>>> GetByRef(
        string bookingRef,
        CancellationToken cancellationToken)
    {
        var staffId = GetCurrentUserId();
        if (staffId is null)
        {
            return Unauthorized(ApiResponse<PosBookingLookupResponse>.Fail("Unauthorized access."));
        }

        var booking = await bookingRepository
            .GetBookingByRefWithDetailsAsync(bookingRef, cancellationToken);

        if (booking is null)
        {
            return NotFound(ApiResponse<PosBookingLookupResponse>.Fail(
                QrTicketMessages.BookingNotFound));
        }

        return Ok(ApiResponse<PosBookingLookupResponse>.Success(
            MapToLookupResponse(booking),
            "Lookup booking thanh cong."));
    }

    private static PosBookingLookupResponse MapToLookupResponse(Booking booking)
    {
        var showtime = booking.Showtime;
        var seatLabels = booking.BookingSeatBookings
            .Select(bs => bs.Seat?.SeatLabel)
            .Where(label => !string.IsNullOrEmpty(label))
            .Cast<string>()
            .OrderBy(label => label, StringComparer.Ordinal)
            .ToList();

        return new PosBookingLookupResponse
        {
            BookingId = booking.Id,
            BookingRef = booking.BookingRef,
            BookingStatus = booking.Status,
            BookedAt = CinemaTime.ToLocal(booking.BookedAt),
            ExpiresAt = booking.ExpiresAt == default
                ? null
                : CinemaTime.ToLocal(booking.ExpiresAt),

            ShowtimeId = booking.ShowtimeId,
            MovieTitle = showtime?.Movie?.Title ?? string.Empty,
            CinemaName = showtime?.Cinema?.Name ?? string.Empty,
            RoomName = showtime?.Room?.Name ?? string.Empty,
            ShowtimeStart = showtime is null
                ? default
                : CinemaTime.ToLocal(showtime.StartTime),
            ShowtimeEnd = showtime is null
                ? default
                : CinemaTime.ToLocal(showtime.EndTime),

            SeatLabels = seatLabels,
            TotalAmount = booking.TotalAmount,
            DiscountAmount = booking.DiscountAmount,
            FinalAmount = booking.FinalAmount,

            Payments = booking.Payments
                .Select(p => new PosLookupPayment
                {
                    PaymentId = p.Id,
                    Gateway = p.Gateway,
                    Status = p.Status,
                    Amount = p.Amount,
                    PaidAt = p.PaidAt.HasValue ? CinemaTime.ToLocal(p.PaidAt.Value) : null
                })
                .ToList(),
            Tickets = booking.Tickets
                .Select(t => new PosTicketItem
                {
                    TicketId = t.Id,
                    SeatLabel = t.BookingSeat?.Seat?.SeatLabel ?? string.Empty,
                    QrImageBase64 = string.Empty,
                    Token = t.QrCode
                })
                .ToList()
        };
    }

    private static bool IsGoneMessage(string message) =>
        message.Contains(QrTicketMessages.BookingExpired, StringComparison.OrdinalIgnoreCase);

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }
}
