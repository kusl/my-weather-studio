namespace Weather.Web.Services;

public enum GeolocationError
{
    None,
    PermissionDenied,
    PositionUnavailable,
    Timeout,
    NotSupported,
    InsecureContext,
    Unknown,
}

public sealed record GeolocationResult(bool Success, double Latitude, double Longitude, double? Accuracy, GeolocationError Error)
{
    public static GeolocationResult Failed(GeolocationError error) => new(false, 0, 0, null, error);
}

public interface IGeolocationService
{
    ValueTask<GeolocationResult> GetCurrentPositionAsync(CancellationToken cancellationToken = default);
}
