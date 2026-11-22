# Weather.AppHost

The [Aspire](https://aspire.dev/) orchestrator. It launches the API and the
Blazor dashboard together, wires service discovery between them, and opens the
Aspire dashboard for logs, traces and metrics.

## Run it

Install the Aspire CLI once (`curl -sSL https://aspire.dev/install.sh | bash`),
then either:

```bash
cd src/Weather.AppHost && aspire run
```

or, from the repository root:

```bash
dotnet run --project src/Weather.AppHost
```

That starts:

- **api** — the ASP.NET Core Weather API.
- **web** — the Blazor dashboard, with a reference to `api`.

## How the wiring works

```csharp
var api = builder.AddDotnetProject("api", "../Weather.Api/Weather.Api.csproj");

builder.AddDotnetProject("web", "../Weather.Web/Weather.Web.csproj")
    .WithReference(api)
    .WaitFor(api)
    .WithExternalHttpEndpoints();
```

- This is the Aspire 17 project model. The AppHost uses plain
  `Microsoft.NET.Sdk` with explicit `Aspire.Hosting.AppHost` and
  `Aspire.Hosting.Dotnet` packages (the old `Aspire.AppHost.Sdk` is obsolete,
  `ASPIRE012`), and services are added by path (relative to this directory), so
  there are no `ProjectReference`s and no generated `Projects.*` types.
- `AddDotnetProject` builds each service itself when it starts.
- `.WithReference(api)` injects `services__api__http__0` into the web app, which
  its `https+http://api` client resolves through service discovery. The value
  overrides the standalone default in the web app's
  `appsettings.Development.json`.
- `.WaitFor(api)` holds the web app until the API is healthy.

The dashboard and orchestrator (DCP) come from the Aspire CLI bundle. When the
CLI isn't installed, the build obtains the bundle through the SDK's `dnx`.

## Profiles

`Properties/launchSettings.json` has a single `http` profile with the dashboard
on `http://localhost:15080` and `ASPIRE_ALLOW_UNSECURED_TRANSPORT=true`, so no
developer certificate is needed. Aspire proxies service endpoints on
`localhost`; for access from other devices on the network, run the API and web
projects directly (see the [root README](../../README.md#run-it)).

## Observability

Both services call `AddServiceDefaults()` from
[`Weather.ServiceDefaults`](../Weather.ServiceDefaults/README.md), which exports
OpenTelemetry over OTLP. Under this host, that telemetry flows into the Aspire
dashboard with no extra configuration.
