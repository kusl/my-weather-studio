using System.Threading.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Weather.Core.Abstractions;
using Weather.Core.Telemetry;
using Weather.Infrastructure.Caching;
using Weather.Infrastructure.Nws;
using Weather.Infrastructure.Services;

namespace Weather.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddWeatherInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<NwsClientOptions>(configuration.GetSection(NwsClientOptions.SectionName));
        services.Configure<SqliteCacheOptions>(configuration.GetSection(SqliteCacheOptions.SectionName));

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<WeatherTelemetry>();

        services.TryAddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.TryAddSingleton<IForecastCache, SqliteForecastCache>();
        services.TryAddSingleton<IPointMetadataCache, SqlitePointMetadataCache>();
        services.AddHostedService<DatabaseInitializer>();

        services.AddHttpClient<INwsApiClient, NwsApiClient>(static (serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<NwsClientOptions>>().Value;
                client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
                client.Timeout = Timeout.InfiniteTimeSpan;
                if (!client.DefaultRequestHeaders.UserAgent.TryParseAdd(options.UserAgent))
                {
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
                }

                client.DefaultRequestHeaders.Accept.ParseAdd("application/geo+json");
            })
            .AddStandardResilienceHandler()
            .Configure(static (resilience, serviceProvider) =>
                ApplyTimeouts(resilience, serviceProvider.GetRequiredService<IOptions<NwsClientOptions>>().Value.RequestTimeout));

        services.TryAddScoped<IWeatherService, WeatherService>();

        services.TryAddSingleton<INeighborhoodWarmer, NeighborhoodWarmer>();
        services.TryAddSingleton<RateLimiter>(static _ => new TokenBucketRateLimiter(new TokenBucketRateLimiterOptions
        {
            TokenLimit = 20,
            TokensPerPeriod = 10,
            ReplenishmentPeriod = TimeSpan.FromSeconds(1),
            AutoReplenishment = true,
            QueueLimit = 64,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
        }));
        services.AddHostedService<NeighborhoodWarmingBackgroundService>();

        return services;
    }

    private static void ApplyTimeouts(HttpStandardResilienceOptions resilience, TimeSpan attemptTimeout)
    {
        resilience.AttemptTimeout.Timeout = attemptTimeout;
        resilience.TotalRequestTimeout.Timeout = attemptTimeout * 3;
        resilience.CircuitBreaker.SamplingDuration = attemptTimeout * 2;
    }
}
