using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Polly;
using Weather.Core.Abstractions;
using Weather.Core.Models;
using Weather.Core.Telemetry;
using Weather.Infrastructure.Logging;
using Weather.Infrastructure.Serialization;

namespace Weather.Infrastructure.Nws;

internal sealed class NwsApiClient(HttpClient httpClient, WeatherTelemetry telemetry, ILogger<NwsApiClient> logger) : INwsApiClient
{
    private const string PointsEndpoint = "points";
    private const string ForecastEndpoint = "forecast";

    public async Task<PointMetadata?> GetPointMetadataAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default)
    {
        using var activity = telemetry.ActivitySource.StartActivity("NWS points", ActivityKind.Internal);
        activity?.SetTag("weather.coordinate", coordinate.ToApiString());
        var started = Stopwatch.GetTimestamp();

        try
        {
            using var response = await httpClient.GetAsync($"points/{coordinate.ToApiString()}", cancellationToken);
            RecordDuration(PointsEndpoint, response.StatusCode, started, conditional: false);
            activity?.SetTag("http.response.status_code", (int)response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                logger.NoPointCoverage(coordinate);
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                RecordFailure(PointsEndpoint, response.StatusCode);
                logger.PointsFailed((int)response.StatusCode, coordinate);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync(WeatherJsonContext.Default.NwsPointResponse, cancellationToken);
            if (payload?.Properties is not { GridId: { } gridId } properties)
            {
                logger.PointsPayloadIncomplete(coordinate);
                return null;
            }

            var location = properties.RelativeLocation?.Properties;
            return new PointMetadata(
                Query: coordinate,
                Grid: new GridPoint(gridId, properties.GridX, properties.GridY),
                ForecastUrl: properties.Forecast ?? string.Empty,
                ForecastHourlyUrl: properties.ForecastHourly ?? string.Empty,
                City: location?.City,
                State: location?.State,
                TimeZone: properties.TimeZone,
                RadarStation: properties.RadarStation);
        }
        catch (Exception ex) when (IsUpstreamFailure(ex, cancellationToken))
        {
            RecordFailure(PointsEndpoint, statusCode: null);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.PointsRequestFailed(ex, coordinate);
            return null;
        }
    }

    public async Task<ForecastFetchResult> GetForecastAsync(GridPoint grid, string? etag = null, CancellationToken cancellationToken = default)
    {
        var conditional = !string.IsNullOrEmpty(etag);
        using var activity = telemetry.ActivitySource.StartActivity("NWS forecast", ActivityKind.Internal);
        activity?.SetTag("weather.grid", grid.ToString());
        activity?.SetTag("weather.conditional", conditional);
        var started = Stopwatch.GetTimestamp();

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                string.Create(CultureInfo.InvariantCulture, $"gridpoints/{grid.GridId}/{grid.GridX},{grid.GridY}/forecast"));

            if (conditional)
            {
                request.Headers.TryAddWithoutValidation("If-None-Match", etag);
            }

            using var response = await httpClient.SendAsync(request, cancellationToken);
            RecordDuration(ForecastEndpoint, response.StatusCode, started, conditional);
            activity?.SetTag("http.response.status_code", (int)response.StatusCode);

            var maxAge = response.Headers.CacheControl?.MaxAge;
            switch (response.StatusCode)
            {
                case HttpStatusCode.NotModified:
                    return new ForecastFetchResult.NotModified(response.Headers.ETag?.ToString() ?? etag, maxAge);

                case HttpStatusCode.NotFound:
                    logger.NoForecastCoverage(grid);
                    return new ForecastFetchResult.NotFound();

                case HttpStatusCode.TooManyRequests:
                    telemetry.RecordThrottled(ForecastEndpoint);
                    logger.ForecastThrottled(grid);
                    return new ForecastFetchResult.Unavailable();

                case var status when !response.IsSuccessStatusCode:
                    RecordFailure(ForecastEndpoint, status);
                    logger.ForecastFailed((int)status, grid);
                    return new ForecastFetchResult.Unavailable();
            }

            var payload = await response.Content.ReadFromJsonAsync(WeatherJsonContext.Default.NwsForecastResponse, cancellationToken);
            if (payload?.Properties is not { } properties)
            {
                logger.ForecastPayloadEmpty(grid);
                return new ForecastFetchResult.Unavailable();
            }

            return new ForecastFetchResult.Fetched(MapForecast(grid, properties), response.Headers.ETag?.ToString(), maxAge);
        }
        catch (Exception ex) when (IsUpstreamFailure(ex, cancellationToken))
        {
            RecordFailure(ForecastEndpoint, statusCode: null);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.ForecastRequestFailed(ex, grid);
            return new ForecastFetchResult.Unavailable();
        }
    }

    private static bool IsUpstreamFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or TaskCanceledException or ExecutionRejectedException or JsonException
        && !cancellationToken.IsCancellationRequested;

    private static Forecast MapForecast(GridPoint grid, NwsForecastProperties properties) =>
        new(grid, properties.GeneratedAt, properties.UpdateTime, [.. (properties.Periods ?? []).Select(MapPeriod)]);

    private static ForecastPeriod MapPeriod(NwsPeriod period) => new(
        Number: period.Number,
        Name: period.Name ?? string.Empty,
        StartTime: period.StartTime,
        EndTime: period.EndTime,
        IsDaytime: period.IsDaytime,
        Temperature: period.Temperature,
        TemperatureUnit: period.TemperatureUnit ?? "F",
        ProbabilityOfPrecipitation: period.ProbabilityOfPrecipitation?.Value is { } pop
            ? (int)Math.Round(pop, MidpointRounding.AwayFromZero)
            : null,
        WindSpeed: period.WindSpeed ?? string.Empty,
        WindDirection: period.WindDirection ?? string.Empty,
        ShortForecast: period.ShortForecast ?? string.Empty,
        DetailedForecast: period.DetailedForecast ?? string.Empty,
        Icon: period.Icon ?? string.Empty);

    private void RecordDuration(string endpoint, HttpStatusCode statusCode, long started, bool conditional) =>
        telemetry.RecordNwsRequest(endpoint, (int)statusCode, Stopwatch.GetElapsedTime(started).TotalMilliseconds, conditional);

    private void RecordFailure(string endpoint, HttpStatusCode? statusCode) =>
        telemetry.RecordError(endpoint, statusCode is { } code ? (int)code : 0);
}
