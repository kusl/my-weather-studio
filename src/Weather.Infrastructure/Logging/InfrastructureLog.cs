using Microsoft.Extensions.Logging;
using Weather.Core.Models;

namespace Weather.Infrastructure.Logging;

internal static partial class InfrastructureLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "SQLite cache schema initialised (WAL enabled).")]
    public static partial void SchemaInitialised(this ILogger logger);

    [LoggerMessage(EventId = 1100, Level = LogLevel.Information, Message = "NWS has no grid coverage for {Coordinate}.")]
    public static partial void NoPointCoverage(this ILogger logger, GeoCoordinate coordinate);

    [LoggerMessage(EventId = 1101, Level = LogLevel.Warning, Message = "NWS /points returned {StatusCode} for {Coordinate}.")]
    public static partial void PointsFailed(this ILogger logger, int statusCode, GeoCoordinate coordinate);

    [LoggerMessage(EventId = 1102, Level = LogLevel.Warning, Message = "NWS /points payload for {Coordinate} was missing grid metadata.")]
    public static partial void PointsPayloadIncomplete(this ILogger logger, GeoCoordinate coordinate);

    [LoggerMessage(EventId = 1103, Level = LogLevel.Warning, Message = "NWS /points request failed for {Coordinate}.")]
    public static partial void PointsRequestFailed(this ILogger logger, Exception exception, GeoCoordinate coordinate);

    [LoggerMessage(EventId = 1200, Level = LogLevel.Information, Message = "NWS has no forecast for grid {Grid}.")]
    public static partial void NoForecastCoverage(this ILogger logger, GridPoint grid);

    [LoggerMessage(EventId = 1201, Level = LogLevel.Warning, Message = "NWS throttled the forecast request for grid {Grid} (HTTP 429).")]
    public static partial void ForecastThrottled(this ILogger logger, GridPoint grid);

    [LoggerMessage(EventId = 1202, Level = LogLevel.Warning, Message = "NWS /forecast returned {StatusCode} for grid {Grid}.")]
    public static partial void ForecastFailed(this ILogger logger, int statusCode, GridPoint grid);

    [LoggerMessage(EventId = 1203, Level = LogLevel.Warning, Message = "NWS /forecast payload for grid {Grid} was empty.")]
    public static partial void ForecastPayloadEmpty(this ILogger logger, GridPoint grid);

    [LoggerMessage(EventId = 1204, Level = LogLevel.Warning, Message = "NWS /forecast request failed for grid {Grid}.")]
    public static partial void ForecastRequestFailed(this ILogger logger, Exception exception, GridPoint grid);

    [LoggerMessage(EventId = 1300, Level = LogLevel.Warning, Message = "Serving stale point metadata for {Coordinate}; NWS resolve failed.")]
    public static partial void ServingStaleMetadata(this ILogger logger, GeoCoordinate coordinate);

    [LoggerMessage(EventId = 1301, Level = LogLevel.Warning, Message = "Serving stale forecast for grid {Grid}; NWS unavailable.")]
    public static partial void ServingStaleForecast(this ILogger logger, GridPoint grid);

    [LoggerMessage(EventId = 1400, Level = LogLevel.Information, Message = "Neighbourhood warming service started.")]
    public static partial void WarmingStarted(this ILogger logger);

    [LoggerMessage(EventId = 1401, Level = LogLevel.Information, Message = "Neighbourhood warming service stopping.")]
    public static partial void WarmingStopping(this ILogger logger);

    [LoggerMessage(EventId = 1402, Level = LogLevel.Error, Message = "Failed to warm the neighbourhood around {Origin}.")]
    public static partial void WarmingFailed(this ILogger logger, Exception exception, GridPoint origin);

    [LoggerMessage(EventId = 1403, Level = LogLevel.Debug, Message = "Rate limiter rejected warming for {Cell}; skipping.")]
    public static partial void WarmingRateLimited(this ILogger logger, GridPoint cell);

    [LoggerMessage(EventId = 1404, Level = LogLevel.Debug, Message = "Neighbourhood warm queue closed; dropped origin {Origin}.")]
    public static partial void WarmingQueueClosed(this ILogger logger, GridPoint origin);
}
