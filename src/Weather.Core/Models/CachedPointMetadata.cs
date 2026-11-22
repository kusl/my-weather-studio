namespace Weather.Core.Models;

public sealed record CachedPointMetadata(PointMetadata Metadata, DateTimeOffset RetrievedAtUtc, DateTimeOffset ExpiresAtUtc)
{
    public bool IsFresh(DateTimeOffset now) => ExpiresAtUtc > now;
}
