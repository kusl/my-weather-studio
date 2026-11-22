using System.Globalization;

namespace Weather.Infrastructure.Caching;

internal static class TimestampText
{
    public static string ToText(DateTimeOffset value) => value.ToString("O", CultureInfo.InvariantCulture);

    public static DateTimeOffset Parse(string text) =>
        DateTimeOffset.Parse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
}
