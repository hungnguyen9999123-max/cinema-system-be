using CinemaSystem.Common;
using CinemaSystem.Common.Constants;
using CinemaSystem.Common.DTOs.Fnb;
using CinemaSystem.Common.Exceptions;
using CinemaSystem.DAL.Interfaces;
using CinemaSystem.DAL.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaSystem.Services.Services.Fnb;

public sealed class FnbOrderService(
    IFnbOrderRepository fnbOrderRepository,
    IFnbOrderDetailRepository fnbOrderDetailRepository,
    IFnbItemRepository fnbItemRepository,
    IBookingRepository bookingRepository,
    IPaymentRepository paymentRepository,
    IUnitOfWork unitOfWork,
    CinemaDbContext dbContext) : IFnbOrderService
{
    private static readonly string[] ValidStatuses = ["PENDING", "PAID", "CONFIRMED", "CANCELLED"];
    private const string ActiveStatus = "ACTIVE";

    public async Task<PagedResult<FnbOrderResponse>> SearchAsync(
        FnbOrderSearchRequest request,
        CancellationToken cancellationToken = default)
    {
        var query = fnbOrderRepository.Query()
            .AsNoTracking()
            .Include(o => o.FnbOrderDetails)
            .ThenInclude(d => d.Item)
            .AsQueryable();

        if (request.BookingId.HasValue)
        {
            query = query.Where(o => o.BookingId == request.BookingId.Value);
        }

        if (request.CustomerId.HasValue)
        {
            query = query.Where(o => o.CustomerId == request.CustomerId.Value);
        }

        if (request.StaffId.HasValue)
        {
            query = query.Where(o => o.StaffId == request.StaffId.Value);
        }

        if (request.IsCounterOrder.HasValue)
        {
            if (request.IsCounterOrder.Value)
            {
                query = query.Where(o => o.StaffId != null);
            }
            else
            {
                query = query.Where(o => o.StaffId == null);
            }
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = NormalizeStatus(request.Status);
            if (!IsValidStatus(status))
            {
                throw new InvalidOperationException(FnbOrderMessages.InvalidStatus);
            }
            query = query.Where(o => o.OrderStatus == status);
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var orders = await query
            .OrderByDescending(o => o.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(cancellationToken);

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)request.PageSize);
        return new PagedResult<FnbOrderResponse>(
            orders.Select(ToResponse).ToList(),
            request.Page,
            request.PageSize,
            totalCount,
            totalPages);
    }

    public async Task<FnbOrderResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var order = await fnbOrderRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        return order is null ? null : ToResponse(order);
    }

    public async Task<FnbOrderResponse> CreateAsync(
        CreateFnbOrderRequest request,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
            if (booking is null)
            {
                throw new KeyNotFoundException(FnbOrderMessages.BookingNotFound);
            }

            if (booking.CustomerId != customerId)
            {
                throw new UnauthorizedAccessException(FnbOrderMessages.UnauthorizedBooking);
            }

            if (booking.Status != "PENDING")
            {
                throw new BusinessConflictException(FnbOrderMessages.BookingNotEligible);
            }

            var (order, _) = await CreateOrderInternalAsync(
                request.Items,
                customerId,
                bookingId: request.BookingId,
                staffId: null,
                paymentMethod: null,
                cancellationToken);

            booking.TotalAmount += order.TotalAmount;
            booking.FinalAmount = booking.TotalAmount - booking.DiscountAmount;
            bookingRepository.Update(booking);

            await UpdatePaymentAmountAsync(booking.Id, booking.FinalAmount, cancellationToken);

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return ToResponse(order);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FnbOrderResponse> CreateCounterOrderAsync(
        CreateFnbCounterOrderRequest request,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var customerId = request.CustomerId ?? CommonMessages.GuestCustomerId;

            var (order, _) = await CreateOrderInternalAsync(
                request.Items,
                customerId,
                bookingId: null,
                staffId: staffId,
                paymentMethod: request.PaymentMethod,
                cancellationToken);

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return ToResponse(order);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FnbOrderResponse> CreateForCounterAsync(
        CreateFnbOrderForCounterRequest request,
        Guid staffId,
        CancellationToken cancellationToken = default)
    {
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var booking = await bookingRepository.GetByIdAsync(request.BookingId, cancellationToken);
            if (booking is null)
            {
                throw new KeyNotFoundException(FnbOrderMessages.BookingNotFound);
            }

            if (booking.Status != "PENDING" && booking.Status != "CONFIRMED")
            {
                throw new BusinessConflictException(FnbOrderMessages.BookingNotEligible);
            }

            var (order, _) = await CreateOrderInternalAsync(
                request.Items,
                customerId: booking.CustomerId,
                bookingId: request.BookingId,
                staffId: staffId,
                paymentMethod: request.PaymentMethod,
                cancellationToken);

            booking.TotalAmount += order.TotalAmount;
            booking.FinalAmount = booking.TotalAmount - booking.DiscountAmount;
            bookingRepository.Update(booking);

            await UpdatePaymentAmountAsync(booking.Id, booking.FinalAmount, cancellationToken);

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return ToResponse(order);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(cancellationToken);
            throw;
        }
    }

    public async Task<FnbOrderResponse?> UpdateStatusAsync(
        Guid id,
        UpdateFnbOrderStatusRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await fnbOrderRepository.GetByIdWithDetailsAsync(id, cancellationToken);
        if (order is null)
        {
            return null;
        }

        var newStatus = NormalizeStatus(request.Status);
        if (!IsValidStatus(newStatus))
        {
            throw new InvalidOperationException(FnbOrderMessages.InvalidStatus);
        }

        ValidateStatusTransition(order.OrderStatus, newStatus);

        order.OrderStatus = newStatus;
        fnbOrderRepository.Update(order);
        await fnbOrderRepository.SaveChangesAsync(cancellationToken);

        return ToResponse(order);
    }

    private async Task<(FnbOrder Order, Dictionary<Guid, FnbItem> ItemDict)> CreateOrderInternalAsync(
        List<CreateFnbOrderItemRequest> items,
        Guid customerId,
        Guid? bookingId,
        Guid? staffId,
        string? paymentMethod,
        CancellationToken cancellationToken)
    {
        var itemIds = items.Select(i => i.ItemId).Distinct().ToList();
        var fnbItems = await fnbItemRepository.Query()
            .Where(i => itemIds.Contains(i.Id) && i.Status == ActiveStatus)
            .ToListAsync(cancellationToken);

        if (fnbItems.Count != itemIds.Count)
        {
            var notFoundIds = itemIds.Except(fnbItems.Select(i => i.Id));
            throw new KeyNotFoundException(string.Format(FnbOrderMessages.ItemsNotFound, string.Join(", ", notFoundIds)));
        }

        var itemDict = fnbItems.ToDictionary(i => i.Id);
        var orderDetails = new List<FnbOrderDetail>();
        decimal totalAmount = 0;

        foreach (var orderItem in items)
        {
            if (!itemDict.TryGetValue(orderItem.ItemId, out var fnbItem))
            {
                throw new KeyNotFoundException(string.Format(FnbOrderMessages.ItemNotFound, orderItem.ItemId));
            }

            var subtotal = fnbItem.Price * orderItem.Quantity;
            totalAmount += subtotal;

            orderDetails.Add(new FnbOrderDetail
            {
                Id = Guid.NewGuid(),
                ItemId = fnbItem.Id,
                Quantity = orderItem.Quantity,
                UnitPrice = fnbItem.Price,
                Subtotal = subtotal
            });
        }

        var now = DateTime.UtcNow;
        var order = new FnbOrder
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            CustomerId = customerId,
            StaffId = staffId,
            TotalAmount = totalAmount,
            OrderStatus = staffId.HasValue && paymentMethod != "VNPAY" ? "PAID" : "PENDING",
            PaymentMethod = paymentMethod,
            CreatedAt = now
        };

        // Add order details to context first
        foreach (var detail in orderDetails)
        {
            detail.FnbOrderId = order.Id;
            detail.FnbOrder = order;
            dbContext.FnbOrderDetails.Add(detail);
        }

        // Add order - EF Core will see FK relationship is already resolved
        await fnbOrderRepository.AddAsync(order, cancellationToken);

        return (order, itemDict);
    }

    private async Task UpdatePaymentAmountAsync(Guid bookingId, decimal newAmount, CancellationToken cancellationToken)
    {
        var payment = await paymentRepository.GetLatestForBookingAsync(bookingId, "VNPAY", cancellationToken);
        if (payment is not null && payment.Status == "PENDING")
        {
            payment.Amount = newAmount;
            paymentRepository.Update(payment);
        }
    }

    private static void ValidateStatusTransition(string currentStatus, string newStatus)
    {
        var allowedTransitions = new Dictionary<string, string[]>
        {
            ["PENDING"] = ["CONFIRMED", "PAID", "CANCELLED"],
            ["CONFIRMED"] = ["CANCELLED"],
            ["PAID"] = ["CANCELLED"],
            ["CANCELLED"] = []
        };

        if (!allowedTransitions.TryGetValue(currentStatus, out var allowed) || !allowed.Contains(newStatus))
        {
            throw new BusinessConflictException(
                string.Format(FnbOrderMessages.InvalidStatusTransition, currentStatus, newStatus));
        }
    }

    private static FnbOrderResponse ToResponse(FnbOrder order)
        => new(
            order.Id,
            order.BookingId,
            order.CustomerId,
            order.StaffId,
            order.TotalAmount,
            order.OrderStatus,
            order.CreatedAt,
            order.PaymentMethod,
            order.FnbOrderDetails.Select(d => new FnbOrderDetailResponse(
                d.Id,
                d.ItemId,
                d.Item?.Name ?? string.Empty,
                d.Quantity,
                d.UnitPrice,
                d.Subtotal)).ToList());

    private static string NormalizeStatus(string status) => status.Trim().ToUpperInvariant();

    private static bool IsValidStatus(string status) => ValidStatuses.Contains(status);
}
