using Weather.Core.Abstractions;
using Weather.Core.Models;

namespace Weather.Api.Tests.Fakes;

internal sealed class FakeNwsApiClient : INwsApiClient
{
    public Task<PointMetadata?> GetPointMetadataAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default)
    {
        if (Math.Abs(coordinate.Latitude) < 0.001 && Math.Abs(coordinate.Longitude) < 0.001)
        {
            return Task.FromResult<PointMetadata?>(null);
        }

        return Task.FromResult<PointMetadata?>(new PointMetadata(
            Query: coordinate,
            Grid: new GridPoint("AKQ", 83, 61),
            ForecastUrl: "https://api.weather.gov/gridpoints/AKQ/83,61/forecast",
            ForecastHourlyUrl: "https://api.weather.gov/gridpoints/AKQ/83,61/forecast/hourly",
            City: "Bethel Manor",
            State: "VA",
            TimeZone: "America/New_York",
            RadarStation: "KAKQ"));
    }

    public Task<ForecastFetchResult> GetForecastAsync(GridPoint grid, string? etag = null, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var forecast = new Forecast(grid, now, now,
        [
            new ForecastPeriod(1, "This Afternoon", now, now.AddHours(4), true, 90, "F", 3,
                "5 to 12 mph", "S", "Sunny", "Sunny, with a high near 90.", "icon-day"),
            new ForecastPeriod(2, "Tonight", now.AddHours(4), now.AddHours(16), false, 74, "F", null,
                "12 mph", "S", "Mostly Clear", "Mostly clear, with a low around 74.", "icon-night"),
        ]);

        return Task.FromResult<ForecastFetchResult>(new ForecastFetchResult.Fetched(forecast, "\"e\"", TimeSpan.FromMinutes(30)));
    }
}
