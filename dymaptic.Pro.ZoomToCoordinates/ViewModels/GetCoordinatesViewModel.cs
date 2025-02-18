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
            UpdateCoordinateLabels();

            // Update coordinates if we have a point
            if (_mapPoint != null)
            {
                UpdateCoordinates(_mapPoint);
                UpdateFormattedCoordinates();
            }
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
                Display = MGRSPoint.Display;
                break;

            case CoordinateFormat.UTM:
                FormattedXCoordinate = UTMPoint.Easting.ToString();
                FormattedYCoordinate = UTMPoint.Northing.ToString();
                Display = UTMPoint.Display;
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
                ConvertToMGRS(mapPoint.X, mapPoint.Y, out GridSRItem mgrs);
                MGRSPoint = mgrs;
                XCoordinate = mgrs.Easting;
                YCoordinate = mgrs.Northing;
                Display = mgrs.Display;
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
