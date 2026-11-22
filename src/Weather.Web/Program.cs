using Weather.ServiceDefaults;
using Weather.Web.Components;
using Weather.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddScoped<IGeolocationService, GeolocationService>();

var apiBaseUrl = new Uri(builder.Configuration["WeatherApi:BaseUrl"] ?? "https+http://api");
builder.Services.AddHttpClient<IWeatherApiClient, WeatherApiClient>(client => client.BaseAddress = apiBaseUrl)
    .AddStandardResilienceHandler();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapDefaultEndpoints();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
