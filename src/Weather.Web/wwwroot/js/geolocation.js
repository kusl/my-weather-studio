const errorCodes = new Map([
  [1, "PermissionDenied"],
  [2, "PositionUnavailable"],
  [3, "Timeout"],
]);

const failure = (errorCode) => ({ success: false, latitude: null, longitude: null, accuracy: null, errorCode });

export function getCurrentPosition() {
  if (!window.isSecureContext) {
    return Promise.resolve(failure("InsecureContext"));
  }

  if (!("geolocation" in navigator)) {
    return Promise.resolve(failure("NotSupported"));
  }

  const { promise, resolve } = Promise.withResolvers();
  navigator.geolocation.getCurrentPosition(
    ({ coords }) => resolve({
      success: true,
      latitude: coords.latitude,
      longitude: coords.longitude,
      accuracy: coords.accuracy,
      errorCode: null,
    }),
    (error) => resolve(failure(errorCodes.get(error?.code) ?? "PositionUnavailable")),
    { enableHighAccuracy: false, timeout: 10_000, maximumAge: 300_000 },
  );

  return promise;
}
