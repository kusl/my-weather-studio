namespace Weather.Core.Models;

public closed record ForecastFetchResult
{
    public sealed record Fetched(Forecast Forecast, string? ETag, TimeSpan? MaxAge) : ForecastFetchResult;

    public sealed record NotModified(string? ETag, TimeSpan? MaxAge) : ForecastFetchResult;

    public sealed record NotFound : ForecastFetchResult;

    public sealed record Unavailable : ForecastFetchResult;
}
