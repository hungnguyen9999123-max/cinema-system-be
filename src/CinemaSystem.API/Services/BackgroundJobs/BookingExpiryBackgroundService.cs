using CinemaSystem.DAL.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CinemaSystem.API.Services.BackgroundJobs;

public class BookingExpiryBackgroundService : BackgroundService
{
    private const int BatchSize = 100;
    private const string Pending = "PENDING";
    private const string Expired = "EXPIRED";
    private const string PaymentCancelled = "FAILED";
    private const string Released = "RELEASED";

    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingExpiryBackgroundService> _logger;

    public BookingExpiryBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<BookingExpiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var processed = await ProcessBatchAsync(stoppingToken);
                if (processed > 0)
                {
                    _logger.LogInformation("Expired {Count} pending bookings.", processed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred executing BookingExpiryBackgroundService.");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task<int> ProcessBatchAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        var bookingRepository = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

        var ids = await bookingRepository.Query()
            .Where(b => b.Status == Pending && b.ExpiresAt < DateTime.UtcNow)
            .OrderBy(b => b.ExpiresAt)
            .Take(BatchSize)
            .Select(b => b.Id)
            .ToListAsync(stoppingToken);

        if (ids.Count == 0)
        {
            return 0;
        }

        await unitOfWork.BeginTransactionAsync(stoppingToken);
        try
        {
            var bookings = await bookingRepository.Query()
                .Include(b => b.BookingSeatBookings)
            .Include(b => b.FnbOrders)
            .Include(b => b.Payments)
                .Where(b => ids.Contains(b.Id))
                .ToListAsync(stoppingToken);

            var updated = 0;
            foreach (var booking in bookings)
            {
                if (booking.Status != Pending || booking.ExpiresAt >= DateTime.UtcNow)
                {
                    continue;
                }

                try
                {
                    booking.Status = Expired;
                    foreach (var bookingSeat in booking.BookingSeatBookings)
                    {
                        bookingSeat.SeatStatus = Released;
                    }

                    foreach (var payment in booking.Payments)
                    {
                        if (string.Equals(payment.Status, Pending, StringComparison.OrdinalIgnoreCase))
                        {
                            payment.Status = PaymentCancelled;
                        }
                    }

                    foreach (var fnbOrder in booking.FnbOrders.Where(order => order.OrderStatus == Pending))
                    {
                        fnbOrder.OrderStatus = "CANCELLED";
                    }

                    bookingRepository.Update(booking);
                    updated++;
                }
                catch (DbUpdateConcurrencyException ex)
                {
                    _logger.LogWarning(ex, "Booking {BookingId} was modified by another process, skipping", booking.Id);
                    await unitOfWork.RollbackTransactionAsync(stoppingToken);
                    continue;
                }
            }

            await unitOfWork.CommitTransactionAsync(stoppingToken);
            return updated;
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(stoppingToken);
            throw;
        }
    }
}
