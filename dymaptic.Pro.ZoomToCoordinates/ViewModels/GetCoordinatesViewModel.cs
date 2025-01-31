using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class GetCoordinatesViewModel : CoordinatesBaseViewModel
{
    private double _yCoordinate;
    private double _xCoordinate;
    private CoordinateFormat _selectedFormat;
    private string _xCoordinateLabel = "Longitude:";
    private string _yCoordinateLabel = "Latitude:";
    private MapPoint _mapPoint;

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

    private string _formattedYCoordinate;
    private string _formattedXCoordinate;

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
                FormattedYCoordinate = $"{Math.Abs(YCoordinate):F6}° {(YCoordinate >= 0 ? "N" : "S")}";
                FormattedXCoordinate = $"{Math.Abs(XCoordinate):F6}° {(XCoordinate >= 0 ? "E" : "W")}";
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                FormatDegreesDecimalMinutes(YCoordinate, XCoordinate, out string yDDM, out string xDDM);
                FormattedYCoordinate = yDDM;
                FormattedXCoordinate = xDDM;
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                FormatDegreesMinutesSeconds(YCoordinate, XCoordinate, out string yDMS, out string xDMS);
                FormattedYCoordinate = yDMS;
                FormattedXCoordinate = xDMS;
                break;

            case CoordinateFormat.MGRS:
                FormatMGRS(YCoordinate, XCoordinate, out string northing, out string easting);
                FormattedYCoordinate = northing;
                FormattedXCoordinate = easting;
                break;

            case CoordinateFormat.UTM:
                FormatUTM(YCoordinate, XCoordinate, out string north, out string east);
                FormattedYCoordinate = north;
                FormattedXCoordinate = east;
                break;
        }
    }

    private void FormatMGRS(double latitude, double longitude, out string northing, out string easting)
    {
        if (_mapPoint == null) 
        {
            northing = "";
            easting = "";
            return;
        }

        // Use CoordinateFormatter to convert to MGRS
        string mgrsString = ""; // ArcGIS.Core.Geometry.CoordinateFormatter.ToMgrs(_mapPoint, ArcGIS.Core.Geometry.MgrsConversionMode.Automatic, 5, true);
        
        // MGRS format is a single string, so we'll split it for display
        northing = mgrsString;
        easting = ""; // In MGRS, it's all one string
    }

    private void FormatUTM(double latitude, double longitude, out string northing, out string easting)
    {
        if (_mapPoint == null)
        {
            northing = "";
            easting = "";
            return;
        }

        // Use CoordinateFormatter to convert to UTM
        string utmString = "";  // ArcGIS.Core.Geometry.CoordinateFormatter.ToUtm(_mapPoint, ArcGIS.Core.Geometry.UtmConversionMode.NorthSouthIndicators, true);
        
        // Parse UTM string into northing and easting
        // UTM format is typically "Zone Easting Northing"
        var parts = utmString.Split(' ');
        if (parts.Length >= 3)
        {
            easting = $"{parts[0]} {parts[1]}";
            northing = parts[2];
        }
        else
        {
            easting = utmString;
            northing = "";
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
        if (mapPoint == null) return;

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
                ConvertToMGRS(mapPoint.X, mapPoint.Y, out double xMGRS, out double yMGRS);
                XCoordinate = xMGRS;
                YCoordinate = yMGRS;
                break;

            case CoordinateFormat.UTM:
                ConvertToUTM(mapPoint.X, mapPoint.Y, out double xUTM, out double yUTM);
                XCoordinate = xUTM;
                YCoordinate = yUTM;
                break;
        }
    }

    private static void ConvertToDegreesDecimalMinutes(double longitude, double latitude, out double xDDM, out double yDDM)
    {
        // Convert decimal degrees to degrees decimal minutes
        double lonDegrees = (int)longitude;
        double lonMinutes = Math.Abs((longitude - lonDegrees) * 60);
        xDDM = lonDegrees + (lonMinutes / 100); // Store as decimal number where decimal part represents minutes

        double latDegrees = (int)latitude;
        double latMinutes = Math.Abs((latitude - latDegrees) * 60);
        yDDM = latDegrees + (latMinutes / 100);
    }

    private static void ConvertToDegreesMinutesSeconds(double longitude, double latitude, out double xDMS, out double yDMS)
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

    private static void ConvertToMGRS(double longitude, double latitude, out double xMGRS, out double yMGRS)
    {
        // TODO: Implement MGRS conversion using ArcGIS SDK
        // For now, just pass through the values
        xMGRS = longitude;
        yMGRS = latitude;
    }

    private static void ConvertToUTM(double longitude, double latitude, out double xUTM, out double yUTM)
    {
        // TODO: Implement UTM conversion using ArcGIS SDK
        // For now, just pass through the values
        xUTM = longitude;
        yUTM = latitude;
    }

    public GetCoordinatesViewModel()
    {
        var settings = ZoomToCoordinatesModule.GetSettings();
        _selectedFormat = settings.CoordinateFormat;
        _selectedFormatItem = CoordinateFormats.First(f => f.Format == settings.CoordinateFormat);
        UpdateCoordinateLabels();
    }
}
