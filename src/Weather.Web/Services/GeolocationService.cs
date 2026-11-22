using Microsoft.JSInterop;

namespace Weather.Web.Services;

internal sealed class GeolocationService(IJSRuntime jsRuntime) : IGeolocationService, IAsyncDisposable
{
    private const string ModulePath = "./js/geolocation.js";

    private IJSObjectReference? _module;

    public async ValueTask<GeolocationResult> GetCurrentPositionAsync(CancellationToken cancellationToken = default)
    {
        GeoJsResult? raw;
        try
        {
            _module ??= await jsRuntime.InvokeAsync<IJSObjectReference>("import", cancellationToken, ModulePath);
            raw = await _module.InvokeAsync<GeoJsResult?>("getCurrentPosition", cancellationToken);
        }
        catch (Exception ex) when (ex is JSException or JSDisconnectedException or OperationCanceledException)
        {
            return GeolocationResult.Failed(GeolocationError.Unknown);
        }

        if (raw is { Success: true, Latitude: { } latitude, Longitude: { } longitude })
        {
            return new GeolocationResult(true, latitude, longitude, raw.Accuracy, GeolocationError.None);
        }

        return GeolocationResult.Failed(raw?.ErrorCode switch
        {
            "PermissionDenied" => GeolocationError.PermissionDenied,
            "PositionUnavailable" => GeolocationError.PositionUnavailable,
            "Timeout" => GeolocationError.Timeout,
            "NotSupported" => GeolocationError.NotSupported,
            "InsecureContext" => GeolocationError.InsecureContext,
            _ => GeolocationError.Unknown,
        });
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            return;
        }

        try
        {
            await _module.DisposeAsync();
        }
        catch (JSDisconnectedException)
        {
        }
    }

    private sealed record GeoJsResult(bool Success, double? Latitude, double? Longitude, double? Accuracy, string? ErrorCode);
}
