using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;
public class CoordinatesBaseViewModel : PropertyChangedBase
{
    protected static readonly Settings _settings = ZoomToCoordinatesModule.GetSettings();
    protected string _display = "";

    /// <summary>
    ///     Holds UTM Point information once a conversion has occurred.
    /// </summary>
    protected GridSRItem _utm = new();

    /// <summary>
    ///     Holds MGRS Point information once a conversion has occurred.
    /// </summary>
    protected GridSRItem _mgrs = new();

    protected LongLatItem _longLatItem = new(_settings.Longitude, _settings.Latitude);


    private string _xCoordinateLabel = "Longitude:";
    private string _yCoordinateLabel = "Latitude:";
    private CoordinateFormat _selectedFormat = _settings.CoordinateFormat;
    protected bool _isDegrees = true;
    protected bool _showFormattedDegrees = false;
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
    ///     The selected coordinate format.
    /// </summary>
    public CoordinateFormat SelectedFormat
    {
        get => _selectedFormat;
        set => SetProperty(ref _selectedFormat, value);
    }

    public bool IsDegrees
    {
        get => _isDegrees;
        set => SetProperty(ref _isDegrees, value);
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
        
        // Create MGRS coordinate using truncation, rather than rounding
        ToGeoCoordinateParameter mgrsParam = new(geoCoordType: GeoCoordinateType.MGRS, geoCoordMode: ToGeoCoordinateMode.MgrsNewStyle, numDigits:5, rounding:false, addSpaces:false);
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
        ToGeoCoordinateParameter utmParam = new(geoCoordType: GeoCoordinateType.UTM);
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
            GeoCoordinateString = geoCoordString.Replace(" ", "")
        };
    }

    public void CopyText()
    {
        if (!string.IsNullOrEmpty(Display))
        {
            Clipboard.SetText(Display);
        }
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

        public string Display => $"{Zone}{LatitudeBand}{MGRSquareID}{Easting}{Northing}";

        public string GeoCoordinateString { get; set; } = "";
    }

    public class LongLatItem
    {
        private readonly ToGeoCoordinateParameter _decimalDegreesParam = new(geoCoordType: GeoCoordinateType.DD);
        private readonly ToGeoCoordinateParameter _degreesMinutesSecondsParam = new(geoCoordType: GeoCoordinateType.DMS);
        private readonly ToGeoCoordinateParameter _degreesDecimalMinutesParam = new(geoCoordType: GeoCoordinateType.DDM);

        // Constructor
        public LongLatItem(double  longitude, double latitude)
        {
            Longitude = longitude;
            Latitude = latitude;
            WGS84MapPoint = MapPointBuilderEx.CreateMapPoint(Longitude, Latitude, SpatialReferences.WGS84);
            FormatAsDecimalDegrees();
            FormatAsDegreesMinutesSeconds();
            FormatAsDegreesDecimalMinutes();
        }
        public MapPoint WGS84MapPoint { get; set; }

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

        public string DDGeoCoordinateString { get; set; } = "";
        public string DMSGeoCoordinateString { get; set; } = "";
        public string DDMGeoCoordinateString { get; set; } = "";

        /// <summary>
        /// 
        /// </summary>
        private void FormatAsDecimalDegrees()
        {
            DDGeoCoordinateString = WGS84MapPoint.ToGeoCoordinateString(_decimalDegreesParam);
            string[] parts = DDGeoCoordinateString.Split(' ');
            string latitude = parts[0];
            char latitudeLabel = latitude[^1];
            latitude = latitude[..(latitude.Length - 1)];

            // Formatted Latitude WON'T include a negative value if S is in Southern Hemisphere
            LatitudeDDFormatted = latitudeLabel == 'S' ? $"{latitude:F6}° S" : $"{latitude:F6}° N";
            
            // This WILL include negative value if necessary
            Latitude = latitudeLabel == 'S' ? -1 * double.Parse(latitude) : double.Parse(latitude);

            string longitude = parts[1];
            char longitudeLabel = longitude[^1];
            longitude = longitude[..(longitude.Length - 1)];

            LongitudeDDFormatted = longitudeLabel == 'W' ? $"{longitude}° W" : $"{longitude}° E";
            Longitude = longitudeLabel == 'W' ? - 1 * double.Parse(longitude) : double.Parse(longitude);
        }

        /// <summary>
        ///     Formats degrees as Degrees Minutes Seconds with and without symbols (e.g., 37° 29' 2.08" N  121° 42' 57.95" W)
        /// </summary>
        private void FormatAsDegreesMinutesSeconds()
        {
            DMSGeoCoordinateString = WGS84MapPoint.ToGeoCoordinateString(_degreesMinutesSecondsParam);
            string[] parts = DMSGeoCoordinateString.Split(' ');

            LongitudeDMS = $"{parts[0]} {parts[1]} {parts[2][..^1]}";
            LatitudeDMS = $"{parts[3]} {parts[4]} {parts[5][..^1]}";

            char longitudeLabel = parts[2][^1];
            char latitudeLabel = parts[5][^1];

            LongitudeDMSFormatted = $"{parts[0]}° {parts[1]}' {parts[2][..^1]}'' {longitudeLabel}";
            LatitudeDMSFormatted = $"{parts[3]}° {parts[4]}' {parts[5][..^1]}'' {latitudeLabel}";
        }

        /// <summary>
        ///     Formats degrees as Degrees Decimal Minutes with and without symbols (e.g., 37° 29.1911' N  121° 42.8099' W)
        /// </summary>
        private void FormatAsDegreesDecimalMinutes()
        {
            DDMGeoCoordinateString = WGS84MapPoint.ToGeoCoordinateString(_degreesDecimalMinutesParam);
            string[] parts = DDMGeoCoordinateString.Split(' ');

            LongitudeDDM = $"{parts[0]} {parts[1][..^1]}";
            LatitudeDDM = $"{parts[2]} {parts[3][..^1]}";

            char longitudeLabel = parts[1][^1];
            char latitudeLabel = parts[3][^1];

            LongitudeDDMFormatted = $"{parts[0]}° {parts[1][..^1]}' {longitudeLabel}";
            LatitudeDDMFormatted = $"{parts[2]}° {parts[3][..^1]}' {latitudeLabel}";
        }

        public void UpdateCoordinates(double longitude, double latitude)
        {
            // Update the MapPoint
            WGS84MapPoint = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);

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
        public static bool IsValidDecimalDegree(double value, string axis)
        {
            // "X" is Longitude -180 to 180
            // "Y" is Latitude -90 to 90
            double min = axis == "X" ? -180 : -90;
            double max = axis == "X" ? 180 : 90;

            return value >= min && value <= max;
        }

        /// <summary>
        ///     Removes expected (allowed) non-numeric characters for latitude/longitude values. 
        /// </summary>
        /// <param name="value">The latitude or longitude value as a string which may have degree symbols, minute and seconds symbols as well as notation for hemisphere.</param>
        /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
        /// <param name="isNegative">A reference parameter that is set to <c>true</c> if the coordinate is in the western or southern hemisphere; 
        /// otherwise, remains <c>false</c>.</param>
        /// <returns></returns>
        public static string CleanLatLongCoordinateString(string value, string axis, ref bool isNegative)
        {
            string cleanedValue = value;

            // Handle cardinal directions and set negative flag
            if (axis == "X")
            {
                if (cleanedValue.Contains('W'))
                {
                    cleanedValue = cleanedValue.Replace("W", "");
                    isNegative = true;
                }
                cleanedValue = cleanedValue.Replace("E", "");
            }
            else
            {
                if (cleanedValue.Contains('S'))
                {
                    cleanedValue = cleanedValue.Replace("S", "");
                    isNegative = true;
                }
                cleanedValue = cleanedValue.Replace("N", "");
            }

            // Remove degree symbols and trim
            return cleanedValue.Replace("°", " ").Replace("'", " ").Replace("\"", "").Trim();
        }
    }
}
