using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CinemaSystem.Services.Services.Showtimes;

public sealed class ShowtimeCompletionBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<ShowtimeCompletionBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan SyncInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var showtimeService = scope.ServiceProvider.GetRequiredService<IShowtimeService>();
                var changedCount = await showtimeService.SyncShowtimeStatusesAsync(DateTime.Now, stoppingToken);

                if (changedCount > 0)
                {
                    logger.LogInformation(
                        "Showtime status sync finished. Changed: {ChangedCount}.",
                        changedCount);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred executing ShowtimeCompletionBackgroundService.");
            }

            await Task.Delay(SyncInterval, stoppingToken);
        }
    }
}
