using Dapper;
using Weather.Core.Abstractions;
using Weather.Core.Models;

namespace Weather.Infrastructure.Caching;

internal sealed class SqlitePointMetadataCache(ISqliteConnectionFactory connectionFactory) : IPointMetadataCache
{
    private const string SelectSql =
        """
        SELECT Latitude, Longitude, GridId, GridX, GridY, ForecastUrl, ForecastHourlyUrl,
               City, State, TimeZone, RadarStation, RetrievedAtUtc, ExpiresAtUtc
        FROM PointMetadata
        WHERE CoordinateKey = @CoordinateKey;
        """;

    private const string UpsertSql =
        """
        INSERT INTO PointMetadata
            (CoordinateKey, Latitude, Longitude, GridId, GridX, GridY, ForecastUrl, ForecastHourlyUrl,
             City, State, TimeZone, RadarStation, RetrievedAtUtc, ExpiresAtUtc)
        VALUES
            (@CoordinateKey, @Latitude, @Longitude, @GridId, @GridX, @GridY, @ForecastUrl, @ForecastHourlyUrl,
             @City, @State, @TimeZone, @RadarStation, @RetrievedAtUtc, @ExpiresAtUtc)
        ON CONFLICT(CoordinateKey) DO UPDATE SET
            Latitude          = excluded.Latitude,
            Longitude         = excluded.Longitude,
            GridId            = excluded.GridId,
            GridX             = excluded.GridX,
            GridY             = excluded.GridY,
            ForecastUrl       = excluded.ForecastUrl,
            ForecastHourlyUrl = excluded.ForecastHourlyUrl,
            City              = excluded.City,
            State             = excluded.State,
            TimeZone          = excluded.TimeZone,
            RadarStation      = excluded.RadarStation,
            RetrievedAtUtc    = excluded.RetrievedAtUtc,
            ExpiresAtUtc      = excluded.ExpiresAtUtc;
        """;

    public async Task<CachedPointMetadata?> GetAsync(GeoCoordinate coordinate, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var key = coordinate.Rounded().ToCacheKey();
        var row = await connection.QuerySingleOrDefaultAsync<MetadataRow>(
            new CommandDefinition(SelectSql, new { CoordinateKey = key }, cancellationToken: cancellationToken));

        if (row is null)
        {
            return null;
        }

        var metadata = new PointMetadata(
            Query: new GeoCoordinate(row.Latitude, row.Longitude),
            Grid: new GridPoint(row.GridId, row.GridX, row.GridY),
            ForecastUrl: row.ForecastUrl,
            ForecastHourlyUrl: row.ForecastHourlyUrl,
            City: row.City,
            State: row.State,
            TimeZone: row.TimeZone,
            RadarStation: row.RadarStation);

        return new CachedPointMetadata(
            metadata,
            TimestampText.Parse(row.RetrievedAtUtc),
            TimestampText.Parse(row.ExpiresAtUtc));
    }

    public async Task UpsertAsync(CachedPointMetadata entry, CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        var coordinate = entry.Metadata.Query.Rounded();
        var grid = entry.Metadata.Grid;

        await connection.ExecuteAsync(new CommandDefinition(UpsertSql, new
        {
            CoordinateKey = coordinate.ToCacheKey(),
            coordinate.Latitude,
            coordinate.Longitude,
            grid.GridId,
            grid.GridX,
            grid.GridY,
            entry.Metadata.ForecastUrl,
            entry.Metadata.ForecastHourlyUrl,
            entry.Metadata.City,
            entry.Metadata.State,
            entry.Metadata.TimeZone,
            entry.Metadata.RadarStation,
            RetrievedAtUtc = TimestampText.ToText(entry.RetrievedAtUtc),
            ExpiresAtUtc = TimestampText.ToText(entry.ExpiresAtUtc),
        }, cancellationToken: cancellationToken));
    }

    private sealed class MetadataRow
    {
        public double Latitude { get; set; }

        public double Longitude { get; set; }

        public string GridId { get; set; } = string.Empty;

        public int GridX { get; set; }

        public int GridY { get; set; }

        public string ForecastUrl { get; set; } = string.Empty;

        public string ForecastHourlyUrl { get; set; } = string.Empty;

        public string? City { get; set; }

        public string? State { get; set; }

        public string? TimeZone { get; set; }

        public string? RadarStation { get; set; }

        public string RetrievedAtUtc { get; set; } = string.Empty;

        public string ExpiresAtUtc { get; set; } = string.Empty;
    }
}
