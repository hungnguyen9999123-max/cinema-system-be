using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CinemaSystem.Common.DTOs.Bookings;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.Enums;
using CinemaSystem.Services.Services.Bookings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CinemaSystem.API.Controllers;

[ApiController]
[Route("api")]
public class BookingsController(IBookingService bookingService) : ControllerBase
{
    [Authorize]
    [HttpPost("bookings")]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BookingResponseDto>>> CreateBooking(
        [FromBody] CreateBookingRequestDto request,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized(ApiResponse<BookingResponseDto>.Fail("Unauthorized access."));
        }

        var role = GetCurrentRole() ?? nameof(UserRole.Customer);

        if (string.IsNullOrWhiteSpace(request.Gateway))
        {
            request.Gateway = "VNPAY";
        }

        var response = await bookingService.CreateBookingAsync(customerId.Value, role, request, cancellationToken);

        var apiResponse = new BookingResponseDto
        {
            BookingId = response.BookingId,
            BookingRef = response.BookingRef,
            ExpiresAt = response.ExpiresAt,
            TotalAmount = response.TotalAmount,
            PromotionId = null,
            DiscountAmount = response.DiscountAmount,
            FinalAmount = response.FinalAmount,
            Status = response.BookingStatus,
            PaymentId = response.PaymentId,
            PaymentGateway = response.PaymentGateway,
            PaymentStatus = response.PaymentStatus,
            FnbOrders = response.FnbOrders.ToList()
        };

        var location = Url.Action(nameof(GetBooking), new { id = response.BookingId })
            ?? $"/api/bookings/{response.BookingId}";
        return Created(location, ApiResponse<BookingResponseDto>.Success(apiResponse, "Booking created successfully."));
    }

    [Authorize]
    [HttpGet("bookings/my-bookings")]
    [ProducesResponseType<ApiResponse<MyBookingsPagedResultDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<MyBookingsPagedResultDto>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<MyBookingsPagedResultDto>>> GetMyBookings(
        [FromQuery] string? status,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized(ApiResponse<MyBookingsPagedResultDto>.Fail("Unauthorized access."));
        }

        var query = new MyBookingsQueryRequest
        {
            Status = status,
            FromDate = fromDate,
            ToDate = toDate,
            Page = page,
            PageSize = pageSize
        };

        var result = await bookingService.GetMyBookingsAsync(customerId.Value, query, cancellationToken);
        return Ok(ApiResponse<MyBookingsPagedResultDto>.Success(result, "My bookings retrieved successfully."));
    }

    [Authorize]
    [HttpGet("bookings/{id}")]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<BookingResponseDto>>> GetBooking(
        Guid id,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized(ApiResponse<BookingResponseDto>.Fail("Unauthorized access."));
        }

        var role = GetCurrentRole() ?? nameof(UserRole.Customer);
        var response = await bookingService.GetBookingByIdAsync(id, customerId.Value, role, cancellationToken);
        return Ok(ApiResponse<BookingResponseDto>.Success(response, "Booking retrieved successfully."));
    }

    [Authorize]
    [HttpPost("bookings/{id}/cancel")]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ApiResponse<BookingResponseDto>>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ApiResponse<BookingResponseDto>>> CancelBooking(
        Guid id,
        CancellationToken cancellationToken)
    {
        var customerId = GetCurrentUserId();
        if (customerId is null)
        {
            return Unauthorized(ApiResponse<BookingResponseDto>.Fail("Unauthorized access."));
        }

        var role = GetCurrentRole() ?? nameof(UserRole.Customer);
        var response = await bookingService.CancelBookingAsync(id, customerId.Value, role, cancellationToken);
        return Ok(ApiResponse<BookingResponseDto>.Success(response, "Booking cancelled successfully."));
    }

    [HttpGet("showtimes/{showtimeId}/seats")]
    [ProducesResponseType<ApiResponse<IEnumerable<SeatMapItemDto>>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<IEnumerable<SeatMapItemDto>>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<IEnumerable<SeatMapItemDto>>>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IEnumerable<SeatMapItemDto>>>> GetSeatMap(
        Guid showtimeId,
        CancellationToken cancellationToken)
    {
        var response = await bookingService.GetSeatMapAsync(showtimeId, cancellationToken);
        return Ok(ApiResponse<IEnumerable<SeatMapItemDto>>.Success(response, "Seat map retrieved successfully."));
    }

    private Guid? GetCurrentUserId()
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        return Guid.TryParse(raw, out var userId) ? userId : null;
    }

    private string? GetCurrentRole()
    {
        return User.FindFirstValue(ClaimTypes.Role);
    }
}