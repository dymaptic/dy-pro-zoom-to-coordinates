using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.ObjectModel;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;
public class CoordinatesBaseViewModel : PropertyChangedBase
{
    // Constants for UTM/MGRS calculations
    private const string LatitudeBands = "CDEFGHJKLMNPQRSTUVWX";  // 'C' to 'X' excluding 'I' and 'O'
    private const string GridSquareLetters = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // Excluding 'I' and 'O'
    private const int NorthernHemisphereBase = 32600;  // EPSG base for northern hemisphere
    private const int SouthernHemisphereBase = 32700;  // EPSG base for southern hemisphere
    public const int WGS84_EPSG = 4326;

    public class CoordinateFormatItem
    {
        public CoordinateFormat Format { get; set; }
        public string? DisplayName { get; set; }

        public override string ToString()
        {
            return DisplayName!;
        }
    }

    public class GridSRItem
    {
        public int EPSG { get; set; }
        public int Zone { get; set; }
        public string GridID { get; set; } = "";  // stores latitude band, one of "CDEFGHJKLMNPQRSTUVWXX";  // Excludes 'I' and 'O'

        public int Easting { get; set; }
        public int Northing { get; set; }
        public string Display => $"{Zone}{GridID} {Easting} {Northing}";
    }

    // Properties
    public ObservableCollection<CoordinateFormatItem> CoordinateFormats { get; } =
    [
        new CoordinateFormatItem { Format = CoordinateFormat.DecimalDegrees, DisplayName = "Decimal Degrees" },
        new CoordinateFormatItem { Format = CoordinateFormat.DegreesDecimalMinutes, DisplayName = "Degrees Decimal Minutes" },
        new CoordinateFormatItem { Format = CoordinateFormat.DegreesMinutesSeconds, DisplayName = "Degrees Minutes Seconds" },
        new CoordinateFormatItem { Format = CoordinateFormat.MGRS, DisplayName = "MGRS" },
        new CoordinateFormatItem { Format = CoordinateFormat.UTM, DisplayName = "UTM" }
    ];

    private static int CalculateUTMZone(double longitude) => (int)Math.Floor((longitude + 180) / 6) + 1;

    /// <summary>
    /// Returns latitude/longitude in decimal degrees as Degrees Decimal Minutes (e.g., 37° 29.1911' N  121° 42.8099' W)
    /// </summary>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="yDDM"></param>
    /// <param name="xDDM"></param>
    public static void FormatDegreesDecimalMinutes(double latitude, double longitude, out string yDDM, out string xDDM)
    {
        // Latitude
        int latDegrees = (int)Math.Abs(latitude);
        double latMinutes = Math.Abs((Math.Abs(latitude) - latDegrees) * 60);
        yDDM = $"{latDegrees}° {latMinutes:F4}' {(latitude >= 0 ? "N" : "S")}";

        // Longitude
        int lonDegrees = (int)Math.Abs(longitude);
        double lonMinutes = Math.Abs((Math.Abs(longitude) - lonDegrees) * 60);
        xDDM = $"{lonDegrees}° {lonMinutes:F4}' {(longitude >= 0 ? "E" : "W")}";
    }

    /// <summary>
    /// Returns latitude/longitude in decimal degrees as Degrees Minutes Seconds (e.g., 37° 29' 2.08" N  121° 42' 57.95" W)
    /// </summary>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="yDMS"></param>
    /// <param name="xDMS"></param>
    public static void FormatDegreesMinutesSeconds(double latitude, double longitude, out string yDMS, out string xDMS)
    {
        // Latitude
        int latDegrees = (int)Math.Abs(latitude);
        double latTotalMinutes = Math.Abs((Math.Abs(latitude) - latDegrees) * 60);
        int latMinutes = (int)latTotalMinutes;
        double latSeconds = (latTotalMinutes - latMinutes) * 60;
        yDMS = $"{latDegrees}° {latMinutes}' {latSeconds:F2}\" {(latitude >= 0 ? "N" : "S")}";

        // Longitude
        int lonDegrees = (int)Math.Abs(longitude);
        double lonTotalMinutes = Math.Abs((Math.Abs(longitude) - lonDegrees) * 60);
        int lonMinutes = (int)lonTotalMinutes;
        double lonSeconds = (lonTotalMinutes - lonMinutes) * 60;
        xDMS = $"{lonDegrees}° {lonMinutes}' {lonSeconds:F2}\" {(longitude >= 0 ? "E" : "W")}";
    }

    private static string GetLatitudeBand(double latitude)
    {
        int bandIndex = (int)Math.Floor((latitude + 80) / 8);
        return LatitudeBands[Math.Clamp(bandIndex, 0, LatitudeBands.Length - 1)].ToString();
    }

    public static int GetUTMEpsgCode(double latitude, int zone)
    {
        // For Northern Hemisphere: EPSG = 32600 + zone (e.g., 32601-32660)
        // For Southern Hemisphere: EPSG = 32700 + zone (e.g., 32701-32760)
        return (latitude >= 0 ? NorthernHemisphereBase : SouthernHemisphereBase) + zone;
    }

    public static void ConvertToMGRS(double longitude, double latitude, out GridSRItem mgrs)
    {
        // First convert to UTM since MGRS builds on UTM
        SpatialReference wgs84 = SpatialReferenceBuilder.CreateSpatialReference(WGS84_EPSG);
        MapPoint wgs84Point = MapPointBuilderEx.CreateMapPoint(longitude, latitude, wgs84);

        int zone = CalculateUTMZone(longitude);
        string latBand = GetLatitudeBand(latitude);
        int epsg = GetUTMEpsgCode(latitude, zone);

        // Project to UTM
        SpatialReference utmSR = SpatialReferenceBuilder.CreateSpatialReference(epsg);
        MapPoint? utmPoint = GeometryEngine.Instance.Project(wgs84Point, utmSR) as MapPoint 
            ?? throw new InvalidOperationException("Failed to project point to UTM coordinates");

        // Calculate 100km grid square letters
        int easting = (int)Math.Floor(utmPoint.X);
        int northing = (int)Math.Floor(utmPoint.Y);

        // Get the column letter from the easting
        // Get the column letter correctly for MGRS repeating every 3 zones
        int column = ((zone - 1) % 3) * 8 + (easting / 100000) % 8;
        //int column = (easting / 100000) % GridSquareLetters.Length;

        // Get the row letter from the northing
        int row = ((northing / 100000) % 20);
        if (zone % 2 == 0)
        {
            // Even numbered zones offset the row letters
            row = (row + 5) % 20;
        }

        // Create the grid square ID from the column and row
        string gridSquare = $"{GridSquareLetters[column]}{GridSquareLetters[row]}";

        // Get the truncated easting and northing (within the 100km grid square)
        int truncatedEasting = easting % 100000;
        int truncatedNorthing = northing % 100000;

        mgrs = new GridSRItem
        {
            EPSG = epsg,
            Zone = zone,
            GridID = latBand + gridSquare,
            Easting = truncatedEasting,
            Northing = truncatedNorthing
        };
    }

    public static void ConvertToUTM(double longitude, double latitude, out GridSRItem utm)
    {
        SpatialReference wgs84 = SpatialReferenceBuilder.CreateSpatialReference(WGS84_EPSG);
        MapPoint wgs84Point = MapPointBuilderEx.CreateMapPoint(longitude, latitude, wgs84);
        
        int zone = CalculateUTMZone(longitude);
        string gridID = GetLatitudeBand(latitude);
        int epsg = GetUTMEpsgCode(latitude, zone);

        SpatialReference utmSR = SpatialReferenceBuilder.CreateSpatialReference(epsg);
        MapPoint? utmPoint = GeometryEngine.Instance.Project(wgs84Point, utmSR) as MapPoint ?? throw new InvalidOperationException("Failed to project point to UTM coordinates");
        utm = new GridSRItem
        {
            EPSG = epsg,
            Zone = zone,
            GridID = gridID,
            Easting = (int)Math.Round(utmPoint.X),
            Northing = (int)Math.Round(utmPoint.Y)
        };
    }
}
