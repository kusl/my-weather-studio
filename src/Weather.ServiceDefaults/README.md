# Weather.ServiceDefaults

The shared **Aspire service defaults**. Every executable service in the
solution (the API and the Blazor app) calls `AddServiceDefaults()` to get
consistent observability, health and service discovery without
repeating the wiring. This project is deliberately **app-agnostic** — it knows
nothing about forecasts or caches.

## What `AddServiceDefaults()` configures

- **OpenTelemetry** (`ConfigureOpenTelemetry`):
  - **Logging** with formatted messages and scopes included.
  - **Metrics**: ASP.NET Core and `HttpClient` instrumentation, plus the
    built-in `System.Runtime` meter (no separate runtime instrumentation package
    is needed on .NET 9 and later).
  - **Tracing**: the application's own activity source plus ASP.NET Core and
    `HttpClient` instrumentation.
  - **OTLP export**: enabled automatically when `OTEL_EXPORTER_OTLP_ENDPOINT` is
    set (which Aspire does for you when running under the AppHost).
- **Health checks** (`AddDefaultHealthChecks`): a default "self" liveness check.
- **Service discovery**: logical service names (e.g. `https+http://api`) resolve
  to real addresses.
- **HttpClient defaults** (`ConfigureHttpClientDefaults`): every outbound
  `HttpClient` gets service-discovery resolution.

Resilience is **not** added globally. A global standard resilience handler plus
a per-client one (which the NWS client needs for its own timeouts) stacks two
pipelines on the same client, multiplying retries and timeouts. Instead each
typed client adds exactly one `AddStandardResilienceHandler()`: the NWS client in
`Weather.Infrastructure`, the API client in `Weather.Web`.

## What `MapDefaultEndpoints()` adds

Health endpoints, intended for non-production by default:

- `GET /health` — readiness: all health checks must pass.
- `GET /alive` — liveness: only checks tagged `live`.

## Why app-specific telemetry lives elsewhere

The app's own meter (`Weather.Cache`) and trace source (`Weather.Nws`) are
defined in [`Weather.Core`](../Weather.Core/README.md) and **registered with
OpenTelemetry in the API host** (`Weather.Api/Program.cs`), not here. Keeping
this project free of app specifics means it could be lifted into any service
unchanged — which is the whole point of the Aspire "service defaults" pattern.

## Usage

```csharp
var builder = WebApplication.CreateBuilder(args);
builder.AddServiceDefaults();
// ... app-specific registrations ...
var app = builder.Build();
app.MapDefaultEndpoints();
```

Both [`Weather.Api`](../Weather.Api/README.md) and
[`Weather.Web`](../Weather.Web/README.md) do exactly this; the
[AppHost](../Weather.AppHost/README.md) then surfaces the resulting telemetry in
the Aspire dashboard.
