using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;

namespace Weather.Infrastructure.Caching;

public interface ISqliteConnectionFactory
{
    Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default);
}

public sealed class SqliteConnectionFactory(IOptions<SqliteCacheOptions> options) : ISqliteConnectionFactory
{
    private readonly string _connectionString = options.Value.ConnectionString;

    public async Task<SqliteConnection> CreateOpenConnectionAsync(CancellationToken cancellationToken = default)
    {
        var connection = new SqliteConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }
}
