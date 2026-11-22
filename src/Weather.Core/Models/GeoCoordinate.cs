using System.Globalization;

namespace Weather.Core.Models;

public readonly record struct GeoCoordinate
{
    public GeoCoordinate(double latitude, double longitude)
    {
        if (!IsValidLatitude(latitude))
        {
            throw new ArgumentOutOfRangeException(nameof(latitude), latitude, "Latitude must be between -90 and 90 degrees.");
        }

        if (!IsValidLongitude(longitude))
        {
            throw new ArgumentOutOfRangeException(nameof(longitude), longitude, "Longitude must be between -180 and 180 degrees.");
        }

        Latitude = latitude;
        Longitude = longitude;
    }

    public double Latitude { get; }

    public double Longitude { get; }

    public static bool IsValid(double latitude, double longitude) =>
        IsValidLatitude(latitude) && IsValidLongitude(longitude);

    public GeoCoordinate Rounded(int decimals = 4) =>
        new(Math.Round(Latitude, decimals, MidpointRounding.AwayFromZero),
            Math.Round(Longitude, decimals, MidpointRounding.AwayFromZero));

    public string ToApiString() =>
        string.Create(CultureInfo.InvariantCulture, $"{Latitude:0.####},{Longitude:0.####}");

    public string ToCacheKey() => ToApiString();

    public override string ToString() => ToApiString();

    private static bool IsValidLatitude(double value) => value is >= -90 and <= 90;

    private static bool IsValidLongitude(double value) => value is >= -180 and <= 180;
}
