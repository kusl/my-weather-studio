namespace Weather.Core.Models;

public sealed record ForecastPeriod(
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
