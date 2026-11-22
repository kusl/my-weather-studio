# Weather.Web

The Blazor front end (interactive server rendering): a single-page **sky-state
dashboard** that reads the user's location from the browser, asks the
[Weather API](../Weather.Api/README.md) for a forecast, and renders it. Its
defining job is to handle the *unhappy* paths as gracefully as the happy one —
including browsers that are too old to run it at all.

## What it does

1. The first HTML response runs the **browser gate** (below). On a current
   browser it starts Blazor.
2. On first interactive render the dashboard asks the browser for the current
   position (`navigator.geolocation`).
3. On success it calls `GET /api/forecast?latitude={lat}&longitude={lon}` and
   renders the result.
4. On **any** failure it falls back to a manual latitude/longitude form plus a
   one-click sample location (Newport News, VA).

## Browser support and the gate

Only the latest evergreen browsers are supported. `wwwroot/js/gate.js` is the
one piece of code written for old browsers: ES3 syntax, no dependencies, styled
by the equally conservative `wwwroot/css/gate.css`. `App.razor` loads it instead
of `blazor.web.js`; it carries the fingerprinted Blazor URL in a `data-` attribute.

The gate checks exactly what the app relies on — modern JavaScript syntax
(optional chaining, class private fields, async iteration), modules,
`Promise.withResolvers`, `Object.groupBy`, `structuredClone`, `fetch`,
`WebSocket`, `<dialog>`, and the CSS features `app.css` uses (nesting, cascade
layers, `:has()`, container queries, `color-mix()`, `dvh` units). Then:

- **All present** — it injects `blazor.web.js` with `autostart="false"` and calls
  `Blazor.start()`.
- **Anything missing** — it reveals the `LegacyGate` component's markup: a
  full-page explanation in plain language, what to do next, the list of missing
  features, and a read-only diagnostics block (result, problems, page, time,
  user agent, platform, language, viewport, screen, secure context, cookies,
  online) with **Copy details** and **Reload** buttons. Copy uses the async
  clipboard where allowed and falls back to select-and-copy.
- **Blazor fails to download or start** — the same page with a "couldn't
  start" message and the error in the diagnostics.

A `<noscript>` block covers browsers with JavaScript disabled.

## The geolocation contract

[`wwwroot/js/geolocation.js`](wwwroot/js/geolocation.js) is an ES module that
`GeolocationService` imports on demand (no globals). `getCurrentPosition()`
**never rejects** — it always resolves an object:

```js
{ success, latitude, longitude, accuracy, errorCode }
// errorCode ∈ "PermissionDenied" | "PositionUnavailable" | "Timeout" | "NotSupported" | "InsecureContext" | null
```

Browsers only expose geolocation to secure contexts (HTTPS or `localhost`). The
module checks `window.isSecureContext` first so a phone opening
`http://192.168.x.x:5080` gets a clear explanation instead of a generic denial.

| Outcome | State shown | Fallback offered |
| --- | --- | --- |
| Position obtained | forecast loads | — |
| Permission denied | "Location is off" | manual entry + sample |
| Plain-HTTP page | "Location needs a secure connection" | manual entry + sample |
| Position unavailable / timeout / unsupported / JS fault | "Couldn't pin down where you are" | manual entry + sample |
| Coordinate outside NWS coverage (API 404) | "No coverage for that spot" | manual entry + sample |
| API/network error | "The forecast didn't load" | retry + sample |

Geolocation needs JS interop, so it runs in `OnAfterRenderAsync(firstRender:
true)` — never during prerender.

## Talking to the API

`IWeatherApiClient` / `WeatherApiClient` is a typed `HttpClient` with its own
standard resilience handler. Its base address is `https+http://api`
(`WeatherApi:BaseUrl`), resolved by service discovery:

- **Under Aspire** the AppHost injects `services__api__http__0`.
- **Standalone in Development** `appsettings.Development.json` supplies
  `Services:api:http` = `http://localhost:5180`, the API's default port.

A `404` from the API is mapped to `null` (an expected "no coverage" outcome),
not an exception.

## Running

`Properties/launchSettings.json` binds `http://0.0.0.0:5080`, so
`dotnet run --project src/Weather.Web` is reachable from other devices on the
same network at `http://<laptop-ip>:5080`.

## The signature element

The hero (`Components/Forecast/SkyHero.razor`) paints the sky implied by the
current period; the gradient comes from day/night and keywords in the short
forecast (`Services/SkyPalette.cs`). Upcoming periods are `PeriodCard`
components. Styles use native CSS nesting, cascade layers, container queries and
`color-mix()`; keyboard focus is visible and `prefers-reduced-motion` is
respected.

Circuit reconnection uses the .NET 10+ `ReconnectModal` pattern (a `<dialog>`
plus a collocated JS module), styled to match.

## Testability

The page depends only on `IGeolocationService` and `IWeatherApiClient`, so
[`Weather.Web.Tests`](../../tests/Weather.Web.Tests/) renders it with bUnit 2
using fakes — no browser, JS runtime or HTTP server required.

## Layout

```text
Weather.Web/
├─ Components/
│  ├─ App.razor              # document, styles, gate script
│  ├─ Routes.razor           # router with NotFoundPage
│  ├─ _Imports.razor
│  ├─ Forecast/
│  │  ├─ SkyHero.razor       # the hero panel
│  │  └─ PeriodCard.razor    # one upcoming period
│  ├─ Layout/
│  │  ├─ MainLayout.razor
│  │  ├─ NavMenu.razor
│  │  ├─ LegacyGate.razor    # markup revealed by gate.js
│  │  └─ ReconnectModal.razor (+ .razor.js)
│  └─ Pages/
│     ├─ Home.razor          # the dashboard + state machine
│     ├─ About.razor
│     ├─ Error.razor
│     └─ NotFound.razor
├─ Services/
│  ├─ IGeolocationService.cs / GeolocationService.cs
│  ├─ IWeatherApiClient.cs   / WeatherApiClient.cs
│  ├─ WeatherViewModels.cs
│  ├─ PeriodFormat.cs
│  └─ SkyPalette.cs
├─ wwwroot/
│  ├─ app.css
│  ├─ css/gate.css
│  └─ js/
│     ├─ gate.js             # legacy-safe browser gate
│     └─ geolocation.js      # never-rejecting geolocation module
└─ Program.cs
```
