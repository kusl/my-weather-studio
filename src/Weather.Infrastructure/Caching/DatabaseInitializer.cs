using Dapper;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Weather.Infrastructure.Logging;

namespace Weather.Infrastructure.Caching;

public sealed class DatabaseInitializer(ISqliteConnectionFactory connectionFactory, ILogger<DatabaseInitializer> logger) : IHostedService
{
    internal const string SchemaSql =
        """
        PRAGMA journal_mode = WAL;
        PRAGMA busy_timeout = 5000;
        PRAGMA foreign_keys = ON;

        CREATE TABLE IF NOT EXISTS PointMetadata (
            CoordinateKey     TEXT    NOT NULL PRIMARY KEY,
            Latitude          REAL    NOT NULL,
            Longitude         REAL    NOT NULL,
            GridId            TEXT    NOT NULL,
            GridX             INTEGER NOT NULL,
            GridY             INTEGER NOT NULL,
            ForecastUrl       TEXT    NOT NULL,
            ForecastHourlyUrl TEXT    NOT NULL,
            City              TEXT    NULL,
            State             TEXT    NULL,
            TimeZone          TEXT    NULL,
            RadarStation      TEXT    NULL,
            RetrievedAtUtc    TEXT    NOT NULL,
            ExpiresAtUtc      TEXT    NOT NULL
        );

        CREATE TABLE IF NOT EXISTS GridForecast (
            GridId         TEXT    NOT NULL,
            GridX          INTEGER NOT NULL,
            GridY          INTEGER NOT NULL,
            Payload        TEXT    NOT NULL,
            ETag           TEXT    NULL,
            GeneratedAt    TEXT    NOT NULL,
            UpdateTime     TEXT    NOT NULL,
            RetrievedAtUtc TEXT    NOT NULL,
            ExpiresAtUtc   TEXT    NOT NULL,
            PRIMARY KEY (GridId, GridX, GridY)
        );

        CREATE INDEX IF NOT EXISTS IX_GridForecast_ExpiresAtUtc
            ON GridForecast (ExpiresAtUtc);
        """;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(SchemaSql, cancellationToken: cancellationToken));
        logger.SchemaInitialised();
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
