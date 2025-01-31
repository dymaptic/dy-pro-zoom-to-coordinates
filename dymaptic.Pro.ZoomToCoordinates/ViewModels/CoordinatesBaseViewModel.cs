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

    public static void ConvertToMGRS(double longitude, double latitude, out double xMGRS, out double yMGRS)
    {
        // TODO: Implement MGRS conversion using ArcGIS SDK
        // For now, just pass through the values
        xMGRS = longitude;
        yMGRS = latitude;
    }

    public static void ConvertToUTM(double longitude, double latitude, out double xUTM, out double yUTM)
    {
        // TODO: Implement UTM conversion using ArcGIS SDK
        // For now, just pass through the values
        xUTM = longitude;
        yUTM = latitude;
    }
}
