using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Weather.Api.Tests.Fakes;
using Weather.Core.Abstractions;

namespace Weather.Api.Tests;

internal sealed class WeatherApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"weather-api-tests-{Guid.NewGuid():N}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("Cache:ConnectionString", $"Data Source={_databasePath}");
        builder.ConfigureTestServices(static services =>
        {
            services.RemoveAll<INwsApiClient>();
            services.AddSingleton<INwsApiClient, FakeNwsApiClient>();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
        {
            return;
        }

        SqliteConnection.ClearAllPools();
        string[] files = [_databasePath, $"{_databasePath}-wal", $"{_databasePath}-shm"];
        foreach (var file in files)
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
            }
        }
    }
}
