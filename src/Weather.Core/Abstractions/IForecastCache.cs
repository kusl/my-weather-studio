using Weather.Core.Models;

namespace Weather.Core.Abstractions;

public interface IForecastCache
{
    Task<CachedForecast?> GetAsync(GridPoint grid, CancellationToken cancellationToken = default);

    Task UpsertAsync(CachedForecast entry, CancellationToken cancellationToken = default);
}
