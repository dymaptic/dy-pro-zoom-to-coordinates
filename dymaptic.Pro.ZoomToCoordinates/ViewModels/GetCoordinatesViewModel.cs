using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using System.Linq;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class GetCoordinatesViewModel : CoordinatesBaseViewModel
{
    private string _yCoordinateString = "";
    private string _xCoordinateString = "";

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
    ///     Only applicable for Decimal Degrees / Degrees Minutes Seconds / Degrees Decimal Minutes
    /// </summary>
    public bool ShowFormattedDegrees
    {
        get => _showFormattedDegrees;
        set
        {
            SetProperty(ref _showFormattedDegrees, value);
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

            if (SelectedFormat == CoordinateFormat.MGRS || SelectedFormat == CoordinateFormat.UTM)
            {
                IsDegrees = false;
            } 
            else
            {
                IsDegrees = true;
            }

            UpdateCoordinateLabels();
            UpdateCoordinates();
        }
    }

    public MapPoint MapPoint
    {
        get => _mapPoint;
        set => SetProperty(ref _mapPoint, value);
    }

    public string YCoordinateString
    {
        get => _yCoordinateString;
        set => SetProperty(ref _yCoordinateString, value);
    }

    public string XCoordinateString
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
                _longLatItem.UpdateCoordinates(MapPoint.X, MapPoint.Y);
                break;


            case CoordinateFormat.MGRS:
                FormatAsMGRS(MapPoint.X, MapPoint.Y, out _mgrs);
                break;

            case CoordinateFormat.UTM:
                FormatAsUTM(MapPoint.X, MapPoint.Y, out _utm);
                break;
        }
        UpdateFormattedCoordinates();
    }

    public void UpdateFormattedCoordinates()
    {
        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                if (_showFormattedDegrees)
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
                if (_showFormattedDegrees)
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
                if (_showFormattedDegrees)
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
                Display = _mgrs.Display;
                break;

            case CoordinateFormat.UTM:
                XCoordinateString = _utm.Easting.ToString();
                YCoordinateString = _utm.Northing.ToString();
                Display = _utm.Display;
                break;
        }
    }
}
