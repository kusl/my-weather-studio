namespace Weather.Infrastructure.Nws;

public sealed class NwsClientOptions
{
    public const string SectionName = "Nws";

    public string BaseUrl { get; set; } = "https://api.weather.gov";

    public string UserAgent { get; set; } = "weather-dashboard/1.0 (+https://github.com/collabskus/weather)";

    public TimeSpan DefaultForecastTtl { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan PointMetadataTtl { get; set; } = TimeSpan.FromDays(30);

    public TimeSpan MaxForecastTtl { get; set; } = TimeSpan.FromHours(6);

    public TimeSpan RequestTimeout { get; set; } = TimeSpan.FromSeconds(15);
}
