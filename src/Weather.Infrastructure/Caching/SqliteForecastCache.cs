using System.Text.Json;
using Dapper;
using Weather.Core.Abstractions;
using Weather.Core.Models;
using Weather.Infrastructure.Serialization;

namespace Weather.Infrastructure.Caching;

internal sealed class SqliteForecastCache(ISqliteConnectionFactory connectionFactory) : IForecastCache
{
    private const string SelectSql =
        """
        SELECT Payload, ETag, RetrievedAtUtc, ExpiresAtUtc
        FROM GridForecast
        WHERE GridId = @GridId AND GridX = @GridX AND GridY = @GridY;
        """;

    private const string UpsertSql =
        """
        INSERT INTO GridForecast
            (GridId, GridX, GridY, Payload, ETag, GeneratedAt, UpdateTime, RetrievedAtUtc, ExpiresAtUtc)
        VALUES
            (@GridId, @GridX, @GridY, @Payload, @ETag, @GeneratedAt, @UpdateTime, @RetrievedAtUtc, @ExpiresAtUtc)
        ON CONFLICT(GridId, GridX, GridY) DO UPDATE SET
            Payload        = excluded.Payload,
            ETag           = excluded.ETag,
            GeneratedAt    = excluded.GeneratedAt,
            UpdateTime     = excluded.UpdateTime,
            RetrievedAtUtc = excluded.RetrievedAtUtc,
            ExpiresAtUtc   = excluded.ExpiresAtUtc;
        """;

    public async Task<CachedForecast?> GetAsync(GridPoint grid, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var row = await connection.QuerySingleOrDefaultAsync<ForecastRow>(
            new CommandDefinition(SelectSql, new { grid.GridId, grid.GridX, grid.GridY }, cancellationToken: cancellationToken));

        if (row is null || JsonSerializer.Deserialize(row.Payload, WeatherJsonContext.Default.Forecast) is not { } forecast)
        {
            return null;
        }

        return new CachedForecast(forecast, row.ETag, TimestampText.Parse(row.RetrievedAtUtc), TimestampText.Parse(row.ExpiresAtUtc));
    }

    public async Task UpsertAsync(CachedForecast entry, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var grid = entry.Forecast.Grid;
        await connection.ExecuteAsync(new CommandDefinition(UpsertSql, new
        {
            grid.GridId,
            grid.GridX,
            grid.GridY,
            Payload = JsonSerializer.Serialize(entry.Forecast, WeatherJsonContext.Default.Forecast),
            entry.ETag,
            GeneratedAt = TimestampText.ToText(entry.Forecast.GeneratedAt),
            UpdateTime = TimestampText.ToText(entry.Forecast.UpdateTime),
            RetrievedAtUtc = TimestampText.ToText(entry.RetrievedAtUtc),
            ExpiresAtUtc = TimestampText.ToText(entry.ExpiresAtUtc),
        }, cancellationToken: cancellationToken));
    }

    private sealed class ForecastRow
    {
        public string Payload { get; set; } = string.Empty;

        public string? ETag { get; set; }

        public string RetrievedAtUtc { get; set; } = string.Empty;

        public string ExpiresAtUtc { get; set; } = string.Empty;
    }
}
