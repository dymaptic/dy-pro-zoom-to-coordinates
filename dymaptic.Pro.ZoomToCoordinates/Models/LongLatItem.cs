using ArcGIS.Core.Geometry;

namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class LongLatItem
{
    private readonly ToGeoCoordinateParameter _decimalDegreesParam = new(geoCoordType: GeoCoordinateType.DD);
    private readonly ToGeoCoordinateParameter _degreesMinutesSecondsParam = new(geoCoordType: GeoCoordinateType.DMS);
    private readonly ToGeoCoordinateParameter _degreesDecimalMinutesParam = new(geoCoordType: GeoCoordinateType.DDM);

    // Constructor
    public LongLatItem(double longitude=0, double latitude=0)
    {
        Longitude = longitude;
        Latitude = latitude;
        MapPoint = MapPointBuilderEx.CreateMapPoint(Longitude, Latitude, SpatialReferences.WGS84);
        FormatAsDecimalDegrees();
        FormatAsDegreesMinutesSeconds();
        FormatAsDegreesDecimalMinutes();
    }
    public MapPoint MapPoint { get; private set; }

    #region Decimal Degrees
    public double Longitude { get; set; }
    public double Latitude { get; set; }

    public string LongitudeDDFormatted { get; set; } = "";
    public string LatitudeDDFormatted { get; set; } = "";

    public string DecimalDegrees => $"{Latitude}, {Longitude}";
    public string DecimalDegreesFormatted => $"{LatitudeDDFormatted} {LongitudeDDFormatted}";
    #endregion

    #region Degrees Minutes Seconds
    // Degrees/Minutes/Seconds without the degree, minute, second symbols or N/S/E/W labels
    public string LongitudeDMS { get; set; } = "";
    public string LatitudeDMS { get; set; } = "";

    public string LongitudeDMSFormatted { get; set; } = "";
    public string LatitudeDMSFormatted { get; set; } = "";

    public string DegreesMinutesSeconds => $"{LatitudeDMS} {LongitudeDMS}";
    public string DegreesMinutesSecondsFormatted => $"{LatitudeDMSFormatted} {LongitudeDMSFormatted}";
    #endregion

    #region Degrees Decimal Minutes
    public string LongitudeDDM { get; set; } = "";
    public string LatitudeDDM { get; set; } = "";

    public string LongitudeDDMFormatted { get; set; } = "";
    public string LatitudeDDMFormatted { get; set; } = "";

    public string DegreesDecimalMinutes => $"{LatitudeDDM} {LongitudeDDM}";
    public string DegreesDecimalMinutesFormatted => $"{LatitudeDDMFormatted} {LongitudeDDMFormatted}";
    #endregion

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

        LatitudeDDM = $"{latDegrees} {latMinutes} {latitudeLabel}";
        LongitudeDDM = $"{longDegrees} {longMinutes} {longitudeLabel}";

        LatitudeDDMFormatted = $"{latDegrees}° {latMinutes}' {latitudeLabel}";
        LongitudeDDMFormatted = $"{longDegrees}° {longMinutes}' {longitudeLabel}";
    }

    /// <summary>
    ///     Update the LongLatItem's mapPoint using the one from the ViewModel.
    /// </summary>
    /// <param name="mapPoint">The mapPoint from the ViewModel.</param>
    public void Update(MapPoint mapPoint)
    {
        MapPoint = MapPointBuilderEx.CreateMapPoint(mapPoint.X, mapPoint.Y, SpatialReferences.WGS84);

        FormatAsDecimalDegrees();
        FormatAsDegreesMinutesSeconds();
        FormatAsDegreesDecimalMinutes();
    }

    /// <summary>
    ///     Update the LongLatItem's mapPoint using lat/long.
    /// </summary>
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    public void Update(double longitude, double latitude)
    {
        MapPoint = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);

        FormatAsDecimalDegrees();
        FormatAsDegreesMinutesSeconds();
        FormatAsDegreesDecimalMinutes();
    }

    /// <summary>
    ///     Validates that the decimal degrees entered are valid.
    /// </summary>
    /// <param name="value">The latitude or longitude value as a double.</param>
    /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
    /// <returns></returns>
    public static bool IsValidDecimalDegree(double value, CoordinateAxis axis)
    {
        // "X" is Longitude -180 to 180
        // "Y" is Latitude -90 to 90
        double min = axis == CoordinateAxis.X ? -180 : -90;
        double max = axis == CoordinateAxis.X ? 180 : 90;

        return value >= min && value <= max;
    }

    /// <summary>
    ///     Removes expected (allowed) non-numeric characters for latitude/longitude values. 
    /// </summary>
    /// <param name="value">The latitude or longitude value as a string which may have degree symbols, minute and seconds symbols as well as notation for hemisphere.</param>
    /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
    /// otherwise, remains <c>false</c>.</param>
    /// <returns></returns>
    public static string CleanLatLongCoordinateString(string value, CoordinateAxis axis)
    {
        string cleanedValue = value;

        // Handle cardinal directions and set negative flag
        if (axis == CoordinateAxis.X)
        {
            if (cleanedValue.Contains('W'))
            {
                cleanedValue = cleanedValue.Replace("W", "");
            }
            cleanedValue = cleanedValue.Replace("E", "");
        }
        else
        {
            if (cleanedValue.Contains('S'))
            {
                cleanedValue = cleanedValue.Replace("S", "");
            }
            cleanedValue = cleanedValue.Replace("N", "");
        }

        // Remove degree symbols and trim
        return cleanedValue.Replace("°", " ").Replace("'", " ").Replace("\"", "").Trim();
    }
}
