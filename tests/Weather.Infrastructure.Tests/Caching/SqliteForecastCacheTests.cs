using Dapper;
using Weather.Infrastructure.Caching;

namespace Weather.Infrastructure.Tests.Caching;

public sealed class SqliteForecastCacheTests
{
    private static readonly GridPoint Grid = new("AKQ", 83, 61);
    private static readonly DateTimeOffset Now = new(2026, 6, 17, 18, 0, 0, TimeSpan.Zero);

    private static Forecast BuildForecast(DateTimeOffset generatedAt, string firstShort = "Sunny", int firstTemp = 90) =>
        new(
            Grid,
            generatedAt,
            generatedAt.AddMinutes(-30),
            [
                new ForecastPeriod(1, "This Afternoon", generatedAt, generatedAt.AddHours(4),
                    true, firstTemp, "F", 3, "5 to 12 mph", "S", firstShort,
                    "Detailed afternoon text.", "icon-day"),
                new ForecastPeriod(2, "Tonight", generatedAt.AddHours(4), generatedAt.AddHours(16),
                    false, 74, "F", null, "12 mph", "S", "Mostly Clear",
                    "Detailed night text.", "icon-night"),
            ]);

    [Test]
    public async Task Upsert_then_get_round_trips_the_forecast()
    {
        await using var harness = await SqliteCacheHarness.CreateAsync();
        var cache = new SqliteForecastCache(harness.Factory);

        var forecast = BuildForecast(Now);
        var entry = new CachedForecast(forecast, "\"etag-1\"", Now, Now.AddHours(1));

        await cache.UpsertAsync(entry);
        var loaded = await cache.GetAsync(Grid);

        loaded.ShouldNotBeNull();
        loaded!.ETag.ShouldBe("\"etag-1\"");
        loaded.RetrievedAtUtc.ShouldBe(Now);
        loaded.ExpiresAtUtc.ShouldBe(Now.AddHours(1));
        loaded.Forecast.Grid.ShouldBe(Grid);
        loaded.Forecast.Periods.Count.ShouldBe(2);
        loaded.Forecast.Periods[0].Temperature.ShouldBe(90);
        loaded.Forecast.Periods[0].ShortForecast.ShouldBe("Sunny");
        loaded.Forecast.Periods[1].ProbabilityOfPrecipitation.ShouldBeNull();
        loaded.IsFresh(Now).ShouldBeTrue();
    }

    [Test]
    public async Task Get_returns_null_for_a_missing_grid()
    {
        await using var harness = await SqliteCacheHarness.CreateAsync();
        var cache = new SqliteForecastCache(harness.Factory);

        (await cache.GetAsync(new GridPoint("OKX", 33, 35))).ShouldBeNull();
    }

    [Test]
    public async Task Expired_entry_is_returned_but_reports_not_fresh()
    {
        await using var harness = await SqliteCacheHarness.CreateAsync();
        var cache = new SqliteForecastCache(harness.Factory);

        var entry = new CachedForecast(BuildForecast(Now), "\"old\"", Now.AddHours(-2), Now.AddHours(-1));
        await cache.UpsertAsync(entry);

        var loaded = await cache.GetAsync(Grid);

        loaded.ShouldNotBeNull();
        loaded!.IsFresh(Now).ShouldBeFalse();
    }

    [Test]
    public async Task Upsert_overwrites_an_existing_entry_for_the_same_grid()
    {
        await using var harness = await SqliteCacheHarness.CreateAsync();
        var cache = new SqliteForecastCache(harness.Factory);

        await cache.UpsertAsync(new CachedForecast(
            BuildForecast(Now, firstShort: "Sunny", firstTemp: 90), "\"v1\"", Now, Now.AddHours(1)));
        await cache.UpsertAsync(new CachedForecast(
            BuildForecast(Now.AddHours(1), firstShort: "Cloudy", firstTemp: 70), "\"v2\"", Now.AddHours(1), Now.AddHours(2)));

        var loaded = await cache.GetAsync(Grid);

        loaded.ShouldNotBeNull();
        loaded!.ETag.ShouldBe("\"v2\"");
        loaded.Forecast.Periods[0].ShortForecast.ShouldBe("Cloudy");
        loaded.Forecast.Periods[0].Temperature.ShouldBe(70);
    }

    [Test]
    public async Task Payloads_written_by_earlier_versions_still_load()
    {
        await using var harness = await SqliteCacheHarness.CreateAsync();
        const string legacyPayload =
            """
            {"grid":{"gridId":"AKQ","gridX":83,"gridY":61},"generatedAt":"2026-06-17T18:00:00+00:00","updateTime":"2026-06-17T17:30:00+00:00","periods":[{"number":1,"name":"This Afternoon","startTime":"2026-06-17T18:00:00+00:00","endTime":"2026-06-17T22:00:00+00:00","isDaytime":true,"temperature":90,"temperatureUnit":"F","probabilityOfPrecipitation":3,"windSpeed":"5 to 12 mph","windDirection":"S","shortForecast":"Sunny","detailedForecast":"Detailed afternoon text.","icon":"icon-day"},{"number":2,"name":"Tonight","startTime":"2026-06-17T22:00:00+00:00","endTime":"2026-06-18T10:00:00+00:00","isDaytime":false,"temperature":74,"temperatureUnit":"F","windSpeed":"12 mph","windDirection":"S","shortForecast":"Mostly Clear","detailedForecast":"Detailed night text.","icon":"icon-night"}]}
            """;

        await using (var connection = await harness.Factory.CreateOpenConnectionAsync())
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO GridForecast (GridId, GridX, GridY, Payload, ETag, GeneratedAt, UpdateTime, RetrievedAtUtc, ExpiresAtUtc)
                VALUES ('AKQ', 83, 61, @Payload, '"legacy"', '2026-06-17T18:00:00.0000000+00:00', '2026-06-17T17:30:00.0000000+00:00',
                        '2026-06-17T18:00:00.0000000+00:00', '2026-06-17T19:00:00.0000000+00:00');
                """,
                new { Payload = legacyPayload });
        }

        var loaded = await new SqliteForecastCache(harness.Factory).GetAsync(Grid);

        loaded.ShouldNotBeNull();
        loaded!.ETag.ShouldBe("\"legacy\"");
        loaded.ExpiresAtUtc.ShouldBe(Now.AddHours(1));
        loaded.Forecast.Grid.ShouldBe(Grid);
        loaded.Forecast.Periods.Count.ShouldBe(2);
        loaded.Forecast.Periods[0].ProbabilityOfPrecipitation.ShouldBe(3);
        loaded.Forecast.Periods[1].ProbabilityOfPrecipitation.ShouldBeNull();
        loaded.Forecast.Periods[1].ShortForecast.ShouldBe("Mostly Clear");
    }

    [Test]
    public async Task Payload_format_matches_earlier_versions()
    {
        await using var harness = await SqliteCacheHarness.CreateAsync();
        await new SqliteForecastCache(harness.Factory).UpsertAsync(new CachedForecast(BuildForecast(Now), "\"e\"", Now, Now.AddHours(1)));

        await using var connection = await harness.Factory.CreateOpenConnectionAsync();
        var payload = await connection.ExecuteScalarAsync<string>("SELECT Payload FROM GridForecast;");

        payload.ShouldNotBeNull();
        payload.ShouldStartWith("""{"grid":{"gridId":"AKQ","gridX":83,"gridY":61},"generatedAt":""");
        payload.ShouldContain("\"shortForecast\":\"Mostly Clear\"");
        payload.Split("probabilityOfPrecipitation").Length.ShouldBe(2);
    }
}
