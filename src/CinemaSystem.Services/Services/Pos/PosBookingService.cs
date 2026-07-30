using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Bookings;
using CinemaSystem.Common.DTOs.Fnb;
using CinemaSystem.Common.DTOs.Pos;
using CinemaSystem.Common.Enums;
using CinemaSystem.Common.Helpers;
using CinemaSystem.DAL.Models;
using CinemaSystem.Services.Services.Bookings;
using CinemaSystem.Services.Services.Fnb;
using CinemaSystem.Services.Services.Payments;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.Services.Services.Pos;

/// <summary>
/// Thin wrapper that bridges the old POS contract (CreatePosBookingRequest +
/// PosCreateTicketResponse) onto the unified <see cref="IBookingService"/>
/// entry point. All validation, seat locking, pricing and booking creation
/// lives in <see cref="BookingService"/>; this service only adapts the
/// request shape, picks the correct pay-flow branch (CASH vs VNPay) and
/// shapes the response payload for the counter UI.
/// </summary>
public class PosBookingService : IPosBookingService
{
    private const string GatewayVnPay = "VNPAY";
    private const string GatewayCash = "CASH";

    private readonly IBookingService _bookingService;
    private readonly IPaymentService _paymentService;
    private readonly IFnbOrderService _fnbOrderService;
    private readonly ILogger<PosBookingService> _logger;

    public PosBookingService(
        IBookingService bookingService,
        IPaymentService paymentService,
        IFnbOrderService fnbOrderService,
        ILogger<PosBookingService> logger)
    {
        _bookingService = bookingService;
        _paymentService = paymentService;
        _fnbOrderService = fnbOrderService;
        _logger = logger;
    }

    public async Task<PosCreateTicketResponse> CreatePosTicketAsync(
        Guid staffId,
        CreatePosBookingRequest request,
        CancellationToken cancellationToken = default)
    {
        var gateway = NormalizeGateway(request.Gateway);

        var bookingRequest = new CreateBookingRequestDto
        {
            ShowtimeId = request.ShowtimeId,
            SeatIds = request.SeatIds ?? [],
            AudienceTypeId = request.AudienceTypeId,
            PromotionCode = request.PromotionCode,
            // Walk-in customer id, when supplied. Null = staff sells on the
            // counter without a customer record (stored under the staff id).
            CustomerId = request.CustomerInfo?.CustomerId,
            Gateway = gateway,
            PosCustomer = request.CustomerInfo is null
                ? null
                : new PosCustomerInfoDto
                {
                    FullName = request.CustomerInfo.FullName,
                    Phone = request.CustomerInfo.Phone,
                    Email = request.CustomerInfo.Email
                }
        };

        var bookingResponse = await _bookingService.CreateBookingAsync(
            staffId,
            nameof(UserRole.Staff),
            bookingRequest,
            cancellationToken);

        // F&B remains a separate order flow. Create it after the booking so
        // it can be attached to the generated booking ID exactly once.
        FnbOrderResponse? fnbOrder = null;
        if (request.FnbItems is { Count: > 0 })
        {
            var fnbRequest = new CreateFnbOrderForCounterRequest
            {
                BookingId = bookingResponse.BookingId,
                Items = request.FnbItems
                    .Select(item => new CreateFnbOrderItemRequest
                    {
                        ItemId = item.ItemId,
                        Quantity = item.Quantity
                    })
                    .ToList(),
                PaymentMethod = gateway
            };

            fnbOrder = await _fnbOrderService.CreateForCounterAsync(
                fnbRequest,
                staffId,
                cancellationToken);

            _logger.LogInformation(
                "Attached F&B order {FnbOrderId} ({FnbTotal}) to POS booking {BookingRef}.",
                fnbOrder.Id,
                fnbOrder.TotalAmount,
                bookingResponse.BookingRef);
        }

        var fnbOrders = fnbOrder is null
            ? null
            : (IReadOnlyList<FnbOrderSummaryDto>)[ToFnbOrderSummary(fnbOrder)];
        var fnbTotal = fnbOrder?.TotalAmount ?? 0m;

        var totalAmount = bookingResponse.TotalAmount + fnbTotal;
        var grandTotal = bookingResponse.FinalAmount + fnbTotal;

        if (string.Equals(bookingResponse.PaymentGateway, GatewayCash, StringComparison.OrdinalIgnoreCase))
        {
            // CASH bookings are 2-step: PENDING booking + PENDING payment,
            // no QR generated. Staff must hit POST /api/pos/tickets/{paymentId}/confirm
            // to flip them to CONFIRMED/PAID/BOOKED and receive the QR list.
            return new PosCreateTicketResponse
            {
                IsVnpay = false,
                Cash = new PosBookingResponse
                {
                    BookingId = bookingResponse.BookingId,
                    BookingRef = bookingResponse.BookingRef,
                    PaymentGateway = GatewayCash,
                    TotalAmount = totalAmount,
                    DiscountAmount = bookingResponse.DiscountAmount,
                    FinalAmount = grandTotal,
                    IsPendingConfirmation = true,
                    ExpiresAt = bookingResponse.ExpiresAt,
                    FnbOrders = fnbOrders,
                    FnbTotalAmount = fnbTotal
                }
            };
        }

        var paymentUrl = await BuildPaymentUrlForBookingAsync(bookingResponse, grandTotal, cancellationToken);
        var vnpayResponse = BuildVnpayResponse(bookingResponse, totalAmount, grandTotal, paymentUrl, fnbOrders, fnbTotal);
        return new PosCreateTicketResponse
        {
            IsVnpay = true,
            Vnpay = vnpayResponse
        };
    }

    private async Task<string> BuildPaymentUrlForBookingAsync(
        CreateBookingResponseDto bookingResponse,
        decimal grandTotal,
        CancellationToken cancellationToken)
    {
        var payment = new Payment
        {
            Id = bookingResponse.PaymentId ?? Guid.Empty,
            BookingId = bookingResponse.BookingId,
            Gateway = bookingResponse.PaymentGateway ?? GatewayVnPay,
            Amount = grandTotal,
            Status = bookingResponse.PaymentStatus ?? "PENDING",
            CreatedAt = bookingResponse.ExpiresAt.AddMinutes(-10)
        };

        var booking = new Booking
        {
            Id = bookingResponse.BookingId,
            BookingRef = bookingResponse.BookingRef,
            FinalAmount = grandTotal,
            ExpiresAt = bookingResponse.ExpiresAt
        };

        try
        {
            return _paymentService.BuildVnPayPaymentUrl(payment, booking, isPosStaff: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to build VNPay payment URL for booking {BookingId}.", bookingResponse.BookingId);
            throw;
        }
    }

    private static PosVnpayResponse BuildVnpayResponse(
        CreateBookingResponseDto booking,
        decimal totalAmount,
        decimal grandTotal,
        string paymentUrl,
        IReadOnlyList<FnbOrderSummaryDto>? fnbOrders,
        decimal fnbTotal)
    {
        return new PosVnpayResponse
        {
            BookingId = booking.BookingId,
            BookingRef = booking.BookingRef,
            MovieTitle = string.Empty,
            CinemaName = string.Empty,
            RoomName = string.Empty,
            ShowtimeStart = default,
            ShowtimeEnd = default,
            SeatLabels = [],
            PaymentGateway = GatewayVnPay,
            PaymentId = booking.PaymentId ?? Guid.Empty,
            PaymentStatus = booking.PaymentStatus ?? "PENDING",
            TotalAmount = totalAmount,
            DiscountAmount = booking.DiscountAmount,
            FinalAmount = grandTotal,
            PaymentUrl = paymentUrl,
            ExpiresAt = CinemaTime.ToLocal(booking.ExpiresAt),
            FnbOrders = fnbOrders,
            FnbTotalAmount = fnbTotal
        };
    }

    private static FnbOrderSummaryDto ToFnbOrderSummary(FnbOrderResponse order) => new()
    {
        OrderId = order.Id,
        TotalAmount = order.TotalAmount,
        OrderStatus = order.OrderStatus,
        CreatedAt = order.CreatedAt,
        Items = order.Items
            .Select(item => new FnbOrderItemSummaryDto
            {
                ItemId = item.ItemId,
                ItemName = item.ItemName,
                Quantity = item.Quantity,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Subtotal
            })
            .ToList()
    };

    private static string NormalizeGateway(string? gateway)
    {
        if (string.IsNullOrWhiteSpace(gateway))
        {
            return GatewayCash;
        }

        return gateway.Trim().ToUpperInvariant() switch
        {
            GatewayVnPay => GatewayVnPay,
            GatewayCash => GatewayCash,
            _ => throw new InvalidOperationException(PosMessages.UnsupportedGateway)
        };
    }
}
