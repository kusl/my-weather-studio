using Weather.Core.Models;

namespace Weather.Core.Abstractions;

public interface IPointMetadataCache
{
    Task<CachedPointMetadata?> GetAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default);

    Task UpsertAsync(CachedPointMetadata entry, CancellationToken cancellationToken = default);
}
