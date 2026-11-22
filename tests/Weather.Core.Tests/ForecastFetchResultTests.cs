namespace Weather.Core.Tests;

public sealed class ForecastFetchResultTests
{
    private static readonly Forecast Sample = new(new GridPoint("AKQ", 83, 61), DateTimeOffset.UtcNow, DateTimeOffset.UtcNow, []);

    private static string Describe(ForecastFetchResult result) => result switch
    {
        ForecastFetchResult.Fetched => "fetched",
        ForecastFetchResult.NotModified => "not-modified",
        ForecastFetchResult.NotFound => "not-found",
        ForecastFetchResult.Unavailable => "unavailable",
    };

    [Test]
    public void Fetched_carries_forecast_etag_and_max_age()
    {
        var result = new ForecastFetchResult.Fetched(Sample, "\"abc\"", TimeSpan.FromMinutes(30));

        result.Forecast.ShouldBe(Sample);
        result.ETag.ShouldBe("\"abc\"");
        result.MaxAge.ShouldBe(TimeSpan.FromMinutes(30));
    }

    [Test]
    public void NotModified_carries_etag_and_max_age()
    {
        var result = new ForecastFetchResult.NotModified("\"abc\"", TimeSpan.FromMinutes(10));

        result.ETag.ShouldBe("\"abc\"");
        result.MaxAge.ShouldBe(TimeSpan.FromMinutes(10));
    }

    [Test]
    public void Every_outcome_is_matched_exhaustively()
    {
        Describe(new ForecastFetchResult.Fetched(Sample, null, null)).ShouldBe("fetched");
        Describe(new ForecastFetchResult.NotModified(null, null)).ShouldBe("not-modified");
        Describe(new ForecastFetchResult.NotFound()).ShouldBe("not-found");
        Describe(new ForecastFetchResult.Unavailable()).ShouldBe("unavailable");
    }

    [Test]
    public void Terminal_outcomes_have_value_equality() =>
        new ForecastFetchResult.NotFound().ShouldBe(new ForecastFetchResult.NotFound());
}

public sealed class GridPointTests
{
    [Test]
    public void ToString_is_office_slash_x_comma_y() =>
        new GridPoint("AKQ", 83, 61).ToString().ShouldBe("AKQ/83,61");

    [Test]
    public void Records_with_the_same_values_are_equal() =>
        new GridPoint("AKQ", 83, 61).ShouldBe(new GridPoint("AKQ", 83, 61));

    [Test]
    public void Records_with_different_values_are_not_equal() =>
        new GridPoint("AKQ", 83, 61).ShouldNotBe(new GridPoint("AKQ", 83, 62));

    [Test]
    public void With_expression_replaces_a_single_index() =>
        (new GridPoint("AKQ", 83, 61) with { GridX = 84 }).ShouldBe(new GridPoint("AKQ", 84, 61));
}
