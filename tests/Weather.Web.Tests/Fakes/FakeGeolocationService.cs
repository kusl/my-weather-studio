namespace Weather.Web.Tests.Fakes;

internal sealed class FakeGeolocationService : IGeolocationService
{
    private readonly Func<ValueTask<GeolocationResult>> _respond;

    private FakeGeolocationService(Func<ValueTask<GeolocationResult>> respond) => _respond = respond;

    public static FakeGeolocationService Returning(GeolocationResult result) => new(() => ValueTask.FromResult(result));

    public static (FakeGeolocationService Service, TaskCompletionSource<GeolocationResult> Gate) Gated()
    {
        var gate = new TaskCompletionSource<GeolocationResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        return (new FakeGeolocationService(() => new ValueTask<GeolocationResult>(gate.Task)), gate);
    }

    public ValueTask<GeolocationResult> GetCurrentPositionAsync(CancellationToken cancellationToken = default) => _respond();
}
