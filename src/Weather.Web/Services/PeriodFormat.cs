using System.Globalization;

namespace Weather.Web.Services;

public static class PeriodFormat
{
    private const string Missing = "—";

    public static string Precipitation(ForecastPeriodDto period) =>
        period.ProbabilityOfPrecipitation is { } value
            ? string.Create(CultureInfo.InvariantCulture, $"{value}%")
            : Missing;

    public static string Wind(ForecastPeriodDto period)
    {
        var speed = string.IsNullOrWhiteSpace(period.WindSpeed) ? Missing : period.WindSpeed;
        return string.IsNullOrWhiteSpace(period.WindDirection) ? speed : $"{period.WindDirection} {speed}";
    }
}
