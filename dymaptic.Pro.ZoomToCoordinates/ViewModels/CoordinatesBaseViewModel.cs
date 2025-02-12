using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.ObjectModel;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;
public class CoordinatesBaseViewModel : PropertyChangedBase
{
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
        public int Zone { get; set; }

        // If UTM stores latitude band, one of "CDEFGHJKLMNPQRSTUVWXX" Excludes 'I' and 'O' (1 character total) 
        // If MGRS stores latitude band AND 100km Square ID (3 characters total)
        public string GridID { get; set; } = "";

        public int Easting { get; set; }
        public int Northing { get; set; }
        public string Display => $"{Zone}{GridID} {Easting} {Northing}";

        public string GeoCoordinateString { get; set; } = "";
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

    public static void ConvertToMGRS(double longitude, double latitude, out GridSRItem mgrs)
    {
        MapPoint wgs84Point = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);
        ToGeoCoordinateParameter mgrsParam = new(GeoCoordinateType.MGRS);
        string geoCoordString = wgs84Point.ToGeoCoordinateString(mgrsParam);

        //ToGeoCoordinateParameter dmsParam = new(GeoCoordinateType.DMS);
        //string geoCoordStringDMS = wgs84Point.ToGeoCoordinateString(dmsParam);

        //ToGeoCoordinateParameter ddmParam = new(GeoCoordinateType.DDM);
        //string geoCoordStringDDM = wgs84Point.ToGeoCoordinateString(ddmParam);

        int zone = int.Parse(geoCoordString[..2]);
        string latBand = geoCoordString[2..3];
        string gridSquare = geoCoordString[3..5];
        mgrs = new GridSRItem
        {
            Zone = zone,
            GridID = latBand + gridSquare,
            Easting = int.Parse(geoCoordString[5..10]),
            Northing = int.Parse(geoCoordString[10..]),
            GeoCoordinateString = geoCoordString
        };
    }

    public static void ConvertToUTM(double longitude, double latitude, out GridSRItem utm)
    {
        MapPoint wgs84Point = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);
        ToGeoCoordinateParameter utmParam = new(GeoCoordinateType.UTM);
        string geoCoordString = wgs84Point.ToGeoCoordinateString(utmParam);

        string[] parts = geoCoordString.Split(" ");
        int zone = int.Parse(parts[0][..2]);
        string latBand = parts[0][2..3];

        utm = new GridSRItem
        {
            Zone = zone,
            GridID = latBand,
            Easting = int.Parse(parts[1]),
            Northing = int.Parse(parts[2]),
            GeoCoordinateString = geoCoordString
        };
    }
}
