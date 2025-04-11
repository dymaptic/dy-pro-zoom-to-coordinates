using ArcGIS.Core.Geometry;

namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class LongLatDecimalDegrees : LongLatItem
{
    public string LongitudeDDFormatted { get; set; } = "";
    public string LatitudeDDFormatted { get; set; } = "";

    public string DecimalDegrees => $"{Latitude}, {Longitude}";
    public string DecimalDegreesFormatted => $"{LatitudeDDFormatted} {LongitudeDDFormatted}";

    /// <summary>
    ///     Update the LongLatItem's mapPoint using the one from the ViewModel.
    /// </summary>
    /// <param name="mapPoint">The mapPoint from the ViewModel.</param>
    public override void Update(MapPoint mapPoint)
    {
        MapPoint = MapPointBuilderEx.CreateMapPoint(mapPoint.X, mapPoint.Y, SpatialReferences.WGS84);

        FormatAsDecimalDegrees();
    }

    /// <summary>
    ///     Update the LongLatItem's mapPoint using lat/long.
    /// </summary>
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    public override void Update(double longitude, double latitude)
    {
        MapPoint = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);

        FormatAsDecimalDegrees();
    }

    /// <summary>
    ///     Format degrees as decimal degrees.
    /// </summary>
    private void FormatAsDecimalDegrees()
    {
        string _ddGeoCoordinateString = MapPoint.ToGeoCoordinateString(_decimalDegreesParam);
        string[] parts = _ddGeoCoordinateString.Split(' ');
        string latitude = parts[0];
        char latitudeLabel = latitude[^1];
        latitude = latitude[..(latitude.Length - 1)];

        // Remove leading zeros
        latitude = latitude.TrimStart('0');

        // Formatted Latitude WON'T include a negative value if S is in Southern Hemisphere
        LatitudeDDFormatted = latitudeLabel == 'S' ? $"{latitude:F6}° S" : $"{latitude:F6}° N";

        // This WILL include negative value if necessary
        Latitude = latitudeLabel == 'S' ? -1 * double.Parse(latitude) : double.Parse(latitude);

        string longitude = parts[1];
        char longitudeLabel = longitude[^1];
        longitude = longitude[..(longitude.Length - 1)];

        // Remove leading zeros
        longitude = longitude.TrimStart('0');

        LongitudeDDFormatted = longitudeLabel == 'W' ? $"{longitude:F6}° W" : $"{longitude:F6}° E";
        Longitude = longitudeLabel == 'W' ? -1 * double.Parse(longitude) : double.Parse(longitude);
    }

    private readonly ToGeoCoordinateParameter _decimalDegreesParam = new(geoCoordType: GeoCoordinateType.DD);
}
