using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Weather.Core.Abstractions;
using Weather.Core.Models;
using Weather.Core.Telemetry;
using Weather.Infrastructure.Logging;
using Weather.Infrastructure.Nws;

namespace Weather.Infrastructure.Services;

internal sealed class WeatherService(
    IPointMetadataCache metadataCache,
    IForecastCache forecastCache,
    INwsApiClient nwsClient,
    INeighborhoodWarmer warmer,
    WeatherTelemetry telemetry,
    TimeProvider timeProvider,
    IOptions<NwsClientOptions> options,
    ILogger<WeatherService> logger) : IWeatherService
{
    private const string MetadataCacheName = "metadata";
    private const string ForecastCacheName = "forecast";

    private readonly NwsClientOptions _options = options.Value;

    public async Task<Forecast?> GetForecastAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default)
    {
        if (await ResolveMetadataAsync(coordinate.Rounded(), cancellationToken) is not { } metadata)
        {
            return null;
        }

        var forecast = await GetForecastByGridAsync(metadata.Grid, cancellationToken);
        if (forecast is not null)
        {
            warmer.RequestWarming(metadata.Grid);
        }

        return forecast;
    }

    public async Task<NeighborhoodForecast?> GetNeighborhoodForecastAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default)
    {
        var rounded = coordinate.Rounded();
        if (await ResolveMetadataAsync(rounded, cancellationToken) is not { } metadata
            || await GetForecastByGridAsync(metadata.Grid, cancellationToken) is not { } primary)
        {
            return null;
        }

        warmer.RequestWarming(metadata.Grid);

        var now = timeProvider.GetUtcNow();
        List<Forecast> neighbors = [];
        foreach (var cell in GridNeighborhood.Surrounding(metadata.Grid))
        {
            if (await forecastCache.GetAsync(cell, cancellationToken) is { } cached && cached.IsFresh(now))
            {
                neighbors.Add(cached.Forecast);
            }
        }

        return new NeighborhoodForecast(rounded, primary, neighbors);
    }

    public async Task<Forecast?> GetForecastByGridAsync(GridPoint grid, CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var cached = await forecastCache.GetAsync(grid, cancellationToken);
        if (cached is not null && cached.IsFresh(now))
        {
            telemetry.RecordCacheHit(ForecastCacheName);
            return cached.Forecast;
        }

        telemetry.RecordCacheMiss(ForecastCacheName);

        switch (await nwsClient.GetForecastAsync(grid, cached?.ETag, cancellationToken))
        {
            case ForecastFetchResult.Fetched fetched:
                await forecastCache.UpsertAsync(
                    new CachedForecast(fetched.Forecast, fetched.ETag, now, ComputeExpiry(now, fetched.MaxAge)),
                    cancellationToken);
                return fetched.Forecast;

            case ForecastFetchResult.NotModified notModified when cached is not null:
                await forecastCache.UpsertAsync(
                    cached with
                    {
                        ETag = notModified.ETag ?? cached.ETag,
                        RetrievedAtUtc = now,
                        ExpiresAtUtc = ComputeExpiry(now, notModified.MaxAge),
                    },
                    cancellationToken);
                return cached.Forecast;

            case ForecastFetchResult.Unavailable when cached is not null:
                logger.ServingStaleForecast(grid);
                return cached.Forecast;

            default:
                return null;
        }
    }

    private async Task<PointMetadata?> ResolveMetadataAsync(GeoCoordinate coordinate, CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();
        var cached = await metadataCache.GetAsync(coordinate, cancellationToken);
        if (cached is not null && cached.IsFresh(now))
        {
            telemetry.RecordCacheHit(MetadataCacheName);
            return cached.Metadata;
        }

        telemetry.RecordCacheMiss(MetadataCacheName);

        if (await nwsClient.GetPointMetadataAsync(coordinate, cancellationToken) is { } fetched)
        {
            await metadataCache.UpsertAsync(new CachedPointMetadata(fetched, now, now + _options.PointMetadataTtl), cancellationToken);
            return fetched;
        }

        if (cached is not null)
        {
            logger.ServingStaleMetadata(coordinate);
        }

        return cached?.Metadata;
    }

    private DateTimeOffset ComputeExpiry(DateTimeOffset now, TimeSpan? maxAge)
    {
        var ttl = maxAge is { } value && value > TimeSpan.Zero ? value : _options.DefaultForecastTtl;
        return now + (ttl > _options.MaxForecastTtl ? _options.MaxForecastTtl : ttl);
    }
}
