using System.Globalization;
using System.Net;
using System.Net.Http.Json;

namespace Weather.Web.Services;

public sealed class WeatherApiClient(HttpClient httpClient) : IWeatherApiClient
{
    public Task<ForecastDto?> GetForecastAsync(double latitude, double longitude, CancellationToken cancellationToken = default) =>
        GetOrDefaultAsync<ForecastDto>("/api/forecast", latitude, longitude, cancellationToken);

    public Task<NeighborhoodDto?> GetNeighborhoodAsync(double latitude, double longitude, CancellationToken cancellationToken = default) =>
        GetOrDefaultAsync<NeighborhoodDto>("/api/forecast/neighborhood", latitude, longitude, cancellationToken);

    private async Task<T?> GetOrDefaultAsync<T>(string path, double latitude, double longitude, CancellationToken cancellationToken)
        where T : class
    {
        using var response = await httpClient.GetAsync(
            string.Create(CultureInfo.InvariantCulture, $"{path}?latitude={latitude}&longitude={longitude}"),
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken);
    }
}
