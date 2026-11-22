namespace Weather.Core.Models;

public sealed record NeighborhoodForecast(GeoCoordinate Query, Forecast Primary, IReadOnlyList<Forecast> Neighbors);
