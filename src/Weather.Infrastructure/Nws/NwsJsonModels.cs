namespace Weather.Infrastructure.Nws;

internal sealed record NwsPointResponse(NwsPointProperties? Properties);

internal sealed record NwsPointProperties(
    string? GridId,
    int GridX,
    int GridY,
    string? Forecast,
    string? ForecastHourly,
    string? TimeZone,
    string? RadarStation,
    NwsRelativeLocation? RelativeLocation);

internal sealed record NwsRelativeLocation(NwsRelativeLocationProperties? Properties);

internal sealed record NwsRelativeLocationProperties(string? City, string? State);

internal sealed record NwsForecastResponse(NwsForecastProperties? Properties);

internal sealed record NwsForecastProperties(
    DateTimeOffset GeneratedAt,
    DateTimeOffset UpdateTime,
    IReadOnlyList<NwsPeriod>? Periods);

internal sealed record NwsPeriod(
    int Number,
    string? Name,
    DateTimeOffset StartTime,
    DateTimeOffset EndTime,
    bool IsDaytime,
    int Temperature,
    string? TemperatureUnit,
    NwsQuantitativeValue? ProbabilityOfPrecipitation,
    string? WindSpeed,
    string? WindDirection,
    string? ShortForecast,
    string? DetailedForecast,
    string? Icon);

internal sealed record NwsQuantitativeValue(string? UnitCode, double? Value);
