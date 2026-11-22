namespace Weather.Web.Services;

public interface IWeatherApiClient
{
    Task<ForecastDto?> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default);

    Task<NeighborhoodDto?> GetNeighborhoodAsync(double latitude, double longitude, CancellationToken cancellationToken = default);
}
