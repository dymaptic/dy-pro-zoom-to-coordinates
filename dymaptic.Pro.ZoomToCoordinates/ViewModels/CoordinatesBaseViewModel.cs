using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;
public class CoordinatesBaseViewModel : PropertyChangedBase
{
    public static readonly Settings _settings = ZoomToCoordinatesModule.GetSettings();
    private string _display = "";
    private GridSRItem _utm = new();
    private GridSRItem _mgrs = new();
    private string _xCoordinateLabel = "Longitude:";
    private string _yCoordinateLabel = "Latitude:";
    private CoordinateFormat _selectedFormat = _settings.CoordinateFormat;
    public ICommand? CopyTextCommand { get; set; }


    public static ObservableCollection<CoordinateFormatItem> CoordinateFormats { get; } =
    [
        new CoordinateFormatItem { Format = CoordinateFormat.DecimalDegrees, DisplayName = "Decimal Degrees" },
        new CoordinateFormatItem { Format = CoordinateFormat.DegreesDecimalMinutes, DisplayName = "Degrees Decimal Minutes" },
        new CoordinateFormatItem { Format = CoordinateFormat.DegreesMinutesSeconds, DisplayName = "Degrees Minutes Seconds" },
        new CoordinateFormatItem { Format = CoordinateFormat.MGRS, DisplayName = "MGRS" },
        new CoordinateFormatItem { Format = CoordinateFormat.UTM, DisplayName = "UTM" }
    ];

    /// <summary>
    ///     Shows the formatted X Coordinate followed by the formatted Y Coordinate.
    /// </summary>
    public string Display
    {
        get => _display;
        set => SetProperty(ref _display, value);
    }

    /// <summary>
    ///     Holds MGRS Point information once a conversion has occurred.
    /// </summary>
    public GridSRItem MGRSPoint
    {
        get => _mgrs;
        set => SetProperty(ref _mgrs, value);
    }

    /// <summary>
    ///     Holds UTM Point information once a conversion has occurred.
    /// </summary>
    public GridSRItem UTMPoint
    {
        get => _utm;
        set => SetProperty(ref _utm, value);
    }

    /// <summary>
    ///     The selected coordinate format.
    /// </summary>
    public CoordinateFormat SelectedFormat
    {
        get => _selectedFormat;
        set => SetProperty(ref _selectedFormat, value);
    }

    /// <summary>
    ///     Either Longitude or Easting depending on selected coordinate format.
    /// </summary>
    public string XCoordinateLabel
    {
        get => _xCoordinateLabel;
        set => SetProperty(ref _xCoordinateLabel, value);
    }

    /// <summary>
    ///     Either Latitude or Northing depending on selected coordinate format.
    /// </summary>
    public string YCoordinateLabel
    {
        get => _yCoordinateLabel;
        set => SetProperty(ref _yCoordinateLabel, value);
    }

    /// <summary>
    ///     Format the coordinates as Military Grid Reference System (MGRS).
    /// </summary>
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    /// <param name="mgrs"></param>
    public static void FormatAsMGRS(double longitude, double latitude, out GridSRItem mgrs)
    {
        MapPoint wgs84Point = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);
        ToGeoCoordinateParameter mgrsParam = new(GeoCoordinateType.MGRS);
        string geoCoordString = wgs84Point.ToGeoCoordinateString(mgrsParam);

        int zone = int.Parse(geoCoordString[..2]);
        string latBand = geoCoordString[2..3];
        string gridSquare = geoCoordString[3..5];
        mgrs = new GridSRItem
        {
            Zone = zone,
            LatitudeBand = latBand,
            MGRSquareID = gridSquare,
            Easting = int.Parse(geoCoordString[5..10]),
            Northing = int.Parse(geoCoordString[10..]),
            GeoCoordinateString = geoCoordString
        };
    }

    /// <summary>
    ///     Format the coordinates as Universal Transverse Mercator (UTM).
    /// </summary>
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    /// <param name="utm"></param>
    public static void FormatAsUTM(double longitude, double latitude, out GridSRItem utm)
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
            LatitudeBand = latBand,
            Easting = int.Parse(parts[1]),
            Northing = int.Parse(parts[2]),
            GeoCoordinateString = geoCoordString
        };
    }

    public void CopyText()
    {
        if (!string.IsNullOrEmpty(Display))
        {
            Clipboard.SetText(Display);
        }
    }

    /// <summary>
    /// Returns latitude/longitude in decimal degrees as Degrees Decimal Minutes (e.g., 37° 29.1911' N  121° 42.8099' W)
    /// </summary>
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    /// <param name="xDDM"></param>
    /// <param name="yDDM"></param>
    public static void FormatAsDegreesDecimalMinutes(double longitude, double latitude, out string xDDM, out string yDDM)
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
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    /// <param name="xDMS"></param>
    /// <param name="yDMS"></param>
    public static void FormatAsDegreesMinutesSeconds(double longitude, double latitude, out string xDMS, out string yDMS)
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

    protected void UpdateCoordinateLabels()
    {
        if (SelectedFormat == CoordinateFormat.UTM || SelectedFormat == CoordinateFormat.MGRS)
        {
            XCoordinateLabel = "Easting:";
            YCoordinateLabel = "Northing:";
        }
        else
        {
            XCoordinateLabel = "Longitude:";
            YCoordinateLabel = "Latitude:";
        }
    }

    public class CoordinateFormatItem
    {
        public CoordinateFormat Format { get; set; }
        public string DisplayName { get; set; } = "";

        public override string ToString()
        {
            return DisplayName!;
        }
    }

    public class GridSRItem
    {
        public int Zone { get; set; }

        // UTM and MGRS stores latitude band, one of "CDEFGHJKLMNPQRSTUVWXX" Excludes 'I' and 'O' (1 character total) 
        public string LatitudeBand { get; set; } = "";

        // MGRS only stores 100km Square ID (2 characters total)
        public string MGRSquareID { get; set; } = "";

        public int Easting { get; set; }
        public int Northing { get; set; }
        public string Display => $"{Zone}{LatitudeBand}{MGRSquareID} {Easting} {Northing}";

        public string GeoCoordinateString { get; set; } = "";
    }
}
