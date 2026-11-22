# Weather.Infrastructure

The implementation layer. Everything that touches the network, the database, or
background work lives here: the NWS HTTP client, the two SQLite/Dapper caches,
the orchestrating `WeatherService`, and the background neighbourhood warmer. It
implements the abstractions from [`Weather.Core`](../Weather.Core/README.md).

Most types are **`internal`** — the public surface is one extension method,
`AddWeatherInfrastructure`, plus a few options and connection types. Internals
are made visible to the test project via `InternalsVisibleTo`.

The project references the ASP.NET Core shared framework, which already ships
hosting, options, logging and `System.Threading.RateLimiting`, so the only
packages are Dapper, `Microsoft.Data.Sqlite` and
`Microsoft.Extensions.Http.Resilience`. All logging uses `[LoggerMessage]` source
generation (`Logging/InfrastructureLog.cs`).

## The NWS client (`Nws/`)

`NwsApiClient` is a typed `HttpClient` over `api.weather.gov`. It owns HTTP
concerns only — it does **no caching**.

- Two calls: `points/{lat},{lon}` to resolve a coordinate to a grid cell, and
  `gridpoints/{id}/{x},{y}/forecast` for the forecast.
- **Conditional GETs**: a known ETag is sent as `If-None-Match` (added without
  validation so weak/quoted tags round-trip exactly); a `304` becomes a
  `NotModified` result carrying the new `max-age`.
- **Defensive parsing**: the wire DTOs (`NwsJsonModels`) are positional records
  that mirror only the fields consumed and are fully nullable, because the
  upstream API is outside our control. A null probability-of-precipitation maps
  to `null`, not `0`. Parsing goes through the source-generated
  `WeatherJsonContext` (no reflection).
- **Failure mapping**: `404` → `NotFound`; `429` → throttled + `Unavailable`;
  other non-success, a transport exception, an unparseable payload, or a Polly rejection (attempt/total
  timeout, open circuit) → `Unavailable`. Cancellation is never swallowed.
- Every call is timed, tagged for metrics, and wrapped in a `Weather.Nws`
  tracing span.

The `HttpClient` itself — base address, infinite client-side timeout (the
resilience pipeline owns timeouts), required `User-Agent`, and
`Accept: application/geo+json` — is configured in the composition root, with
`AddStandardResilienceHandler()` (retry with jitter, circuit breaker, timeout)
layered on. `Nws:RequestTimeout` sets the per-attempt timeout; the total timeout
and circuit-breaker sampling window are derived from it so the pipeline's
validation rules always hold.

## The caches (`Caching/`)

Two SQLite tiers, both via **Dapper**:

- **`SqlitePointMetadataCache`** — coordinate→grid metadata, keyed by the
  coordinate rounded to four decimals so nearby users collapse onto one row.
  Long TTL (the mapping effectively never changes).
- **`SqliteForecastCache`** — the forecast payload as JSON, keyed by
  `(GridId, GridX, GridY)`, with ETag and timestamps in their own columns. Short
  TTL, honouring NWS `Cache-Control`.

Supporting pieces:

- **`DatabaseInitializer`** (an `IHostedService`) creates the schema on startup.
  It's idempotent (`IF NOT EXISTS`) and enables **WAL** mode for read/write
  concurrency.
- **`SqliteConnectionFactory`** / `ISqliteConnectionFactory` open connections
  from `SqliteCacheOptions.ConnectionString`.
- **`TimestampText`** centralises ISO-8601 round-trip ("O") formatting/parsing
  with the invariant culture, so writes and reads can never drift.
- Rows are materialised into small mutable row classes, which lets Dapper
  convert SQLite's 64-bit `INTEGER` columns to `int` (constructor-based mapping
  of positional records requires exact type matches and fails on SQLite).

The schema, the coordinate key format and the JSON payload shape are stable:
existing cache databases keep working unchanged, and tests pin compatibility
with payloads written by earlier versions.

## The orchestrator (`Services/WeatherService.cs`)

`WeatherService` is the only type that knows the **freshness policy**, because
only it knows the *semantic* outcome of a lookup. For a coordinate it:

1. resolves metadata (cache → NWS `/points`), serving stale metadata if NWS
   can't be reached;
2. reads the forecast cache; if fresh, returns it (and records a cache hit);
3. otherwise calls NWS with any known ETag and switches over the closed
   `ForecastFetchResult` hierarchy:
   - **Fetched** → store and return;
   - **NotModified** → extend the TTL and return the cached payload (the cheapest
     path — no transfer, no re-parse);
   - **Unavailable** with a cached copy → serve it **stale** (no write);
   - **NotFound** / unavailable-with-no-cache → `null`.

When a forecast is produced via the user-facing entry points, it enqueues the
origin cell for background warming. `GetForecastByGridAsync` (used by the warmer)
deliberately does **not** enqueue, so warming can't recurse.

## The background warmer (`Services/`)

- **`NeighborhoodWarmer`** — a bounded `Channel<GridPoint>` (capacity 256,
  drop-oldest). Enqueuing is non-blocking, so request handlers never wait and a
  spike can't grow memory without bound.
- **`NeighborhoodWarmingBackgroundService`** — consumes the channel and fans out
  to the surrounding cells, guarded by a **global token-bucket rate limiter**, an
  **in-flight dedup set**, and a **skip-if-already-fresh** check. Each cell is
  fetched in its **own DI scope** so the scoped `IWeatherService` is never a
  captive dependency of the singleton service.

## Composition root (`DependencyInjection.cs`)

`AddWeatherInfrastructure(configuration)` wires everything: binds
`NwsClientOptions` (section `"Nws"`) and `SqliteCacheOptions` (section
`"Cache"`); registers `TimeProvider.System`, `WeatherTelemetry`, the caches and
connection factory, the `DatabaseInitializer`, the resilient NWS `HttpClient`,
the scoped `WeatherService`, and the singleton warmer + rate limiter +
background service.

## Configuration

| Section | Key | Default |
| --- | --- | --- |
| `Nws` | `BaseUrl` | `https://api.weather.gov` |
| `Nws` | `UserAgent` | `weather-dashboard/1.0 (+repo URL)` — **override with your contact** |
| `Nws` | `DefaultForecastTtl` | 30 minutes |
| `Nws` | `PointMetadataTtl` | 30 days |
| `Nws` | `MaxForecastTtl` | 6 hours |
| `Cache` | `ConnectionString` | `Data Source=weather-cache.db` |

## Tested by

[`Weather.Infrastructure.Tests`](../../tests/Weather.Infrastructure.Tests/) — the
client against a stub HTTP handler (parsing, conditional GET, every failure
mapping), the caches against a **real temp SQLite database** (including
backward compatibility of the cached payload), the full
`WeatherService` cache-aside policy (with controllable time), and the background
warmer's fan-out and skip-fresh behaviour.
