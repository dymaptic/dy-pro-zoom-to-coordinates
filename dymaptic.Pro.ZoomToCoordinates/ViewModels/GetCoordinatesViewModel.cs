using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class GetCoordinatesViewModel : CoordinatesBaseViewModel
{
    private double _yCoordinate;
    private double _xCoordinate;
    private CoordinateFormat _selectedFormat;
    private string _xCoordinateLabel = "Longitude:";
    private string _yCoordinateLabel = "Latitude:";
    private string _formattedYCoordinate;
    private string _formattedXCoordinate;
    private string _display;
    private MapPoint _mapPoint;
    private GridSRItem _utm;
    private GridSRItem _mgrs;

    public ICommand CopyTextCommand { get; }

    private CoordinateFormatItem _selectedFormatItem;
    public CoordinateFormatItem SelectedFormatItem
    {
        get => _selectedFormatItem;
        set
        {
            if (value != null && SetProperty(ref _selectedFormatItem, value))
            {
                _selectedFormat = value.Format;
                UpdateCoordinateLabels();

                // Update coordinates if we have a point
                if (_mapPoint != null)
                {
                    UpdateCoordinates(_mapPoint);
                }
            }
        }
    }
   
    public GridSRItem MGRSPoint
    {
        get => _mgrs;
        set
        {
            if (value != null && SetProperty(ref _mgrs, value))
            {
                _mgrs = value;
            }
        }
    }

    public GridSRItem UTMPoint
    {
        get => _utm;
        set
        {
            if (value != null && SetProperty(ref _utm, value))
            {
                _utm = value;
            }
        }
    }

    public string XCoordinateLabel
    {
        get => _xCoordinateLabel;
        set => SetProperty(ref _xCoordinateLabel, value);
    }

    public string YCoordinateLabel
    {
        get => _yCoordinateLabel;
        set => SetProperty(ref _yCoordinateLabel, value);
    }


    public string Display
    {
        get => _display;
        private set => SetProperty(ref _display, value);
    }


    public string FormattedYCoordinate
    {
        get => _formattedYCoordinate;
        set => SetProperty(ref _formattedYCoordinate, value);
    }

    public string FormattedXCoordinate
    {
        get => _formattedXCoordinate;
        set => SetProperty(ref _formattedXCoordinate, value);
    }

    // Keep the numeric properties for internal use
    private double YCoordinate
    {
        get => _yCoordinate;
        set
        {
            if (SetProperty(ref _yCoordinate, value))
            {
                UpdateFormattedCoordinates();
            }
        }
    }

    private double XCoordinate
    {
        get => _xCoordinate;
        set
        {
            if (SetProperty(ref _xCoordinate, value))
            {
                UpdateFormattedCoordinates();
            }
        }
    }

    private void UpdateFormattedCoordinates()
    {
        switch (_selectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                FormattedXCoordinate = $"{Math.Abs(XCoordinate):F6}° {(XCoordinate >= 0 ? "E" : "W")}";
                FormattedYCoordinate = $"{Math.Abs(YCoordinate):F6}° {(YCoordinate >= 0 ? "N" : "S")}";
                Display = $"{FormattedXCoordinate} {FormattedYCoordinate}";
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                FormatDegreesDecimalMinutes(YCoordinate, XCoordinate, out string yDDM, out string xDDM);
                FormattedXCoordinate = xDDM;
                FormattedYCoordinate = yDDM;
                Display = $"{FormattedXCoordinate} {FormattedYCoordinate}";
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                FormatDegreesMinutesSeconds(YCoordinate, XCoordinate, out string yDMS, out string xDMS);
                FormattedXCoordinate = xDMS;
                FormattedYCoordinate = yDMS;
                Display = $"{FormattedXCoordinate} {FormattedYCoordinate}";
                break;

            case CoordinateFormat.MGRS:
                FormattedXCoordinate = MGRSPoint.Easting.ToString();
                FormattedYCoordinate = MGRSPoint.Northing.ToString();
                break;

            case CoordinateFormat.UTM:
                FormattedXCoordinate = UTMPoint.Easting.ToString();
                FormattedYCoordinate = UTMPoint.Northing.ToString();
                break;
        }
    }


    private void UpdateCoordinateLabels()
    {
        if (_selectedFormat == CoordinateFormat.UTM || _selectedFormat == CoordinateFormat.MGRS)
        {
            YCoordinateLabel = "Northing:";
            XCoordinateLabel = "Easting:";
        }
        else
        {
            YCoordinateLabel = "Latitude:";
            XCoordinateLabel = "Longitude:";
        }
    }

    public void UpdateCoordinates(MapPoint mapPoint)
    {
        _mapPoint = mapPoint;
        if (mapPoint == null)
        {
            Display = "";
            return;
        }

        switch (_selectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                XCoordinate = mapPoint.X;
                YCoordinate = mapPoint.Y;
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                ConvertToDegreesDecimalMinutes(mapPoint.X, mapPoint.Y, out double xDDM, out double yDDM);
                XCoordinate = xDDM;
                YCoordinate = yDDM;
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                ConvertToDegreesMinutesSeconds(mapPoint.X, mapPoint.Y, out double xDMS, out double yDMS);
                XCoordinate = xDMS;
                YCoordinate = yDMS;
                break;

            case CoordinateFormat.MGRS:
                ConvertToMGRS(mapPoint.X, mapPoint.Y, out GridSRItem mgrs);
                MGRSPoint = mgrs;
                XCoordinate = mgrs.Easting;
                YCoordinate = mgrs.Northing;
                break;

            case CoordinateFormat.UTM:
                ConvertToUTM(mapPoint.X, mapPoint.Y, out GridSRItem utm);
                UTMPoint = utm;
                XCoordinate = utm.Easting;
                YCoordinate = utm.Northing;
                Display = utm.Display;
                break;
        }
    }

    public GetCoordinatesViewModel()
    {
        Settings settings = ZoomToCoordinatesModule.GetSettings();
        _selectedFormat = settings.CoordinateFormat;
        _selectedFormatItem = CoordinateFormats.First(f => f.Format == settings.CoordinateFormat);
        UpdateCoordinateLabels();

        // Bind the command
        CopyTextCommand = new RelayCommand(() =>
        {
            CopyText();
        });
    }

    private void CopyText()
    {
        if (!string.IsNullOrEmpty(Display))
        {
            Clipboard.SetText(Display);
        }
    }
}
