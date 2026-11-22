namespace Weather.Core.Models;

public sealed record CachedForecast(Forecast Forecast, string? ETag, DateTimeOffset RetrievedAtUtc, DateTimeOffset ExpiresAtUtc)
{
    public bool IsFresh(DateTimeOffset now) => ExpiresAtUtc > now;
}
