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
    private UTMItem _utm;

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


    public UTMItem UTMPoint
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

    private string _formattedYCoordinate;
    private string _formattedXCoordinate;
    private bool _isGridReferenceFormat;

    public bool IsGridReferenceFormat
    {
        get => _isGridReferenceFormat;
        private set => SetProperty(ref _isGridReferenceFormat, value);
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
                // FormatUTM(YCoordinate, XCoordinate, out string north, out string east);
                FormattedYCoordinate = Math.Round(YCoordinate).ToString();
                FormattedXCoordinate = Math.Round(XCoordinate).ToString();
                break;
        }
    }

    /// <summary>
    /// Returns latitude/longitude in decimal degrees as Degrees Decimal Minutes (e.g., 37° 29.1911' N  121° 42.8099' W)
    /// </summary>
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="yDDM"></param>
    /// <param name="xDDM"></param>
    private static void FormatDegreesDecimalMinutes(double latitude, double longitude, out string yDDM, out string xDDM)
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
    /// <param name="latitude"></param>
    /// <param name="longitude"></param>
    /// <param name="yDMS"></param>
    /// <param name="xDMS"></param>
    private static void FormatDegreesMinutesSeconds(double latitude, double longitude, out string yDMS, out string xDMS)
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
        IsGridReferenceFormat = _selectedFormat == CoordinateFormat.UTM || _selectedFormat == CoordinateFormat.MGRS;
        if (IsGridReferenceFormat)
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
                ConvertToUTM(mapPoint.X, mapPoint.Y, out UTMItem utm);
                UTMPoint = utm;
                XCoordinate = utm.Easting;
                YCoordinate = utm.Northing;
                break;
        }
    }

    public GetCoordinatesViewModel()
    {
        Settings settings = ZoomToCoordinatesModule.GetSettings();
        _selectedFormat = settings.CoordinateFormat;
        _selectedFormatItem = CoordinateFormats.First(f => f.Format == settings.CoordinateFormat);
        UpdateCoordinateLabels();
    }
}
