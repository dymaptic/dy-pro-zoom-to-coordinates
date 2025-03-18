using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using dymaptic.Pro.ZoomToCoordinates.Models;
using System.Linq;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class GetCoordinatesViewModel : CoordinatesBaseViewModel
{
    private bool _enableFormatting;

    // MapPoint will always be WGS84 (we ensure it is in the MapTool)
    private MapPoint? _mapPoint;

    private CoordinateFormatItem _selectedFormatItem;

    // Constructor
    public GetCoordinatesViewModel()
    {
        Settings settings = ZoomToCoordinatesModule.GetSettings();
        SelectedFormat = settings.CoordinateFormat;
        _selectedFormatItem = CoordinateFormats.First(f => f.Format == settings.CoordinateFormat);
        UpdateCoordinateLabels();

        // Bind the command
        CopyTextCommand = new RelayCommand(() =>
        {
            CopyText();
        });
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
            UpdateFormattedCoordinates();
        }
    }

    public CoordinateFormatItem SelectedFormatItem
    {
        get => _selectedFormatItem;
        set
        {
            SetProperty(ref _selectedFormatItem, value);
            SelectedFormat = value.Format;

            UpdateCoordinateLabels();
            UpdateCoordinates();
        }
    }

    /// <summary>
    ///     Formatting isn't enabled until the user has clicked the map to avoid the settings Lat/Long values being 
    ///     populated if user clicks the format checkbox prior to clicking the map.
    /// </summary>
    public bool EnableFormatting
    {
        get => _enableFormatting;
        set => SetProperty(ref _enableFormatting, value);
    }

    /// <summary>
    ///     The MapPoint that is created when a user clicks that map.
    /// </summary>
    public MapPoint? MapPoint
    {
        get => _mapPoint;
        set => SetProperty(ref _mapPoint, value);
    }

    /// <summary>
    ///     The Y coordinate value as a string.
    /// </summary>
    public override string YCoordinateString
    {
        get => _yCoordinateString;
        set
        {
            SetProperty(ref _yCoordinateString, value);
            EnableFormatting = true;
        }
    }

    /// <summary>
    ///     The X coordinate value as a string.
    /// </summary>
    public override string XCoordinateString
    {
        get => _xCoordinateString;
        set => SetProperty(ref _xCoordinateString, value);
    }

    public void UpdateCoordinates()
    {
        // Exit early if user hasn't clicked the map
        if (_mapPoint == null) return;

        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
            case CoordinateFormat.DegreesMinutesSeconds:
            case CoordinateFormat.DegreesDecimalMinutes:
                _longLatItem.UpdateCoordinates(MapPoint!.X, MapPoint.Y);
                break;


            case CoordinateFormat.MGRS:
                FormatAsMGRS(MapPoint!.X, MapPoint.Y, out _mgrs);
                break;

            case CoordinateFormat.UTM:
                FormatAsUTM(MapPoint!.X, MapPoint.Y, out _utm);
                break;
        }
        UpdateFormattedCoordinates();
    }

    public void UpdateFormattedCoordinates()
    {
        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                if (_showFormattedCoordinates)
                {
                    XCoordinateString = _longLatItem.LongitudeDDFormatted;
                    YCoordinateString = _longLatItem.LatitudeDDFormatted;
                    Display = _longLatItem.DecimalDegreesFormatted;
                }
                else
                {
                    XCoordinateString = _longLatItem.Longitude.ToString("F6");
                    YCoordinateString = _longLatItem.Latitude.ToString("F6");
                    Display = _longLatItem.DecimalDegrees;
                }
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                if (_showFormattedCoordinates)
                {
                    XCoordinateString = _longLatItem.LongitudeDMSFormatted;
                    YCoordinateString = _longLatItem.LatitudeDMSFormatted;
                    Display = _longLatItem.DegreesMinutesSecondsFormatted;
                }
                else
                {
                    XCoordinateString = _longLatItem.LongitudeDMS;
                    YCoordinateString = _longLatItem.LatitudeDMS;
                    Display = _longLatItem.DegreesMinutesSeconds;
                }
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                if (_showFormattedCoordinates)
                {
                    XCoordinateString = _longLatItem.LongitudeDDMFormatted;
                    YCoordinateString = _longLatItem.LatitudeDDMFormatted;
                    Display = _longLatItem.DegreesDecimalMinutesFormatted;
                }
                else
                {
                    XCoordinateString = _longLatItem.LongitudeDDM;
                    YCoordinateString = _longLatItem.LatitudeDDM;
                    Display = _longLatItem.DegreesDecimalMinutes;
                }
                break;

            case CoordinateFormat.MGRS:
                XCoordinateString = _mgrs.Easting.ToString();
                YCoordinateString = _mgrs.Northing.ToString();
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
                XCoordinateString = _utm.Easting.ToString();
                YCoordinateString = _utm.Northing.ToString();
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
    }

}
