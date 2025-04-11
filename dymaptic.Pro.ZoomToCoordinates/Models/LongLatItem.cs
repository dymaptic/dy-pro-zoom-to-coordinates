using ArcGIS.Core.Geometry;

namespace dymaptic.Pro.ZoomToCoordinates.Models;
public abstract class LongLatItem
{
    // Constructor
    public LongLatItem(double longitude=0, double latitude=0)
    {
        Longitude = longitude;
        Latitude = latitude;
        MapPoint = MapPointBuilderEx.CreateMapPoint(Longitude, Latitude, SpatialReferences.WGS84);
    }
    public MapPoint MapPoint { get; set; }

    public double Longitude { get; set; }
    public double Latitude { get; set; }

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

    public abstract void Update(MapPoint point);
    public abstract void Update(double longitude, double latitude);
}
