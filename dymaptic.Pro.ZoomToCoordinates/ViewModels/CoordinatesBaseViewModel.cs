using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
