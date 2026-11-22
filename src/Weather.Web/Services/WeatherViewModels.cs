namespace Weather.Web.Services;

public sealed record ForecastDto(
    string GridId,
    int GridX,
    int GridY,
    DateTimeOffset GeneratedAt,
    DateTimeOffset UpdateTime,
    IReadOnlyList<ForecastPeriodDto> Periods);

public sealed record ForecastPeriodDto(
    int Number,
    string Name,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsDaytime,
    int Temperature,
    string TemperatureUnit,
    int? ProbabilityOfPrecipitation,
    string WindSpeed,
    string WindDirection,
    string ShortForecast,
    string DetailedForecast,
    string Icon);

public sealed record NeighborhoodDto(double Latitude, double Longitude, ForecastDto Primary, IReadOnlyList<ForecastDto> Neighbors);
