using Weather.Core.Models;

namespace Weather.Api.Contracts;

public sealed record ForecastResponse(
    string GridId,
    int GridX,
    int GridY,
    DateTimeOffset GeneratedAt,
    DateTimeOffset UpdateTime,
    IReadOnlyList<ForecastPeriodResponse> Periods)
{
    public static ForecastResponse FromDomain(Forecast forecast) => new(
        forecast.Grid.GridId,
        forecast.Grid.GridX,
        forecast.Grid.GridY,
        forecast.GeneratedAt,
        forecast.UpdateTime,
        [.. forecast.Periods.Select(ForecastPeriodResponse.FromDomain)]);
}

public sealed record ForecastPeriodResponse(
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
    string Icon)
{
    public static ForecastPeriodResponse FromDomain(ForecastPeriod period) => new(
        period.Number,
        period.Name,
        period.StartTime,
        period.EndTime,
        period.IsDaytime,
        period.Temperature,
        period.TemperatureUnit,
        period.ProbabilityOfPrecipitation,
        period.WindSpeed,
        period.WindDirection,
        period.ShortForecast,
        period.DetailedForecast,
        period.Icon);
}
