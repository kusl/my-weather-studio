using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Weather.Api.Endpoints;
using Weather.Core.Telemetry;
using Weather.Infrastructure;
using Weather.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddWeatherInfrastructure(builder.Configuration);
builder.Services.ConfigureOpenTelemetryMeterProvider(static metrics => metrics.AddMeter(WeatherTelemetry.MeterName));
builder.Services.ConfigureOpenTelemetryTracerProvider(static tracing => tracing.AddSource(WeatherTelemetry.ActivitySourceName));

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapDefaultEndpoints();
app.MapWeatherEndpoints();

app.Run();
