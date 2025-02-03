using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using static dymaptic.Pro.ZoomToCoordinates.ViewModels.CoordinatesBaseViewModel;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;
public class CoordinatesBaseViewModel : PropertyChangedBase
{
    // Constants for UTM/MGRS calculations
    private const string LatitudeBands = "CDEFGHJKLMNPQRSTUVWXX";  // 'C' to 'X' excluding 'I' and 'O'
    private const int NorthernHemisphereBase = 32600;  // EPSG base for northern hemisphere
    private const int SouthernHemisphereBase = 32700;  // EPSG base for southern hemisphere
    private const int WGS84_EPSG = 4326;

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

    public static void ConvertToDegreesDecimalMinutes(double longitude, double latitude, out double xDDM, out double yDDM)
    {
        // Convert decimal degrees to degrees decimal minutes
        double lonDegrees = (int)longitude;
        double lonMinutes = Math.Abs((longitude - lonDegrees) * 60);
        xDDM = lonDegrees + (lonMinutes / 100); // Store as decimal number where decimal part represents minutes

        double latDegrees = (int)latitude;
        double latMinutes = Math.Abs((latitude - latDegrees) * 60);
        yDDM = latDegrees + (latMinutes / 100);
    }

    public static void ConvertToDegreesMinutesSeconds(double longitude, double latitude, out double xDMS, out double yDMS)
    {
        // Convert decimal degrees to degrees minutes seconds
        double lonDegrees = (int)longitude;
        double lonMinutes = (int)(Math.Abs(longitude - lonDegrees) * 60);
        double lonSeconds = Math.Abs((Math.Abs(longitude - lonDegrees) * 60 - lonMinutes) * 60);
        xDMS = lonDegrees + (lonMinutes / 100) + (lonSeconds / 10000); // Store as decimal where decimals represent minutes and seconds

        double latDegrees = (int)latitude;
        double latMinutes = (int)(Math.Abs(latitude - latDegrees) * 60);
        double latSeconds = Math.Abs((Math.Abs(latitude - latDegrees) * 60 - latMinutes) * 60);
        yDMS = latDegrees + (latMinutes / 100) + (latSeconds / 10000);
    }

    private static int CalculateUTMZone(double longitude) => (int)Math.Floor((longitude + 180) / 6) + 1;

    private static string GetLatitudeBand(double latitude)
    {
        int bandIndex = (int)Math.Floor((latitude + 80) / 8);
        return LatitudeBands[Math.Clamp(bandIndex, 0, LatitudeBands.Length - 1)].ToString();
    }

    private static int GetUTMEpsgCode(double latitude, int zone)
    {
        // For Northern Hemisphere: EPSG = 32600 + zone (e.g., 32601-32660)
        // For Southern Hemisphere: EPSG = 32700 + zone (e.g., 32701-32760)
        return (latitude >= 0 ? NorthernHemisphereBase : SouthernHemisphereBase) + zone;
    }

    public static void ConvertToMGRS(double longitude, double latitude, out GridSRItem mgrs)
    {
        SpatialReference wgs84 = SpatialReferenceBuilder.CreateSpatialReference(WGS84_EPSG);
        MapPoint wgs84Point = MapPointBuilderEx.CreateMapPoint(longitude, latitude, wgs84);

        int zone = CalculateUTMZone(longitude);
        string gridID = GetLatitudeBand(latitude);
        int epsg = GetUTMEpsgCode(latitude, zone);

        SpatialReference utmSR = SpatialReferenceBuilder.CreateSpatialReference(epsg);
        MapPoint? utmPoint = GeometryEngine.Instance.Project(wgs84Point, utmSR) as MapPoint ?? throw new InvalidOperationException("Failed to project point to UTM coordinates");
        mgrs = new GridSRItem
        {
            Zone = zone,
            GridID = gridID,
            Easting = (int)Math.Round(utmPoint.X),
            Northing = (int)Math.Round(utmPoint.Y)
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
            Zone = zone,
            GridID = gridID,
            Easting = (int)Math.Round(utmPoint.X),
            Northing = (int)Math.Round(utmPoint.Y)
        };
    }
}
