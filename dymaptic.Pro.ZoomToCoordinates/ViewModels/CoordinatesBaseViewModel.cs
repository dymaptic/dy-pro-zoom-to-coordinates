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
        public string GridID { get; set; } = "";  // stores latitude band, one of "CDEFGHJKLMNPQRSTUVWXX";  // Excludes 'I' and 'O'

        public int Easting { get; set; }
        public int Northing { get; set; }
        public string Display => $"{Zone}{GridID}{Easting}{Northing}";
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

    public static void ConvertToMGRS(double longitude, double latitude, out GridSRItem mgrs)
    {
        SpatialReference wgs84 = SpatialReferenceBuilder.CreateSpatialReference(4326);
        MapPoint wgs84Point = MapPointBuilderEx.CreateMapPoint(longitude, latitude, wgs84);
        int zone = (int)Math.Floor((longitude + 180) / 6) + 1;

        // Step 2: Find the Latitude Band (Grid ID)
        string latitudeBands = "CDEFGHJKLMNPQRSTUVWXX";  // Excludes 'I' and 'O'
        int bandIndex = (int)Math.Floor((latitude + 80) / 8);
        string gridID = latitudeBands[Math.Clamp(bandIndex, 0, latitudeBands.Length - 1)].ToString();

        // Default for UTM North
        int epsg = 26900 + zone;
        if (latitude < 0)
        {
            epsg = 32700 + zone; // UTM South (EPSG codes start from 32700 for southern hemisphere)
        }

        SpatialReference utmSR = SpatialReferenceBuilder.CreateSpatialReference(epsg);
        MapPoint? utmPoint = GeometryEngine.Instance.Project(wgs84Point, utmSR) as MapPoint;

        // Initialize GridSRItem and assign values
        mgrs = new GridSRItem
        {
            Zone = zone,
            GridID = gridID,
            Easting = (int)Math.Round(utmPoint!.X),
            Northing = (int)Math.Round(utmPoint!.Y)
        };
    }

    public static void ConvertToUTM(double longitude, double latitude, out GridSRItem utm)
    {
        SpatialReference wgs84 = SpatialReferenceBuilder.CreateSpatialReference(4326);
        MapPoint wgs84Point = MapPointBuilderEx.CreateMapPoint(longitude, latitude, wgs84);
        int zone = (int)Math.Floor((longitude + 180) / 6) + 1;

        // Step 2: Find the Latitude Band (Grid ID)
        string latitudeBands = "CDEFGHJKLMNPQRSTUVWXX";  // Excludes 'I' and 'O'
        int bandIndex = (int)Math.Floor((latitude + 80) / 8);
        string gridID = latitudeBands[Math.Clamp(bandIndex, 0, latitudeBands.Length - 1)].ToString();

        // Default for UTM North
        int epsg = 26900 + zone;
        if (latitude < 0)
        {
            epsg = 32700 + zone; // UTM South (EPSG codes start from 32700 for southern hemisphere)
        }

        SpatialReference utmSR = SpatialReferenceBuilder.CreateSpatialReference(epsg);
        MapPoint? utmPoint = GeometryEngine.Instance.Project(wgs84Point, utmSR) as MapPoint;

        // Initialize GridSRItem and assign values
        utm = new GridSRItem
        {
            Zone = zone,
            GridID = gridID,
            Easting = (int)Math.Round(utmPoint!.X),
            Northing = (int)Math.Round(utmPoint!.Y)
        };
    }
}
