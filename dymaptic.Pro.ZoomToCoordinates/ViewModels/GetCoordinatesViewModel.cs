using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using System;
using System.Linq;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class GetCoordinatesViewModel : CoordinatesBaseViewModel
{
    private double _yCoordinate;
    private double _xCoordinate;
    private string _formattedYCoordinate = "";
    private string _formattedXCoordinate = "";

    // MapPoint will always be WGS84 (we ensure it is in the MapTool)
    private MapPoint _mapPoint = MapPointBuilderEx.CreateMapPoint();

    private CoordinateFormatItem _selectedFormatItem;
    public CoordinateFormatItem SelectedFormatItem
    {
        get => _selectedFormatItem;
        set
        {
            SetProperty(ref _selectedFormatItem, value);
            SelectedFormat = value.Format;
            UpdateCoordinates(_mapPoint);
            UpdateCoordinateLabels();
            UpdateFormattedCoordinates();
        }
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

    public void UpdateFormattedCoordinates()
    {
        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                FormattedXCoordinate = $"{Math.Abs(XCoordinate):F6}° {(XCoordinate >= 0 ? "E" : "W")}";
                FormattedYCoordinate = $"{Math.Abs(YCoordinate):F6}° {(YCoordinate >= 0 ? "N" : "S")}";
                Display = $"{FormattedXCoordinate} {FormattedYCoordinate}";
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                FormatAsDegreesDecimalMinutes(YCoordinate, XCoordinate, out string yDDM, out string xDDM);
                FormattedXCoordinate = xDDM;
                FormattedYCoordinate = yDDM;
                Display = $"{FormattedXCoordinate} {FormattedYCoordinate}";
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                FormatAsDegreesMinutesSeconds(YCoordinate, XCoordinate, out string yDMS, out string xDMS);
                FormattedXCoordinate = xDMS;
                FormattedYCoordinate = yDMS;
                Display = $"{FormattedXCoordinate} {FormattedYCoordinate}";
                break;

            case CoordinateFormat.MGRS:
                FormattedXCoordinate = _mgrs.Easting.ToString();
                FormattedYCoordinate = _mgrs.Northing.ToString();
                Display = _mgrs.Display;
                break;

            case CoordinateFormat.UTM:
                FormattedXCoordinate = _utm.Easting.ToString();
                FormattedYCoordinate = _utm.Northing.ToString();
                Display = _utm.Display;
                break;
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

        switch (SelectedFormat)    
        {
            case CoordinateFormat.DecimalDegrees:
            case CoordinateFormat.DegreesDecimalMinutes:
            case CoordinateFormat.DegreesMinutesSeconds:
                XCoordinate = mapPoint.X;
                YCoordinate = mapPoint.Y;
                break;

            case CoordinateFormat.MGRS:
                FormatAsMGRS(mapPoint.X, mapPoint.Y, out _mgrs);
                XCoordinate = _mgrs.Easting;
                YCoordinate = _mgrs.Northing;
                Display = _mgrs.Display;
                break;

            case CoordinateFormat.UTM:
                FormatAsUTM(mapPoint.X, mapPoint.Y, out _utm);
                XCoordinate = _utm.Easting;
                YCoordinate = _utm.Northing;
                Display = _utm.Display;
                break;
        }
    }

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
}
