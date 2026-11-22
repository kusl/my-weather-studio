using Weather.Core.Models;

namespace Weather.Core.Abstractions;

public interface IWeatherService
{
    Task<Forecast?> GetForecastAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default);

    Task<NeighborhoodForecast?> GetNeighborhoodForecastAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default);

    Task<Forecast?> GetForecastByGridAsync(GridPoint grid, CancellationToken cancellationToken = default);
}
