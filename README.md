# Weather

A weather dashboard built on the U.S. National Weather Service (NWS) API,
written in **.NET 11 / C# 15**. It pairs a **Blazor** front end (interactive
server rendering) with an **ASP.NET Core Minimal API** backend, caches forecasts
in **SQLite** (via **Dapper**), is wired for observability with
**OpenTelemetry**, and is orchestrated with **Aspire**.

Repository: <https://github.com/collabskus/weather.git>

## What it does

1. The browser is asked for the user's location.
2. That coordinate is resolved to an NWS **forecast grid cell** and the forecast
   for that cell is shown — a "sky-state" hero that paints the sky implied by the
   current conditions, plus cards for the upcoming periods.
3. If the user declines to share their location, the browser can't provide it,
   or the page was opened over plain HTTP (where browsers refuse geolocation),
   the dashboard falls back to manual latitude/longitude entry with a one-click
   sample location.

Forecasts are cached for a short window and reused, so the dashboard stays quick
and the weather service isn't polled more than necessary.

## Design at a glance

The full reasoning is in [`docs/ARCHITECTURE.md`](docs/ARCHITECTURE.md).

- **Caching** — two SQLite tiers: long-lived coordinate→grid metadata (30 days,
  keyed by a 4-dp coordinate so nearby users share a row) and short-lived grid
  forecasts (honouring NWS `Cache-Control`, clamped to 6 h). Conditional GETs
  refresh expiry cheaply; if NWS is unreachable, the last good forecast is
  served rather than an error.
- **Neighbourhood warming** — the user's own cell is fetched synchronously; the
  surrounding 3×3 ring is warmed in the background via a bounded channel and a
  rate-limited `BackgroundService`.
- **Observability** — Aspire `ServiceDefaults` set up OpenTelemetry, health
  checks and service discovery; a custom meter and trace source record cache
  hit-rate and NWS latency, throttling and errors.

## Browser support

Only the **latest versions of evergreen browsers** are supported: Chrome, Edge,
Firefox, Safari and Samsung Internet.

The one exception is the first page load. It carries a small legacy-safe gate
script that checks for every feature the app relies on before starting Blazor.
On an older browser it shows a full-page explanation in plain language, what to
do next, the list of missing features, and a block of diagnostics (browser,
page, viewport, secure context, …) with a **Copy details** button so the problem
is easy to share. The same page appears if the app script fails to download or
start. With JavaScript disabled, a `<noscript>` message explains what to do.

## Geolocation fallback behaviour

| Situation | What the user sees |
| --- | --- |
| Location shared | The forecast for their cell |
| Permission denied | "Location is off" + manual entry + sample location |
| Page opened over plain HTTP (e.g. a phone on your LAN) | "Location needs a secure connection" + manual entry + sample |
| Position unavailable / timeout / unsupported | "Couldn't pin down where you are" + manual entry + sample |
| Coordinate outside NWS coverage | "No coverage for that spot" + manual entry + sample |
| API/network error | "The forecast didn't load" + retry + sample |

## Prerequisites

- **.NET 11 SDK**. [`global.json`](global.json) accepts `11.0.100-rc.3` or any
  newer 11.0 SDK (including GA and later feature bands), so the RC 3 SDK from
  Fedora's repositories works locally while CI uses the latest stable 11.0 SDK.
- For the orchestrated experience, the **Aspire CLI**
  (`curl -sSL https://aspire.dev/install.sh | bash`). Aspire 17 C# AppHosts get
  the dashboard and orchestrator from the CLI bundle. Building without the CLI
  still works: the SDK's `dnx` fetches the bundle on first build.
- No database to install — SQLite is file-based and created on first run.

## Run it

### On your laptop, reachable from your phone

Both services listen on **all interfaces** (`0.0.0.0`) by default, so other
devices on the same Wi-Fi can reach them by your laptop's IP address.

```bash
# Terminal 1 — the API on port 5180
dotnet run --project src/Weather.Api

# Terminal 2 — the dashboard on port 5080
dotnet run --project src/Weather.Web
```

Find your laptop's address (`hostname -I` on Linux) and open
`http://<laptop-ip>:5080` on the phone. On Fedora, open the port once:

```bash
sudo firewall-cmd --add-port=5080/tcp
```

Under `Development` the dashboard finds the API at `http://localhost:5180`
through service-discovery configuration in
`src/Weather.Web/appsettings.Development.json`. To point it elsewhere, set
`Services__api__http__0` or `WeatherApi__BaseUrl`.

Browsers only allow geolocation on HTTPS or `localhost`, so from a phone over
plain HTTP the dashboard explains this and offers manual entry. An old phone
whose browser can no longer update sees the browser gate page instead.

### With Aspire

```bash
cd src/Weather.AppHost && aspire run
# or, from the repository root
dotnet run --project src/Weather.AppHost
```

This starts the API and the dashboard together and prints the Aspire dashboard
URL for logs, traces and metrics. Aspire proxies service endpoints on
`localhost`; use the standalone commands above for LAN access.

## Build and test

```bash
dotnet build weather.slnx
dotnet test --solution weather.slnx
dotnet format weather.slnx --verify-no-changes --severity warn
```

`dotnet test` runs in **Microsoft.Testing.Platform** mode (configured in
`global.json`), which is why the solution is passed with `--solution`.

The solution uses **`weather.slnx`**, **Central Package Management**
([`Directory.Packages.props`](Directory.Packages.props)) and shared build
settings ([`Directory.Build.props`](Directory.Build.props)). Warnings are not
treated as errors by default; set `TreatWarningsAsErrors` to `true` or build
with `-warnaserror` to tighten that up.

## Tests

Four projects using **TUnit**, **bUnit 2**, **Shouldly** and **NSubstitute**:

- **Weather.Core.Tests** — coordinate validation/rounding, grid geometry, the
  closed `ForecastFetchResult` hierarchy, telemetry instruments.
- **Weather.Infrastructure.Tests** — the NWS client against a stub handler; the
  SQLite caches against a real temp database, including compatibility with cache
  payloads written by earlier versions; the `WeatherService` cache-aside policy;
  the background warmer.
- **Weather.Api.Tests** — the endpoints end-to-end via `WebApplicationFactory`
  with a fake NWS source and a real SQLite cache (200 / 400 / 404).
- **Weather.Web.Tests** — the dashboard's state machine and every geolocation
  fallback rendered with bUnit, plus the sky-state palette.

## Project layout

```text
├─ src/
│  ├─ Weather.Core/             # domain models, abstractions, telemetry
│  ├─ Weather.Infrastructure/   # NWS client, SQLite caches, services, warmer
│  ├─ Weather.ServiceDefaults/  # Aspire: OTel, health, service discovery
│  ├─ Weather.Api/              # ASP.NET Core Minimal API
│  ├─ Weather.Web/              # Blazor dashboard + browser gate
│  └─ Weather.AppHost/          # Aspire orchestrator
├─ tests/
│  ├─ Weather.Core.Tests/
│  ├─ Weather.Infrastructure.Tests/
│  ├─ Weather.Api.Tests/
│  └─ Weather.Web.Tests/
├─ docs/ARCHITECTURE.md
├─ .github/                     # CI workflows + Dependabot
├─ Directory.Build.props
├─ Directory.Packages.props
├─ global.json
└─ weather.slnx
```

Each project has its own README with the detail for that layer.

## Your data

The SQLite cache (`weather-cache.db`, next to the API's working directory) keeps
its schema, keys and payload format, so an existing cache is reused as-is after
upgrading. Nothing is deleted or rebuilt.

## A note on the NWS API

The NWS API **requires a descriptive `User-Agent`** that identifies your app and
a contact. Override the default in configuration:

```jsonc
{
  "Nws": { "UserAgent": "your-app/1.0 (you@example.com)" }
}
```

See the [NWS API documentation](https://www.weather.gov/documentation/services-web-api).

## CI

The workflows under [`.github/workflows`](.github/workflows) cover build, test,
`dotnet format` verification, CodeQL (C#, JavaScript and workflow files),
dependency review, and Markdown linting. CI installs the latest stable .NET 11
SDK; Dependabot keeps packages and actions current.

## Licence

AGPLv3
