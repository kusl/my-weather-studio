namespace Weather.Core.Models;

public sealed record Forecast(
    GridPoint Grid,
    DateTimeOffset GeneratedAt,
    DateTimeOffset UpdateTime,
    IReadOnlyList<ForecastPeriod> Periods);
