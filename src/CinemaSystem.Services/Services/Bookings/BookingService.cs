using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Bookings;
using CinemaSystem.Common.DTOs.Promotions;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.Common.Helpers;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using CinemaSystem.Services.Services.Promotions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.Services.Services.Bookings;

public class BookingService : IBookingService
{
    private const string BookingPending = "PENDING";
    private const string BookingConfirmed = "CONFIRMED";
    private const string BookingCancelled = "CANCELLED";
    private const string BookingExpired = "EXPIRED";
    private const string SeatHeld = "HELD";
    private const string SeatBooked = "BOOKED";
    private const string SeatReleased = "RELEASED";
    private const string GatewayCash = "CASH";
    private const string GatewayVnPay = "VNPAY";
    private const string PaymentPaid = "SUCCESS";
    private const string PaymentPending = "PENDING";

    private static readonly TimeSpan PendingBookingTtl = TimeSpan.FromMinutes(10);

    private readonly IBookingRepository _bookingRepository;
    private readonly IShowtimeRepository _showtimeRepository;
    private readonly ISeatRepository _seatRepository;
    private readonly IPricingRuleRepository _pricingRuleRepository;
    private readonly IAudienceTypeRepository _audienceTypeRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPromotionService _promotionService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BookingService> _logger;
    public BookingService(
        IBookingRepository bookingRepository,
        IShowtimeRepository showtimeRepository,
        ISeatRepository seatRepository,
        IPricingRuleRepository pricingRuleRepository,
        IAudienceTypeRepository audienceTypeRepository,
        IPaymentRepository paymentRepository,
        IPromotionService promotionService,
        IUnitOfWork unitOfWork,
        ILogger<BookingService> logger)
    {
        _bookingRepository = bookingRepository;
        _showtimeRepository = showtimeRepository;
        _seatRepository = seatRepository;
        _pricingRuleRepository = pricingRuleRepository;
        _audienceTypeRepository = audienceTypeRepository;
        _paymentRepository = paymentRepository;
        _promotionService = promotionService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<CreateBookingResponseDto> CreateBookingAsync(
        Guid callerUserId,
        string callerRole,
        CreateBookingRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var isStaff = IsStaffOrAbove(callerRole);
        var gateway = NormalizeGateway(request.Gateway, isStaff);

        // Online customer always books for themselves. POS staff can supply a
        // walk-in customer id; otherwise fall back to the staff id so we still
        // have a non-empty FK to USERS.
        var customerId = isStaff
            ? request.CustomerId ?? callerUserId
            : callerUserId;

        if (customerId == Guid.Empty)
        {
            throw new InvalidOperationException("Customer id is required.");
        }

        if (request.SeatIds is null || request.SeatIds.Count == 0)
        {
            throw new InvalidOperationException("At least one seat must be selected.");
        }

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var now = DateTime.UtcNow;

            var showtime = await _showtimeRepository.GetByIdAsync(request.ShowtimeId, cancellationToken);
            if (showtime is null)
            {
                throw new KeyNotFoundException("Showtime not found.");
            }

            if (showtime.StartTime <= now || showtime.EndTime <= now)
            {
                throw new BusinessConflictException("Showtime has already started or ended.");
            }

            var audienceType = await _audienceTypeRepository.GetByIdAsync(request.AudienceTypeId, cancellationToken);
            if (audienceType is null || !audienceType.IsActive)
            {
                throw new InvalidOperationException("Invalid audience type.");
            }

            var seatIds = request.SeatIds.Distinct().ToList();
            var seats = await _seatRepository.GetByIdsAsync(seatIds, cancellationToken);
            if (seats.Count != seatIds.Count)
            {
                throw new KeyNotFoundException("One or more seats were not found.");
            }

            foreach (var seat in seats)
            {
                if (seat.RoomId != showtime.RoomId)
                {
                    throw new BusinessConflictException(
                        $"Seat {seat.SeatLabel} does not belong to the showtime's room.");
                }
            }

            var unavailableSeatIds = await _bookingRepository.Query()
                .Where(b => b.ShowtimeId == request.ShowtimeId
                            && b.Status != BookingCancelled
                            && b.Status != BookingExpired)
                .SelectMany(b => b.BookingSeatBookings)
                .Where(bs => seatIds.Contains(bs.SeatId)
                            && (bs.SeatStatus == SeatHeld
                                || bs.SeatStatus == SeatBooked
                                || bs.SeatStatus == BookingConfirmed
                                || bs.SeatStatus == "REFUNDED"))
                .Select(bs => bs.SeatId)
                .ToListAsync(cancellationToken);

            if (unavailableSeatIds.Count > 0)
            {
                var unavailableSeatId = unavailableSeatIds.First();
                var unavailableSeat = seats.FirstOrDefault(s => s.Id == unavailableSeatId);
                throw new BusinessConflictException(
                    $"Seat {unavailableSeat?.SeatLabel ?? unavailableSeatId.ToString()} is unavailable.");
            }

            var pricingRule = await _pricingRuleRepository.GetActivePricingRuleAsync(
                showtime.CinemaId,
                showtime.Room.RoomType ?? "Standard",
                showtime.TimeSlot,
                showtime.StartTime.Date,
                cancellationToken);
            if (pricingRule is null)
            {
                throw new InvalidOperationException("Pricing rule not found for this showtime.");
            }

            // Booking always starts as PENDING — even POS Cash will be flipped
            // to CONFIRMED by IPosBookingConfirmationService.ConfirmCashPaymentAsync
            // once staff hits POST /api/pos/tickets/{paymentId}/confirm after
            // collecting cash at the counter. VNPay bookings remain PENDING
            // until the IPN handler confirms them. This keeps a single happy
            // path for both flows.
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                ShowtimeId = showtime.Id,
                BookingRef = BookingRefGenerator.Generate(now),
                Status = BookingPending,
                BookedAt = now,
                ExpiresAt = now.Add(PendingBookingTtl),
                DiscountAmount = 0
            };

            var bookingSeats = new List<BookingSeat>();
            decimal totalAmount = 0;

            foreach (var seat in seats)
            {
                if (seat.SeatType is null)
                {
                    throw new InvalidOperationException($"Seat type not found for seat {seat.SeatLabel}.");
                }

                decimal seatMult = seat.SeatType.SeatMultiplier;
                decimal basePrice = pricingRule.BasePrice;
                decimal unitPrice = basePrice
                    * pricingRule.TimeMultiplier
                    * seatMult
                    * audienceType.AudienceMultiplier;

                bookingSeats.Add(new BookingSeat
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    SeatId = seat.Id,
                    ShowtimeId = showtime.Id,
                    PricingRuleId = pricingRule.Id,
                    AudienceTypeId = audienceType.Id,
                    BasePriceSnap = basePrice,
                    TimeMultSnap = pricingRule.TimeMultiplier,
                    SeatMultSnap = seatMult,
                    AudienceMultSnap = audienceType.AudienceMultiplier,
                    UnitPrice = unitPrice,
                    SeatStatus = SeatHeld,
                    CreatedAt = now
                });
                totalAmount += unitPrice;
            }

            booking.TotalAmount = totalAmount;
            booking.FinalAmount = totalAmount;

            if (!string.IsNullOrWhiteSpace(request.PromotionCode))
            {
                // F&B orders are created separately and therefore do not
                // participate in the booking promotion or ticket payment.
                var promotionValidation = await _promotionService.ValidateAsync(
                    customerId,
                    new ValidatePromotionRequest
                    {
                        PromoCode = request.PromotionCode,
                        BookingAmount = booking.TotalAmount
                    },
                    cancellationToken);

                if (!promotionValidation.IsValid || !promotionValidation.PromotionId.HasValue)
                {
                    throw new BusinessConflictException(promotionValidation.Message);
                }

                booking.PromotionId = promotionValidation.PromotionId.Value;
                booking.DiscountAmount = promotionValidation.DiscountAmount;
                booking.FinalAmount = promotionValidation.FinalAmount;
            }

            // Only POS (cash + VNPay at the counter) needs a Payment row
            // attached at booking time so the counter can flip the payment to
            // PAID and generate tickets synchronously. Online customer flow
            // still goes through PaymentService.CreatePaymentAsync after the
            // booking step, which materialises the Payment row there.
            Payment? payment = null;
            if (isStaff)
            {
                payment = new Payment
                {
                    Id = Guid.NewGuid(),
                    BookingId = booking.Id,
                    Gateway = gateway,
                    Amount = booking.FinalAmount,
                    Status = PaymentPending,
                    CreatedAt = now
                };
            }

            await _bookingRepository.AddAsync(booking, cancellationToken);
            await _bookingRepository.AddBookingSeatsAsync(bookingSeats, cancellationToken);
            if (payment is not null)
            {
                await _paymentRepository.AddAsync(payment, cancellationToken);
            }
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation(
                "Booking {BookingRef} created via {Source} by user {UserId}; gateway={Gateway}",
                booking.BookingRef, isStaff ? "POS" : "ONLINE", callerUserId, gateway);

            // Build F&B orders response
            var fnbOrdersResponse = booking.FnbOrders.Select(o => new FnbOrderSummaryDto
            {
                OrderId = o.Id,
                TotalAmount = o.TotalAmount,
                OrderStatus = o.OrderStatus,
                CreatedAt = o.CreatedAt,
                Items = o.FnbOrderDetails?.Select(d => new FnbOrderItemSummaryDto
                {
                    ItemId = d.ItemId,
                    ItemName = d.Item?.Name ?? string.Empty,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal
                }).ToList() ?? new()
            }).ToList();

            return new CreateBookingResponseDto
            {
                BookingId = booking.Id,
                BookingRef = booking.BookingRef,
                BookingStatus = booking.Status,
                ExpiresAt = booking.ExpiresAt,
                TotalAmount = booking.TotalAmount,
                DiscountAmount = booking.DiscountAmount,
                FinalAmount = booking.FinalAmount,
                PaymentId = payment?.Id,
                PaymentGateway = payment?.Gateway,
                PaymentStatus = payment?.Status,
                FnbOrders = fnbOrdersResponse
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<BookingResponseDto> GetBookingByIdAsync(
        Guid bookingId,
        Guid callerUserId,
        string callerRole,
        CancellationToken cancellationToken = default)
    {
        var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);
        if (booking == null)
        {
            throw new KeyNotFoundException("Booking not found.");
        }

        EnsureCanAccessBooking(booking, callerUserId, callerRole);

        return new BookingResponseDto
        {
            BookingId = booking.Id,
            BookingRef = booking.BookingRef,
            ExpiresAt = booking.ExpiresAt,
            TotalAmount = booking.TotalAmount,
            PromotionId = booking.PromotionId,
            DiscountAmount = booking.DiscountAmount,
            FinalAmount = booking.FinalAmount,
            Status = booking.Status,
            FnbOrders = booking.FnbOrders
                .Where(order => !string.Equals(order.OrderStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                .OrderBy(order => order.CreatedAt)
                .Select(ToFnbOrderSummary)
                .ToList()
        };
    }

    public async Task<MyBookingsPagedResultDto> GetMyBookingsAsync(
        Guid customerId,
        MyBookingsQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        if (customerId == Guid.Empty)
        {
            throw new UnauthorizedAccessException("Unauthorized access.");
        }

        var (bookings, totalCount) = await _bookingRepository.GetPagedByCustomerAsync(
            customerId,
            request.Status,
            request.FromDate,
            request.ToDate,
            request.Page,
            request.PageSize,
            cancellationToken);

        var items = bookings.Select(booking => new MyBookingListItemDto
        {
            BookingId = booking.Id,
            BookingRef = booking.BookingRef,
            ShowtimeId = booking.ShowtimeId,
            MovieTitle = booking.Showtime?.Movie?.Title ?? string.Empty,
            PosterUrl = booking.Showtime?.Movie?.PosterUrl,
            CinemaName = booking.Showtime?.Cinema?.Name ?? string.Empty,
            RoomName = booking.Showtime?.Room?.Name ?? string.Empty,
            ShowtimeStart = booking.Showtime?.StartTime ?? default,
            ShowtimeEnd = booking.Showtime?.EndTime ?? default,
            SeatLabels = booking.BookingSeatBookingNavigations?
                .Select(bs => bs.Seat?.SeatLabel ?? string.Empty)
                .Where(label => !string.IsNullOrEmpty(label))
                .OrderBy(label => label, StringComparer.Ordinal)
                .ToList() ?? [],
            TicketCount = booking.BookingSeatBookingNavigations?.Count ?? 0,
            TotalAmount = booking.TotalAmount,
            DiscountAmount = booking.DiscountAmount,
            FinalAmount = booking.FinalAmount,
            PromotionId = booking.PromotionId,
            BookedAt = booking.BookedAt,
            CancelledAt = booking.CancelledAt,
            Status = booking.Status,
            FnbOrders = (booking.FnbOrders ?? [])
                .Where(o => string.Equals(o.OrderStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase) is false)
                .OrderBy(o => o.CreatedAt)
                .Select(o => new FnbOrderSummaryDto
                {
                    OrderId = o.Id,
                    TotalAmount = o.TotalAmount,
                    OrderStatus = o.OrderStatus,
                    CreatedAt = o.CreatedAt,
                    Items = o.FnbOrderDetails?
                        .Where(d => d.Quantity > 0)
                        .OrderBy(d => d.Item?.Name ?? string.Empty)
                        .Select(d => new FnbOrderItemSummaryDto
                        {
                            ItemId = d.ItemId,
                            ItemName = d.Item?.Name ?? string.Empty,
                            Quantity = d.Quantity,
                            UnitPrice = d.UnitPrice,
                            Subtotal = d.Subtotal
                        }).ToList() ?? []
                }).ToList()
        }).ToList();

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => request.PageSize
        };
        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)pageSize);

        return new MyBookingsPagedResultDto
        {
            Items = items,
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }

    public async Task<IEnumerable<SeatMapItemDto>> GetSeatMapAsync(
        Guid showtimeId,
        CancellationToken cancellationToken = default)
    {
        var showtime = await _showtimeRepository.GetByIdAsync(showtimeId, cancellationToken);
        if (showtime == null)
        {
            throw new KeyNotFoundException("Showtime not found.");
        }

            var allSeats = await _seatRepository.GetByRoomIdAsync(showtime.RoomId, cancellationToken);

        var activeBookings = await _bookingRepository.Query()
            .Include(b => b.BookingSeatBookings)
            .Where(b => b.ShowtimeId == showtimeId && b.Status != BookingCancelled)
            .ToListAsync(cancellationToken);

        var bookedSeats = activeBookings
            .SelectMany(b => b.BookingSeatBookings)
            .Where(bs => bs.SeatStatus != SeatReleased)
            .ToList();

        var now = DateTime.UtcNow;

        var seatMap = allSeats.Select(seat =>
        {
            var status = "AVAILABLE";
            var seatBooking = bookedSeats.FirstOrDefault(b => b.SeatId == seat.Id);

            if (seatBooking != null)
            {
                var parentBooking = activeBookings.FirstOrDefault(b => b.Id == seatBooking.BookingId);
                var bookingStatus = parentBooking?.Status;
                var bookingExpiresAt = parentBooking?.ExpiresAt ?? DateTime.MinValue;

                if (bookingStatus == "REFUNDED")
                {
                    // A refunded ticket cannot be sold again. This is deliberately
                    // a distinct UI state rather than AVAILABLE, even though its
                    // ticket QR has been cancelled.
                    status = "UNAVAILABLE";
                }
                else if (bookingStatus == "PENDING" && bookingExpiresAt > now)
                {
                    status = "HELD";
                }
                // A refund is asynchronous. While it is waiting for a definitive
                // gateway result, the original ticket remains valid and its seat
                // must stay unavailable. Relying only on the parent booking's
                // CONFIRMED status incorrectly exposed seats from
                // REFUND_PROCESSING / reconciliation bookings as AVAILABLE.
                else if (seatBooking.SeatStatus is "BOOKED" or "CONFIRMED")
                {
                    status = "BOOKED";
                }
            }

            return new SeatMapItemDto
            {
                SeatId = seat.Id,
                SeatLabel = seat.SeatLabel,
                SeatType = seat.SeatType.Name,
                Status = status
            };
        });

        return seatMap;
    }

    public async Task<BookingResponseDto> CancelBookingAsync(
        Guid bookingId,
        Guid callerUserId,
        string callerRole,
        CancellationToken cancellationToken = default)
    {
        await _unitOfWork.BeginTransactionAsync(cancellationToken);
        try
        {
            var booking = await _bookingRepository.GetByIdAsync(bookingId, cancellationToken);
            if (booking == null)
            {
                throw new KeyNotFoundException("Booking not found.");
            }

            EnsureCanAccessBooking(booking, callerUserId, callerRole);

            if (booking.Status != "PENDING")
            {
                throw new BusinessConflictException(
                    "Paid bookings must use the refund request flow instead of cancellation.");
            }

            booking.Status = BookingCancelled;
            booking.CancelledAt = DateTime.UtcNow;

            foreach (var bs in booking.BookingSeatBookings)
            {
                bs.SeatStatus = SeatReleased;
            }
            CancelPendingFnbOrders(booking);

            _bookingRepository.Update(booking);
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return new BookingResponseDto
            {
                BookingId = booking.Id,
                BookingRef = booking.BookingRef,
                ExpiresAt = booking.ExpiresAt,
                TotalAmount = booking.TotalAmount,
                PromotionId = booking.PromotionId,
                DiscountAmount = booking.DiscountAmount,
                FinalAmount = booking.FinalAmount,
                Status = booking.Status,
                FnbOrders = booking.FnbOrders
                    .Where(order => !string.Equals(order.OrderStatus, "CANCELLED", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(order => order.CreatedAt)
                    .Select(ToFnbOrderSummary)
                    .ToList()
            };
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    private static void EnsureCanAccessBooking(Booking booking, Guid callerUserId, string callerRole)
    {
        if (IsStaffOrAbove(callerRole))
        {
            return;
        }

        if (booking.CustomerId != callerUserId)
        {
            throw new ForbiddenAccessException("You do not have access to this booking.");
        }
    }

    private static bool IsStaffOrAbove(string role)
    {
        return string.Equals(role, nameof(UserRole.Staff), StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, nameof(UserRole.Manager), StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, nameof(UserRole.Admin), StringComparison.OrdinalIgnoreCase);
    }

    private static void CancelPendingFnbOrders(Booking booking)
    {
        foreach (var order in booking.FnbOrders.Where(order => string.Equals(order.OrderStatus, "PENDING", StringComparison.OrdinalIgnoreCase)))
        {
            order.OrderStatus = "CANCELLED";
        }
    }

    private static FnbOrderSummaryDto ToFnbOrderSummary(FnbOrder order) => new()
    {
        OrderId = order.Id,
        TotalAmount = order.TotalAmount,
        OrderStatus = order.OrderStatus,
        CreatedAt = order.CreatedAt,
        Items = order.FnbOrderDetails
            .Where(detail => detail.Quantity > 0)
            .OrderBy(detail => detail.Item?.Name ?? string.Empty)
            .Select(detail => new FnbOrderItemSummaryDto
            {
                ItemId = detail.ItemId,
                ItemName = detail.Item?.Name ?? string.Empty,
                Quantity = detail.Quantity,
                UnitPrice = detail.UnitPrice,
                Subtotal = detail.Subtotal
            }).ToList()
    };

    private static string NormalizeGateway(string? gateway, bool isStaff)
    {
        if (string.IsNullOrWhiteSpace(gateway))
        {
            // Online customer path has no gateway selection at the booking
            // step (PaymentService will materialise the VNPay URL afterwards),
            // so default to VNPAY for that flow.
            return GatewayVnPay;
        }

        var normalized = gateway.Trim().ToUpperInvariant();
        return normalized switch
        {
            GatewayVnPay => GatewayVnPay,
            GatewayCash => isStaff
                ? GatewayCash
                : throw new InvalidOperationException("Cash payment is only available at the counter."),
            _ => throw new InvalidOperationException($"Unsupported gateway '{gateway}'.")
        };
    }
}
