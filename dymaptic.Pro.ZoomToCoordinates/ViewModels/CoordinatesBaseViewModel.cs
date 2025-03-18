﻿using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using dymaptic.Pro.ZoomToCoordinates.Models;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;
public abstract class CoordinatesBaseViewModel : PropertyChangedBase
{
    protected static readonly Settings _settings = ZoomToCoordinatesModule.GetSettings();
    protected string _xCoordinateString = "";
    protected string _yCoordinateString = "";
    protected string _display = "";

    /// <summary>
    ///     Holds UTM Point information once a conversion has occurred.
    /// </summary>
    protected UtmItem _utm = new();

    /// <summary>
    ///     Holds MGRS Point information once a conversion has occurred.
    /// </summary>
    protected MgrsItem _mgrs = new();

    protected LongLatItem _longLatItem = new(_settings.Longitude, _settings.Latitude);


    private string _xCoordinateLabel = "Longitude:";
    private string _yCoordinateLabel = "Latitude:";
    private CoordinateFormat _selectedFormat = _settings.CoordinateFormat;
    protected bool _showFormattedCoordinates = false;
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

    /// <summary>
    ///     Should coordinates be formatted in the display?
    ///     For Decimal degrees, degrees minutes seconds and degrees decimal minutes this adds degree, minute and seconds symbols where applicable.
    ///     For UTM and MGRS, this adds spaces between Easting and Northing to make them easier to read.
    /// </summary>
    public bool ShowFormattedCoordinates
    {
        get => _showFormattedCoordinates;
        set
        {
            SetProperty(ref _showFormattedCoordinates, value);
            UpdateDisplay();
        }
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

    // Abstract or virtual properties to enforce setter logic in derived classes
    public abstract string XCoordinateString { get; set; }
    public abstract string YCoordinateString { get; set; }

    /// <summary>
    ///     Format the coordinates as Military Grid Reference System (MGRS).
    /// </summary>
    /// <param name="longitude"></param>
    /// <param name="latitude"></param>
    /// <param name="mgrs"></param>
    public static void FormatAsMGRS(double longitude, double latitude, out MgrsItem mgrs)
    {
        MapPoint wgs84Point = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);
        
        // Create MGRS coordinate using truncation, rather than rounding
        ToGeoCoordinateParameter mgrsParam = new(geoCoordType: GeoCoordinateType.MGRS, geoCoordMode: ToGeoCoordinateMode.MgrsNewStyle, numDigits:5, rounding:false, addSpaces:false);
        string geoCoordString = wgs84Point.ToGeoCoordinateString(mgrsParam);

        int zone = int.Parse(geoCoordString[..2]);
        string latBand = geoCoordString[2..3];
        string gridSquare = geoCoordString[3..5];
        mgrs = new MgrsItem
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
    public static void FormatAsUTM(double longitude, double latitude, out UtmItem utm)
    {
        MapPoint wgs84Point = MapPointBuilderEx.CreateMapPoint(longitude, latitude, SpatialReferences.WGS84);
        ToGeoCoordinateParameter utmParam = new(geoCoordType: GeoCoordinateType.UTM);
        string geoCoordString = wgs84Point.ToGeoCoordinateString(utmParam);

        string[] parts = geoCoordString.Split(" ");
        int zone = int.Parse(parts[0][..2]);
        string latBand = parts[0][2..3];

        utm = new UtmItem
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

    /// <summary>
    ///     Updates the Display property with the formatted X and Y coordinate information.
    /// </summary>
    protected void UpdateDisplay()
    {
        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                if (_showFormattedCoordinates)
                {
                    _xCoordinateString = _longLatItem.LongitudeDDFormatted;
                    _yCoordinateString = _longLatItem.LatitudeDDFormatted;
                    Display = _longLatItem.DecimalDegreesFormatted;
                }
                else
                {
                    _xCoordinateString = _longLatItem.Longitude.ToString("F6");
                    _yCoordinateString = _longLatItem.Latitude.ToString("F6");
                    Display = _longLatItem.DecimalDegrees;
                }
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                if (_showFormattedCoordinates)
                {
                    _xCoordinateString = _longLatItem.LongitudeDMSFormatted;
                    _yCoordinateString = _longLatItem.LatitudeDMSFormatted;
                    Display = _longLatItem.DegreesMinutesSecondsFormatted;
                }
                else
                {
                    _xCoordinateString = _longLatItem.LongitudeDMS;
                    _yCoordinateString = _longLatItem.LatitudeDMS;
                    Display = _longLatItem.DegreesMinutesSeconds;
                }
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                if (_showFormattedCoordinates)
                {
                    _xCoordinateString = _longLatItem.LongitudeDDMFormatted;
                    _yCoordinateString = _longLatItem.LatitudeDDMFormatted;
                    Display = _longLatItem.DegreesDecimalMinutesFormatted;
                }
                else
                {
                    _xCoordinateString = _longLatItem.LongitudeDDM;
                    _yCoordinateString = _longLatItem.LatitudeDDM;
                    Display = _longLatItem.DegreesDecimalMinutes;
                }
                break;

            case CoordinateFormat.MGRS:
                _xCoordinateString = _mgrs.Easting.ToString();
                _yCoordinateString = _mgrs.Northing.ToString();
                if (_showFormattedCoordinates)
                {
                    Display = _mgrs.Display;
                }
                else
                {
                    Display = _mgrs.GeoCoordinateString;
                }
                break;

            case CoordinateFormat.UTM:
                _xCoordinateString = _utm.Easting.ToString();
                _yCoordinateString = _utm.Northing.ToString();
                if (_showFormattedCoordinates)
                {
                    Display = _utm.Display;
                }
                else
                {
                    Display = _utm.GeoCoordinateString;
                }
                break;
        }

        // Send UI update withtout triggering any setter logic
        // (extensive setter logic for these properties in the derived ZoomCoordinateViewModel class)
        NotifyPropertyChanged(nameof(XCoordinateString));
        NotifyPropertyChanged(nameof(YCoordinateString));
    }
}
