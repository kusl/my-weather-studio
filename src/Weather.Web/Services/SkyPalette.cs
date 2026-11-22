namespace Weather.Web.Services;

public readonly record struct SkyTheme(string From, string To, string Foreground, string Accent, string Label);

public static class SkyPalette
{
    private const string Accent = "#FFB454";

    private static readonly (string[] Keywords, SkyTheme Day, SkyTheme Night)[] Rules =
    [
        (["thunder", "tstorm", "storm"],
            new("#2B3A4A", "#51657A", "#FFFFFF", Accent, "Storms"),
            new("#0E1620", "#243140", "#EAF2FF", Accent, "Storms")),
        (["snow", "sleet", "flurr", "wintry", "ice"],
            new("#9FB4CC", "#E2ECF6", "#16202E", Accent, "Snow"),
            new("#2A3850", "#4A5C76", "#F0F5FB", Accent, "Snow")),
        (["rain", "shower", "drizzle"],
            new("#3F6079", "#7E97AB", "#FFFFFF", Accent, "Rain"),
            new("#16222F", "#2C3E50", "#EAF2FF", Accent, "Rain")),
        (["fog", "haze", "mist", "smoke"],
            new("#9AA6AE", "#CDD6DC", "#1A222B", Accent, "Fog"),
            new("#2A3138", "#49535C", "#EDF1F4", Accent, "Fog")),
        (["cloud", "overcast"],
            new("#8A9BB0", "#C9D4E0", "#1A2433", Accent, "Cloudy"),
            new("#20293A", "#3C4A60", "#E6ECF5", Accent, "Cloudy")),
    ];

    private static readonly SkyTheme ClearDay = new("#4DA6FF", "#BFE3FF", "#0B1F33", Accent, "Clear");
    private static readonly SkyTheme ClearNight = new("#0B1F3A", "#1C3A5E", "#EAF2FF", Accent, "Clear");

    public static SkyTheme ForPeriod(bool isDaytime, string? shortForecast)
    {
        var text = shortForecast ?? string.Empty;
        foreach (var (keywords, day, night) in Rules)
        {
            if (keywords.Any(keyword => text.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
            {
                return isDaytime ? day : night;
            }
        }

        return isDaytime ? ClearDay : ClearNight;
    }
}
