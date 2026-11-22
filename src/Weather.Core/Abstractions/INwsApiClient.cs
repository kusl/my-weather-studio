using Weather.Core.Models;

namespace Weather.Core.Abstractions;

public interface INwsApiClient
{
    Task<PointMetadata?> GetPointMetadataAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default);

    Task<ForecastFetchResult> GetForecastAsync(GridPoint grid, string? etag = null, CancellationToken cancellationToken = default);
}
