    using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.QrTickets;
using CinemaSystem.Common.DTOs.Responses;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.Common.Helpers;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.DAL.Infrastructure;
using Microsoft.Extensions.Logging;
using QRCoder;

    namespace CinemaSystem.Services.Services.QrTickets;

    public class QrTicketService : IQrTicketService
    {
        private const string BookingConfirmed = "CONFIRMED";
        private const string PaymentPaid = "SUCCESS";

        private readonly IQrTicketRepository _qrTicketRepository;
        private readonly IBookingRepository _bookingRepository;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<QrTicketService> _logger;

        public QrTicketService(
            IQrTicketRepository qrTicketRepository,
            IBookingRepository bookingRepository,
            IUnitOfWork unitOfWork,
            ILogger<QrTicketService> logger)
        {
            _qrTicketRepository = qrTicketRepository;
            _bookingRepository = bookingRepository;
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        public string GenerateToken() => QrTokenGenerator.GenerateToken();

        public async Task<ApiResponse<int>> GenerateTicketsForBookingAsync(
            Booking booking,
            CancellationToken cancellationToken = default)
        {
            var endTimeUtc = booking.Showtime is null
                ? DateTime.UtcNow.AddYears(1)
                : CinemaTime.ToUtc(booking.Showtime.EndTime);
            return await GenerateTicketsForBookingAsync(booking, endTimeUtc, cancellationToken);
        }

        public async Task<ApiResponse<int>> GenerateTicketsForBookingAsync(
            Booking booking,
            DateTime expiredAtUtc,
            CancellationToken cancellationToken)
        {
            var validationMessage = GetBookingTicketGenerationError(booking);
            if (validationMessage is not null)
            {
                return ApiResponse<int>.Fail(validationMessage);
            }

            var existingBookingSeatIds = booking.Tickets
                .Select(ticket => ticket.BookingSeatId)
                .ToHashSet();

            var tickets = booking.BookingSeatBookings
                .Where(bookingSeat => !existingBookingSeatIds.Contains(bookingSeat.Id))
                .Select(bookingSeat => new Ticket
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    BookingSeatId = bookingSeat.Id,
                    QrCode = GenerateToken(),
                    QrPayload = "{}",
                    Status = TicketStatus.Valid,
                    GeneratedAt = DateTime.UtcNow,
                    ExpiredAt = expiredAtUtc
                })
                .ToList();

            if (tickets.Count == 0)
            {
                return ApiResponse<int>.Success(0, QrTicketMessages.TicketsGeneratedSuccessfully);
            }

            await _qrTicketRepository.AddRangeAsync(tickets, cancellationToken);
            foreach (var ticket in tickets)
            {
                booking.Tickets.Add(ticket);
            }

            return ApiResponse<int>.Success(tickets.Count, QrTicketMessages.TicketsGeneratedSuccessfully);
        }

        public async Task<ApiResponse<BookingTicketsResponseDto>> GenerateTicketsForBookingAsync(
            Guid bookingId,
            Guid customerId,
            GenerateQrRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var booking = await _bookingRepository.GetByIdForTicketGenerationAsync(bookingId, cancellationToken);
            if (booking is null)
            {
                return ApiResponse<BookingTicketsResponseDto>.Fail(QrTicketMessages.BookingNotFound);
            }

            if (booking.CustomerId != customerId)
            {
                return ApiResponse<BookingTicketsResponseDto>.Fail(QrTicketMessages.BookingNotOwnedByCustomer);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var generationResult = await GenerateTicketsForBookingAsync(booking, cancellationToken);
                if (!generationResult.IsSuccess)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    return ApiResponse<BookingTicketsResponseDto>.Fail(generationResult.Message);
                }

                await _unitOfWork.CommitTransactionAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                _logger.LogError(ex, "Error generating tickets for booking {BookingId}.", bookingId);
                throw;
            }

            var response = await BuildBookingTicketsResponseAsync(bookingId, booking.BookingRef, request, cancellationToken);
            return ApiResponse<BookingTicketsResponseDto>.Success(response, QrTicketMessages.TicketsGeneratedSuccessfully);
        }

        public async Task<ApiResponse<GenerateQrResponseDto>> GenerateQrAsync(
            Guid ticketId,
            Guid customerId,
            GenerateQrRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var ticket = await _qrTicketRepository.GetByIdWithDetailsAsync(ticketId, cancellationToken);
            if (ticket is null)
            {
                return ApiResponse<GenerateQrResponseDto>.Fail(QrTicketMessages.TicketNotFound);
            }

            var ownershipError = GetCustomerBookingOwnershipError(ticket.Booking, customerId);
            if (ownershipError is not null)
            {
                return ApiResponse<GenerateQrResponseDto>.Fail(ownershipError);
            }

            var ticketDisplayError = GetTicketDisplayError(ticket);
            if (ticketDisplayError is not null)
            {
                return ApiResponse<GenerateQrResponseDto>.Fail(ticketDisplayError);
            }

            return ApiResponse<GenerateQrResponseDto>.Success(
                ToGenerateQrResponse(ticket, request.Format),
                QrTicketMessages.QrRetrievedSuccessfully);
        }

        public async Task<ApiResponse<BookingTicketsResponseDto>> GetQrByBookingAsync(
            Guid bookingId,
            Guid customerId,
            GenerateQrRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            if (booking is null)
            {
                return ApiResponse<BookingTicketsResponseDto>.Fail(QrTicketMessages.BookingNotFound);
            }

            if (booking.CustomerId != customerId)
            {
                return ApiResponse<BookingTicketsResponseDto>.Fail(QrTicketMessages.BookingNotOwnedByCustomer);
            }

            if (!string.Equals(booking.Status, BookingConfirmed, StringComparison.OrdinalIgnoreCase))
            {
                return ApiResponse<BookingTicketsResponseDto>.Fail(QrTicketMessages.BookingNotConfirmed);
            }

            var tickets = await _qrTicketRepository.GetByBookingIdAsync(bookingId, cancellationToken);
            if (tickets.Count == 0)
            {
                var bookingForGeneration = await _bookingRepository.GetByIdForTicketGenerationAsync(bookingId, cancellationToken);
                if (bookingForGeneration is null)
                {
                    return ApiResponse<BookingTicketsResponseDto>.Fail(QrTicketMessages.BookingNotFound);
                }

                await _unitOfWork.BeginTransactionAsync(cancellationToken);
                try
                {
                    var generationResult = await GenerateTicketsForBookingAsync(bookingForGeneration, cancellationToken);
                    if (!generationResult.IsSuccess)
                    {
                        await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                        return ApiResponse<BookingTicketsResponseDto>.Fail(generationResult.Message);
                    }

                    await _unitOfWork.CommitTransactionAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    _logger.LogError(ex, "Error auto-generating tickets for booking {BookingId}.", bookingId);
                    throw;
                }

                tickets = await _qrTicketRepository.GetByBookingIdAsync(bookingId, cancellationToken);
            }

            if (tickets.Count == 0)
            {
                return ApiResponse<BookingTicketsResponseDto>.Fail(QrTicketMessages.TicketNotFound);
            }

            return ApiResponse<BookingTicketsResponseDto>.Success(
                BuildBookingTicketsResponse(bookingId, booking.BookingRef, request, tickets),
                QrTicketMessages.TicketsRetrievedSuccessfully);
        }

        public async Task<ApiResponse<VerifyQrResponseDto>> ValidateQrAsync(
            VerifyQrRequestDto request,
            CancellationToken cancellationToken = default)
        {
            var token = NormalizeToken(request.Token);
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException(QrTicketMessages.InvalidToken);
            }

            var ticket = await _qrTicketRepository.GetByTokenAsync(token, cancellationToken);
            if (ticket is null)
            {
                throw new KeyNotFoundException(QrTicketMessages.TicketNotFound);
            }

            var validation = BuildValidationResponse(ticket, includeAlreadyUsedDetails: false);
            return ApiResponse<VerifyQrResponseDto>.Success(validation, QrTicketMessages.QrValidatedSuccessfully);
        }

        public async Task<ApiResponse<VerifyQrResponseDto>> CheckInAsync(
            VerifyQrRequestDto request,
            Guid staffId,
            CancellationToken cancellationToken = default)
        {
            var token = NormalizeToken(request.Token);
            if (string.IsNullOrEmpty(token))
            {
                throw new InvalidOperationException(QrTicketMessages.InvalidToken);
            }

            await _unitOfWork.BeginTransactionAsync(cancellationToken);
            try
            {
                var ticket = await _qrTicketRepository.GetByTokenAsync(token, cancellationToken);
                if (ticket is null)
                {
                    throw new KeyNotFoundException(QrTicketMessages.TicketNotFound);
                }

                var validation = BuildValidationResponse(ticket, includeAlreadyUsedDetails: true);
                if (!validation.IsValid)
                {
                    if (string.Equals(validation.CheckInStatus, CheckInStatus.AlreadyUsed, StringComparison.Ordinal))
                    {
                        throw new BusinessConflictException(validation.Message ?? QrTicketMessages.TicketAlreadyUsed);
                    }

                    throw new InvalidOperationException(validation.Message ?? QrTicketMessages.TicketNotValid);
                }

                ticket.Status = TicketStatus.Scanned;
                ticket.ScannedAt = DateTime.UtcNow;
                ticket.ScannedBy = staffId;
                _qrTicketRepository.Update(ticket);

                await _unitOfWork.CommitTransactionAsync(cancellationToken);

                return ApiResponse<VerifyQrResponseDto>.Success(
                    BuildSuccessResponse(ticket, CheckInStatus.Success, QrTicketMessages.CheckInSuccessful),
                    QrTicketMessages.CheckInSuccessful);
            }
            catch
            {
                await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                throw;
            }
        }

        public async Task<PagedResult<CheckInHistoryItemDto>> GetCheckInHistoryAsync(
            CheckInHistorySearchRequest request,
            CancellationToken cancellationToken = default)
        {
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize < 1 ? 20 : Math.Min(request.PageSize, 100);

            var (items, totalCount) = await _qrTicketRepository.GetCheckInHistoryAsync(
                request with { Page = page, PageSize = pageSize },
                cancellationToken);

            var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

            return new PagedResult<CheckInHistoryItemDto>(
                items.Select(ToCheckInHistoryItem).ToList(),
                page,
                pageSize,
                totalCount,
                totalPages);
        }

        private static string? GetBookingTicketGenerationError(Booking booking)
        {
            if (string.Equals(booking.Status, "CANCELLED", StringComparison.OrdinalIgnoreCase))
            {
                return QrTicketMessages.BookingCancelled;
            }

            if (string.Equals(booking.Status, "EXPIRED", StringComparison.OrdinalIgnoreCase))
            {
                return QrTicketMessages.BookingExpired;
            }

            if (string.Equals(booking.Status, "PENDING", StringComparison.OrdinalIgnoreCase))
            {
                return QrTicketMessages.BookingPending;
            }

            if (!string.Equals(booking.Status, BookingConfirmed, StringComparison.OrdinalIgnoreCase))
            {
                return QrTicketMessages.BookingNotConfirmed;
            }

            if (!HasPaidPayment(booking))
            {
                return QrTicketMessages.PaymentNotCompleted;
            }

            if (booking.Showtime is null)
            {
                return QrTicketMessages.BookingNotFound;
            }

            if (booking.BookingSeatBookings.Count == 0)
            {
                return QrTicketMessages.TicketNotFound;
            }

            return null;
        }

        private async Task<BookingTicketsResponseDto> BuildBookingTicketsResponseAsync(
            Guid bookingId,
            string bookingRef,
            GenerateQrRequestDto request,
            CancellationToken cancellationToken)
        {
            var tickets = await _qrTicketRepository.GetByBookingIdAsync(bookingId, cancellationToken);
            return BuildBookingTicketsResponse(bookingId, bookingRef, request, tickets);
        }

        private static BookingTicketsResponseDto BuildBookingTicketsResponse(
            Guid bookingId,
            string bookingRef,
            GenerateQrRequestDto request,
            IReadOnlyList<Ticket> tickets) =>
            new()
            {
                BookingId = bookingId,
                BookingRef = bookingRef,
                Tickets = tickets
                    .Select(ticket => ToGenerateQrResponse(ticket, request.Format))
                    .ToList()
            };

        private static string? GetCustomerBookingOwnershipError(Booking booking, Guid customerId)
        {
            if (booking.CustomerId != customerId)
            {
                return QrTicketMessages.BookingNotOwnedByCustomer;
            }

            return null;
        }

        private static string? GetTicketDisplayError(Ticket ticket)
        {
            if (!string.Equals(ticket.Booking.Status, BookingConfirmed, StringComparison.OrdinalIgnoreCase))
            {
                return QrTicketMessages.BookingNotConfirmed;
            }

            if (!HasPaidPayment(ticket.Booking))
            {
                return QrTicketMessages.PaymentNotCompleted;
            }

            if (string.Equals(ticket.Status, TicketStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                return QrTicketMessages.TicketCancelled;
            }

            return null;
        }

        private static VerifyQrResponseDto BuildValidationResponse(Ticket ticket, bool includeAlreadyUsedDetails)
        {
            if (!string.Equals(ticket.Booking.Status, BookingConfirmed, StringComparison.OrdinalIgnoreCase))
            {
                return InvalidResponse(CheckInStatus.Invalid, QrTicketMessages.BookingNotConfirmed);
            }

            if (!HasPaidPayment(ticket.Booking))
            {
                return InvalidResponse(CheckInStatus.Invalid, QrTicketMessages.PaymentNotCompleted);
            }

            if (string.Equals(ticket.Status, TicketStatus.Scanned, StringComparison.OrdinalIgnoreCase))
            {
                if (includeAlreadyUsedDetails)
                {
                    return BuildSuccessResponse(ticket, CheckInStatus.AlreadyUsed, QrTicketMessages.TicketAlreadyUsed);
                }

                return InvalidResponse(CheckInStatus.AlreadyUsed, QrTicketMessages.TicketAlreadyUsed);
            }

            if (string.Equals(ticket.Status, TicketStatus.Cancelled, StringComparison.OrdinalIgnoreCase))
            {
                return InvalidResponse(CheckInStatus.Invalid, QrTicketMessages.TicketCancelled);
            }

            if (string.Equals(ticket.Status, TicketStatus.Expired, StringComparison.OrdinalIgnoreCase) ||
                DateTime.UtcNow > ticket.ExpiredAt)
            {
                return InvalidResponse(CheckInStatus.Expired, QrTicketMessages.TicketExpired);
            }

            if (!IsWithinCheckInWindow(ticket.Booking.Showtime))
            {
                return InvalidResponse(CheckInStatus.Invalid, QrTicketMessages.CheckInWindowNotOpen);
            }

            return BuildSuccessResponse(ticket, CheckInStatus.Success, QrTicketMessages.QrValidatedSuccessfully);
        }

        private static VerifyQrResponseDto BuildSuccessResponse(Ticket ticket, string checkInStatus, string message)
        {
            var showtime = ticket.Booking.Showtime;
            return new VerifyQrResponseDto
            {
                IsValid = checkInStatus == CheckInStatus.Success,
                CheckInStatus = checkInStatus,
                Message = message,
                TicketId = ticket.Id,
                BookingRef = ticket.Booking.BookingRef,
                MovieTitle = showtime.Movie.Title,
                CinemaName = showtime.Cinema.Name,
                RoomName = showtime.Room.Name,
                SeatLabel = ticket.BookingSeat.Seat.SeatLabel,
                ShowtimeStart = CinemaTime.ToLocal(showtime.StartTime),
                ShowtimeEnd = CinemaTime.ToLocal(showtime.EndTime),
                ScannedAt = ticket.ScannedAt
            };
        }

        private static VerifyQrResponseDto InvalidResponse(string checkInStatus, string message) =>
            new()
            {
                IsValid = false,
                CheckInStatus = checkInStatus,
                Message = message
            };

        private static bool HasPaidPayment(Booking booking) =>
            booking.Payments.Any(payment =>
                string.Equals(payment.Status, PaymentPaid, StringComparison.OrdinalIgnoreCase));

        private static bool IsWithinCheckInWindow(Showtime showtime)
        {
            var now = DateTime.UtcNow;
            // Showtime.StartTime is persisted as cinema-local time (UTC+7) with DateTimeKind.Unspecified.
            // Convert to UTC before arithmetic against DateTime.UtcNow.
            var startUtc = CinemaTime.ToUtc(showtime.StartTime);
            var earliest = startUtc.AddMinutes(-CheckInDefaults.EarlyCheckInMinutes);
            var latest = startUtc.AddMinutes(CheckInDefaults.LateCheckInMinutes);
            return now >= earliest && now <= latest;
        }

        private static GenerateQrResponseDto ToGenerateQrResponse(Ticket ticket, string format)
        {
            var qrBase64 = GenerateQrBase64(ticket.QrCode, format);
            return new GenerateQrResponseDto
            {
                TicketId = ticket.Id,
                BookingId = ticket.BookingId,
                SeatLabel = ticket.BookingSeat.Seat.SeatLabel,
                Token = ticket.QrCode,
                QrImageBase64 = qrBase64,
                ExpiredAt = CinemaTime.ToLocal(ticket.ExpiredAt),
                Status = ticket.Status
            };
        }

        private static CheckInHistoryItemDto ToCheckInHistoryItem(Ticket ticket)
        {
            var showtime = ticket.Booking.Showtime;
            return new CheckInHistoryItemDto
            {
                TicketId = ticket.Id,
                BookingRef = ticket.Booking.BookingRef,
                MovieTitle = showtime.Movie.Title,
                CinemaName = showtime.Cinema.Name,
                RoomName = showtime.Room.Name,
                SeatLabel = ticket.BookingSeat.Seat.SeatLabel,
                ScannedAt = ticket.ScannedAt ?? DateTime.UtcNow,
                ScannedByName = ticket.ScannedByNavigation?.FullName ?? string.Empty
            };
        }

        private static string GenerateQrBase64(string token, string format)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(token, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(data);
            var pngBytes = qrCode.GetGraphic(20);
            var base64 = Convert.ToBase64String(pngBytes);

            return string.Equals(format, "PNG", StringComparison.OrdinalIgnoreCase)
                ? base64
                : $"data:image/png;base64,{base64}";
        }

        public string RenderQrImageBase64(string token, string format) =>
            GenerateQrBase64(token, format);

        private static string NormalizeToken(string? token) =>
            string.IsNullOrWhiteSpace(token) ? string.Empty : token.Trim();
    }
