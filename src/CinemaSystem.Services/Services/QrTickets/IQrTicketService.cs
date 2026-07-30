using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Bookings;
using CinemaSystem.Common.DTOs.QrTickets;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.DAL.Models;
using System;
using System.Collections.Generic;

namespace CinemaSystem.Services.Services.QrTickets;
public interface IQrTicketService
{
    string GenerateToken();
    Task<ApiResponse<int>> GenerateTicketsForBookingAsync(Booking booking, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> GenerateTicketsForBookingAsync(Booking booking, DateTime expiredAtUtc, CancellationToken cancellationToken);
    Task<ApiResponse<BookingTicketsResponseDto>> GenerateTicketsForBookingAsync(
        Guid bookingId,
        Guid customerId,
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<GenerateQrResponseDto>> GenerateQrAsync(
        Guid ticketId,
        Guid customerId,
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<BookingTicketsResponseDto>> GetQrByBookingAsync(
        Guid bookingId,
        Guid customerId,
        GenerateQrRequestDto request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<VerifyQrResponseDto>> ValidateQrAsync(
        VerifyQrRequestDto request,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<VerifyQrResponseDto>> CheckInAsync(
        VerifyQrRequestDto request,
        Guid staffId,
        CancellationToken cancellationToken = default);
    Task<PagedResult<CheckInHistoryItemDto>> GetCheckInHistoryAsync(
        CheckInHistorySearchRequest request,
        CancellationToken cancellationToken = default);
    string RenderQrImageBase64(string token, string format = "BASE64");
}
