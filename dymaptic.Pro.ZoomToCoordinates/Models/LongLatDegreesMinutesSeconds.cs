using ArcGIS.Core.Geometry;

namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class LongLatDegreesMinutesSeconds : LongLatItem
{
    // Degrees/Minutes/Seconds without the degree, minute, second symbols or N/S/E/W labels
    public string LongitudeDMS { get; set; } = "";
    public string LatitudeDMS { get; set; } = "";

    public string LongitudeDMSFormatted { get; set; } = "";
    public string LatitudeDMSFormatted { get; set; } = "";

    public string DegreesMinutesSeconds => $"{LatitudeDMS} {LongitudeDMS}";
    public string DegreesMinutesSecondsFormatted => $"{LatitudeDMSFormatted} {LongitudeDMSFormatted}";

    /// <summary>
    ///     Update the LongLatItem's mapPoint using the one from the ViewModel.
    /// </summary>
    /// <param name="mapPoint">The mapPoint from the ViewModel.</param>
    public override void Update(MapPoint mapPoint)
    {
        MapPoint = MapPointBuilderEx.CreateMapPoint(mapPoint.X, mapPoint.Y, SpatialReferences.WGS84);
        FormatAsDegreesMinutesSeconds();
    }

    /// <summary>
    ///     Update the LongLatItem's mapPoint using lat/long.
    /// </summary>
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    public override void Update(double longitude, double latitude)
    {
        MapPoint = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);
        FormatAsDegreesMinutesSeconds();
    }


    /// <summary>
    ///     Formats degrees as Degrees Minutes Seconds with and without symbols (e.g., 37° 29' 2.08" N  121° 42' 57.95" W)
    /// </summary>
    private void FormatAsDegreesMinutesSeconds()
    {
        string _dmsGeoCoordinateString = MapPoint.ToGeoCoordinateString(_degreesMinutesSecondsParam);
        string[] parts = _dmsGeoCoordinateString.Split(' ');

        char latitudeLabel = parts[2][^1];
        char longitudeLabel = parts[5][^1];

        // Remove leading zeros from each numeric part
        string latDegrees = parts[0].TrimStart('0');
        string latMinutes = parts[1].TrimStart('0');
        string latSeconds = parts[2][..^1].TrimStart('0');

        string longDegrees = parts[3].TrimStart('0');
        string longMinutes = parts[4].TrimStart('0');
        string longSeconds = parts[5][..^1].TrimStart('0');

        LatitudeDMS = $"{latDegrees} {latMinutes} {latSeconds} {latitudeLabel}";
        LongitudeDMS = $"{longDegrees} {longMinutes} {longSeconds} {longitudeLabel}";

        LatitudeDMSFormatted = $"{latDegrees}° {latMinutes}' {latSeconds}'' {latitudeLabel}";
        LongitudeDMSFormatted = $"{longDegrees}° {longMinutes}' {longSeconds}'' {longitudeLabel}";
    }

    private readonly ToGeoCoordinateParameter _degreesMinutesSecondsParam = new(geoCoordType: GeoCoordinateType.DMS);
}
