using Weather.Core.Models;

namespace Weather.Api.Contracts;

public sealed record NeighborhoodResponse(
    double Latitude,
    double Longitude,
    ForecastResponse Primary,
    IReadOnlyList<ForecastResponse> Neighbors)
{
    public static NeighborhoodResponse FromDomain(NeighborhoodForecast neighborhood) => new(
        neighborhood.Query.Latitude,
        neighborhood.Query.Longitude,
        ForecastResponse.FromDomain(neighborhood.Primary),
        [.. neighborhood.Neighbors.Select(ForecastResponse.FromDomain)]);
}
