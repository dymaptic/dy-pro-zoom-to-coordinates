using ArcGIS.Core.Geometry;

namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class LongLatDegreesDecimalMinutes : LongLatItem
{
    public string LongitudeDDM { get; set; } = "";
    public string LatitudeDDM { get; set; } = "";

    public string LongitudeDDMFormatted { get; set; } = "";
    public string LatitudeDDMFormatted { get; set; } = "";

    public string DegreesDecimalMinutes => $"{LatitudeDDM} {LongitudeDDM}";
    public string DegreesDecimalMinutesFormatted => $"{LatitudeDDMFormatted} {LongitudeDDMFormatted}";

    /// <summary>
    ///     Update the LongLatItem's mapPoint using the one from the ViewModel.
    /// </summary>
    /// <param name="mapPoint">The mapPoint from the ViewModel.</param>
    public override void Update(MapPoint mapPoint)
    {
        MapPoint = MapPointBuilderEx.CreateMapPoint(mapPoint.X, mapPoint.Y, SpatialReferences.WGS84);
        FormatAsDegreesDecimalMinutes();
    }

    /// <summary>
    ///     Update the LongLatItem's mapPoint using lat/long.
    /// </summary>
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    public override void Update(double longitude, double latitude)
    {
        MapPoint = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);
        FormatAsDegreesDecimalMinutes();
    }

    /// <summary>
    ///     Formats degrees as Degrees Decimal Minutes with and without symbols (e.g., 37° 29.1911' N  121° 42.8099' W)
    /// </summary>
    private void FormatAsDegreesDecimalMinutes()
    {
        string _ddmGeoCoordinateString = MapPoint.ToGeoCoordinateString(_degreesDecimalMinutesParam);
        string[] parts = _ddmGeoCoordinateString.Split(' ');

        char latitudeLabel = parts[1][^1];
        char longitudeLabel = parts[3][^1];

        // Remove leading zeros from numeric parts
        string latDegrees = parts[0].TrimStart('0');
        string latMinutes = parts[1][..^1].TrimStart('0');

        string longDegrees = parts[2].TrimStart('0');
        string longMinutes = parts[3][..^1].TrimStart('0');

        // Normalize empty minutes strings
        latMinutes = latMinutes == ".0000" ? "0" : latMinutes;
        longMinutes = longMinutes == ".0000" ? "0" : longMinutes;

        LatitudeDDM = $"{latDegrees} {latMinutes} {latitudeLabel}";
        LongitudeDDM = $"{longDegrees} {longMinutes} {longitudeLabel}";

        LatitudeDDMFormatted = $"{latDegrees}° {latMinutes}' {latitudeLabel}";
        LongitudeDDMFormatted = $"{longDegrees}° {longMinutes}' {longitudeLabel}";
    }

    private readonly ToGeoCoordinateParameter _degreesDecimalMinutesParam = new(geoCoordType: GeoCoordinateType.DDM);
}
