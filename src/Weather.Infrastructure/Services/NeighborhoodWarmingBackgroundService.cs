using System.Collections.Concurrent;
using System.Threading.RateLimiting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Weather.Core.Abstractions;
using Weather.Core.Models;
using Weather.Core.Telemetry;
using Weather.Infrastructure.Logging;

namespace Weather.Infrastructure.Services;

internal sealed class NeighborhoodWarmingBackgroundService(
    INeighborhoodWarmer warmer,
    IServiceScopeFactory scopeFactory,
    IForecastCache forecastCache,
    RateLimiter rateLimiter,
    WeatherTelemetry telemetry,
    TimeProvider timeProvider,
    ILogger<NeighborhoodWarmingBackgroundService> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<GridPoint, byte> _inFlight = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.WarmingStarted();

        try
        {
            await foreach (var origin in warmer.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    await WarmNeighboursAsync(origin, stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
                {
                    logger.WarmingFailed(ex, origin);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
        }

        logger.WarmingStopping();
    }

    private async Task WarmNeighboursAsync(GridPoint origin, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        foreach (var cell in GridNeighborhood.Surrounding(origin))
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_inFlight.TryAdd(cell, 0))
            {
                continue;
            }

            try
            {
                await WarmCellAsync(cell, now, cancellationToken);
            }
            finally
            {
                _inFlight.TryRemove(cell, out _);
            }
        }
    }

    private async Task WarmCellAsync(GridPoint cell, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await forecastCache.GetAsync(cell, cancellationToken) is { } cached && cached.IsFresh(now))
        {
            return;
        }

        using var lease = await rateLimiter.AcquireAsync(1, cancellationToken);
        if (!lease.IsAcquired)
        {
            logger.WarmingRateLimited(cell);
            return;
        }

        await using var scope = scopeFactory.CreateAsyncScope();
        var weatherService = scope.ServiceProvider.GetRequiredService<IWeatherService>();
        if (await weatherService.GetForecastByGridAsync(cell, cancellationToken) is not null)
        {
            telemetry.RecordNeighborWarmed(cell.GridId);
        }
    }
}
