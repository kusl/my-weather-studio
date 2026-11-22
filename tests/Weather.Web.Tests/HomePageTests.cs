using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Weather.Web.Components.Pages;
using Weather.Web.Tests.Fakes;

namespace Weather.Web.Tests;

public sealed class HomePageTests
{
    private static ForecastDto SampleForecast() => new(
        "AKQ", 83, 61, DateTimeOffset.UtcNow, DateTimeOffset.UtcNow,
        [
            new ForecastPeriodDto(1, "This Afternoon", DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddHours(4),
                true, 90, "F", 3, "5 mph", "S", "Sunny", "Sunny, with a high near 90.", "icon-day"),
            new ForecastPeriodDto(2, "Tonight", DateTimeOffset.UtcNow.AddHours(4), DateTimeOffset.UtcNow.AddHours(16),
                false, 74, "F", null, "12 mph", "S", "Mostly Clear", "Mostly clear.", "icon-night"),
        ]);

    private static GeolocationResult Success() =>
        new(true, 37.0879, -76.4505, 12, GeolocationError.None);

    private static BunitContext CreateContext(IGeolocationService geo, IWeatherApiClient api)
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(geo);
        ctx.Services.AddSingleton(api);
        return ctx;
    }

    [Test]
    public void When_permission_denied_the_manual_entry_form_is_shown()
    {
        using var ctx = CreateContext(
            FakeGeolocationService.Returning(GeolocationResult.Failed(GeolocationError.PermissionDenied)),
            FakeWeatherApiClient.Returning(SampleForecast()));

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid=state-denied]"));
        cut.FindAll("[data-testid=manual-entry]").ShouldNotBeEmpty();
    }

    [Test]
    public void When_position_unavailable_the_manual_entry_form_is_shown()
    {
        using var ctx = CreateContext(
            FakeGeolocationService.Returning(GeolocationResult.Failed(GeolocationError.PositionUnavailable)),
            FakeWeatherApiClient.Returning(SampleForecast()));

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid=state-unavailable]"));
    }

    [Test]
    public void When_location_and_api_succeed_the_forecast_is_rendered()
    {
        using var ctx = CreateContext(
            FakeGeolocationService.Returning(Success()),
            FakeWeatherApiClient.Returning(SampleForecast()));

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid=forecast]"));
        cut.Find("[data-testid=hero-temp]").TextContent.ShouldContain("90");
        cut.Markup.ShouldContain("AKQ");
    }

    [Test]
    public void When_the_api_reports_no_coverage_the_not_covered_state_is_shown()
    {
        using var ctx = CreateContext(
            FakeGeolocationService.Returning(Success()),
            FakeWeatherApiClient.Returning(null));

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid=state-not-covered]"));
    }

    [Test]
    public void When_the_api_throws_the_error_state_is_shown()
    {
        using var ctx = CreateContext(
            FakeGeolocationService.Returning(Success()),
            FakeWeatherApiClient.Throwing(new HttpRequestException("boom")));

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid=state-error]"));
    }

    [Test]
    public void While_geolocation_is_pending_the_locating_state_is_shown_then_resolves()
    {
        var (geo, gate) = FakeGeolocationService.Gated();
        using var ctx = CreateContext(geo, FakeWeatherApiClient.Returning(SampleForecast()));

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid=state-locating]"));

        gate.SetResult(Success());
        cut.WaitForAssertion(() => cut.Find("[data-testid=forecast]"));
    }

    [Test]
    public void Manual_submission_loads_the_forecast()
    {
        using var ctx = CreateContext(
            FakeGeolocationService.Returning(GeolocationResult.Failed(GeolocationError.PermissionDenied)),
            FakeWeatherApiClient.Returning(SampleForecast()));

        var cut = ctx.Render<Home>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=manual-entry]"));

        cut.FindAll("[data-testid=manual-entry] input")[0].Input("37.0879");
        cut.FindAll("[data-testid=manual-entry] input")[1].Input("-76.4505");
        cut.FindAll("[data-testid=manual-entry] button")[0].Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid=forecast]"));
    }

    [Test]
    public void Use_sample_button_requests_the_sample_coordinates()
    {
        var api = FakeWeatherApiClient.Returning(SampleForecast());
        using var ctx = CreateContext(
            FakeGeolocationService.Returning(GeolocationResult.Failed(GeolocationError.PermissionDenied)),
            api);

        var cut = ctx.Render<Home>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=manual-entry]"));

        cut.FindAll("[data-testid=manual-entry] button")[1].Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid=forecast]"));
        api.LastLatitude.ShouldBe(37.0879);
        api.LastLongitude.ShouldBe(-76.4505);
    }

    [Test]
    public void When_the_page_is_not_a_secure_context_the_insecure_state_is_shown()
    {
        using var ctx = CreateContext(
            FakeGeolocationService.Returning(GeolocationResult.Failed(GeolocationError.InsecureContext)),
            FakeWeatherApiClient.Returning(SampleForecast()));

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid=state-insecure]"));
        cut.FindAll("[data-testid=manual-entry]").ShouldNotBeEmpty();
    }

    [Test]
    public void Upcoming_periods_are_rendered_as_cards()
    {
        using var ctx = CreateContext(
            FakeGeolocationService.Returning(Success()),
            FakeWeatherApiClient.Returning(SampleForecast()));

        var cut = ctx.Render<Home>();

        cut.WaitForAssertion(() => cut.Find("[data-testid=upcoming]"));
        cut.FindAll("[data-testid=upcoming] .card").Count.ShouldBe(1);
        cut.Find("[data-testid=upcoming] .card").TextContent.ShouldContain("Tonight");
    }

    [Test]
    public void Invalid_manual_input_shows_a_validation_message()
    {
        using var ctx = CreateContext(
            FakeGeolocationService.Returning(GeolocationResult.Failed(GeolocationError.PermissionDenied)),
            FakeWeatherApiClient.Returning(SampleForecast()));

        var cut = ctx.Render<Home>();
        cut.WaitForAssertion(() => cut.Find("[data-testid=manual-entry]"));

        cut.FindAll("[data-testid=manual-entry] input")[0].Input("north");
        cut.FindAll("[data-testid=manual-entry] input")[1].Input("-76.4505");
        cut.FindAll("[data-testid=manual-entry] button")[0].Click();

        cut.WaitForAssertion(() => cut.Find("[role=alert]").TextContent.ShouldContain("Enter numbers"));
    }
}
