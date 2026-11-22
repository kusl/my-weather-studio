namespace Weather.Core.Models;

public sealed record PointMetadata(
    GeoCoordinate Query,
    GridPoint Grid,
    string ForecastUrl,
    string ForecastHourlyUrl,
    string? City,
    string? State,
    string? TimeZone,
    string? RadarStation);
