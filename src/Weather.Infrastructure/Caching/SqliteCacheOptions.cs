namespace Weather.Infrastructure.Caching;

public sealed class SqliteCacheOptions
{
    public const string SectionName = "Cache";

    public string ConnectionString { get; set; } = "Data Source=weather-cache.db";
}
