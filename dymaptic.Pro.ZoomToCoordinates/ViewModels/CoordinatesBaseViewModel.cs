using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using dymaptic.Pro.ZoomToCoordinates.Models;
using dymaptic.Pro.ZoomToCoordinates.Views;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;
public abstract class CoordinatesBaseViewModel : PropertyChangedBase
{
    public ICommand? CopyTextCommand { get; set; }
    public ICommand? OpenSettingsCommand { get; set; }

    private ImageSource? _settingsImageSource = null;

    /// <summary>
    ///     Gets the settings icon image source from ArcGIS Pro resources.
    /// </summary>
    public ImageSource SettingsImageSource
    {
        get
        {
            if (_settingsImageSource == null)
                _settingsImageSource = System.Windows.Application.Current.Resources["CogWheel16"] as ImageSource;
            return _settingsImageSource;
        }
    }
    public static CoordinateFormatItem[] CoordinateFormats { get; } =
    [
        new CoordinateFormatItem { Format = CoordinateFormat.DecimalDegrees, DisplayName = "Decimal Degrees" },
        new CoordinateFormatItem { Format = CoordinateFormat.DegreesDecimalMinutes, DisplayName = "Degrees Decimal Minutes" },
        new CoordinateFormatItem { Format = CoordinateFormat.DegreesMinutesSeconds, DisplayName = "Degrees Minutes Seconds" },
        new CoordinateFormatItem { Format = CoordinateFormat.MGRS, DisplayName = "MGRS" },
        new CoordinateFormatItem { Format = CoordinateFormat.UTM, DisplayName = "UTM" }
    ];

    /// <summary>
    ///     Indicates if user wants to create a graphic after the zoom.
    /// </summary>
    public bool ShowGraphic
    {
        get => _showGraphic;
        set => SetProperty(ref _showGraphic, value);
    }

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
    ///     Provide an informative error message for invalid input or non-implemented coordinate values.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set
        {
            if (SetProperty(ref _errorMessage, value))
            {
                // Force re-evaluation of validation for Display
                NotifyPropertyChanged(nameof(Display));
            }
        }
    }

    /// <summary>
    ///     Allows text to be copied.
    /// </summary>
    public void CopyText()
    {
        if (!string.IsNullOrEmpty(Display))
        {
            Clipboard.SetText(Display);
        }
    }

    /// <summary>
    ///     Opens the Settings window.
    /// </summary>
    public void OpenSettings()
    {
        // Already open?
        var existingWindow = ZoomToCoordinatesModule.GetOpenSettingsWindow();
        if (existingWindow != null)
        {
            // Bring existing window to front
            existingWindow.Activate();
            return;
        }

        var settingsView = new SettingsView
        {
            Owner = FrameworkApplication.Current.MainWindow
        };
        settingsView.Closed += OnSettingsClosed;
        ZoomToCoordinatesModule.SetOpenSettingsWindow(settingsView);
        settingsView.Show();
    }

    private void OnSettingsClosed(object? o, EventArgs e)
    {
        var settingsView = ZoomToCoordinatesModule.GetOpenSettingsWindow();
        if (settingsView != null)
        {
            settingsView.Closed -= OnSettingsClosed;
            ZoomToCoordinatesModule.SetOpenSettingsWindow(null);
        }
    }

    public void CreateGraphic()
    {
        if (_mapPoint == null) { return; }

        MapView mapView = MapView.Active;
        Map map = mapView.Map;
        SpatialReference sr = mapView.Camera.SpatialReference;

        CIMPointSymbol symbol = SymbolFactory.Instance.ConstructPointSymbol(
            color: GetColor(_settings.MarkerColor), 
            size: _settings.MarkerSize, 
            style: GetMarkerStyle(_settings.Marker));

        // 2D Map
        if (map.MapType == MapType.Map)
        {
            // Create the outer Group Layer container (if necessary) which is where point graphics will be placed in ArcGIS Pro Table of Contents
            const string groupLyrName = "Coordinates (Graphics Layers)";
            var groupLyrContainer = map.GetLayersAsFlattenedList()
                .OfType<GroupLayer>()
                .FirstOrDefault(x => x.Name.StartsWith(groupLyrName))
                ?? LayerFactory.Instance.CreateGroupLayer(container: map, index: 0, layerName: groupLyrName);

            // Create point at specified coordinates & graphic for the map
            MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate2D(_mapPoint.X, _mapPoint.Y), SpatialReferences.WGS84);
            CIMGraphic graphic = GraphicFactory.Instance.CreateSimpleGraphic(geometry: point, symbol: symbol);

            // Create the point container inside the group layer container and place graphic into it to create graphic element
            GraphicsLayerCreationParams lyrParams = new() { Name = $"{_display}" };
            GraphicsLayer pointGraphicContainer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(layerParams: lyrParams, container: groupLyrContainer);
            ElementFactory.Instance.CreateGraphicElement(elementContainer: pointGraphicContainer, cimGraphic: graphic, select: false);

            // Finally create a point label graphic & add it to the point container
            CIMTextGraphic label = new()
            {
                Symbol = SymbolFactory.Instance.ConstructTextSymbol(
                    color: GetColor(_settings.FontColor), 
                    size: _settings.FontSize, 
                    fontFamilyName: _settings.FontFamily, 
                    fontStyleName: _settings.FontStyle
                ).MakeSymbolReference(),
                Text = $"    <b>{_display}</b>",
                Shape = point
            };

            pointGraphicContainer.AddElement(cimGraphic: label, select: false);
        }

        // 3D Map (adds an overlay without a label, since CIMTextGraphic is a graphic & graphics are 2D only. Therefore, there isn't a graphics container in the ArcGIS Pro Table of Contents).
        else if (map.IsScene)
        {
            MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate3D(_mapPoint!.X, _mapPoint.Y, z: 0), sr);
            mapView.AddOverlay(point, symbol.MakeSymbolReference());
        }
    }

    /// <summary>
    ///     Updates the View's labels depending on the selected coordinates.
    /// </summary>
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
                    _xCoordinateString = _longLatDD.LongitudeDDFormatted;
                    _yCoordinateString = _longLatDD.LatitudeDDFormatted;
                    Display = _longLatDD.DecimalDegreesFormatted;
                }
                else
                {
                    _xCoordinateString = _longLatDD.Longitude.ToString("F6");
                    _yCoordinateString = _longLatDD.Latitude.ToString("F6");
                    Display = _longLatDD.DecimalDegrees;
                }
                ErrorMessage = string.Empty;
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                if (_showFormattedCoordinates)
                {
                    _xCoordinateString = _longLatDMS.LongitudeDMSFormatted;
                    _yCoordinateString = _longLatDMS.LatitudeDMSFormatted;
                    Display = _longLatDMS.DegreesMinutesSecondsFormatted;
                }
                else
                {
                    _xCoordinateString = _longLatDMS.LongitudeDMS;
                    _yCoordinateString = _longLatDMS.LatitudeDMS;
                    Display = _longLatDMS.DegreesMinutesSeconds;
                }
                ErrorMessage = string.Empty;
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                if (_showFormattedCoordinates)
                {
                    _xCoordinateString = _longLatDDM.LongitudeDDMFormatted;
                    _yCoordinateString = _longLatDDM.LatitudeDDMFormatted;
                    Display = _longLatDDM.DegreesDecimalMinutesFormatted;
                }
                else
                {
                    _xCoordinateString = _longLatDDM.LongitudeDDM;
                    _yCoordinateString = _longLatDDM.LatitudeDDM;
                    Display = _longLatDDM.DegreesDecimalMinutes;
                }
                ErrorMessage = string.Empty;
                break;

            case CoordinateFormat.MGRS:
                if (!string.IsNullOrEmpty(_mgrs.ErrorMessage))
                {
                    ErrorMessage = _mgrs.ErrorMessage;
                    _xCoordinateString = "";
                    _yCoordinateString = "";
                    Display = ErrorMessage;
                }
                else
                {
                    _xCoordinateString = _mgrs.Easting.ToString();
                    _yCoordinateString = _mgrs.Northing.ToString();
                    Display = _showFormattedCoordinates ? _mgrs.Display : _mgrs.GeoCoordinateString;
                    ErrorMessage = string.Empty;
                }
                break;

            case CoordinateFormat.UTM:
                if (!string.IsNullOrEmpty(_utm.ErrorMessage))
                {
                    ErrorMessage = _utm.ErrorMessage;
                    _xCoordinateString = "";
                    _yCoordinateString = "";
                    Display = ErrorMessage;
                }
                else
                {
                    _xCoordinateString = _utm.Easting.ToString();
                    _yCoordinateString = _utm.Northing.ToString();
                    Display = _showFormattedCoordinates ? _utm.Display : _utm.GeoCoordinateString;
                    ErrorMessage = string.Empty;
                }
                break;
        }

        // Send UI update withtout triggering any setter logic
        // (extensive setter logic for these properties in the derived ZoomCoordinateViewModel class)
        NotifyPropertyChanged(nameof(XCoordinateString));
        NotifyPropertyChanged(nameof(YCoordinateString));
    }

    private static CIMColor GetColor(string selectedColor)
    {
        CIMColor color = ColorFactory.Instance.BlackRGB;
        switch (selectedColor)
        {
            case "Black":
                color = ColorFactory.Instance.BlackRGB;

                break;
            case "Gray":
                color = ColorFactory.Instance.GreyRGB;

                break;
            case "White":
                color = ColorFactory.Instance.WhiteRGB;

                break;
            case "Red":
                color = ColorFactory.Instance.RedRGB;

                break;
            case "Green":
                color = ColorFactory.Instance.GreenRGB;

                break;
            case "Blue":
                color = ColorFactory.Instance.BlueRGB;
                
                break;
            case "Pink":
                color = ColorFactory.Instance.CreateRGBColor(255, 115, 223);
                
                break;
            case "Orange":
                color = ColorFactory.Instance.CreateRGBColor(255, 128, 0);

                break;
            case "Yellow":
                color = ColorFactory.Instance.CreateRGBColor(255, 255, 0);

                break;
            case "Purple":
                color = ColorFactory.Instance.CreateRGBColor(153, 0, 153);

                break;
        }
        return color;
    }

    private static SimpleMarkerStyle GetMarkerStyle(string selectedMarker)
    {
        SimpleMarkerStyle marker = SimpleMarkerStyle.Circle;
        switch (selectedMarker)
        {
            case "Circle":
                marker = SimpleMarkerStyle.Circle;

                break;
            case "Cross":
                marker = SimpleMarkerStyle.Cross;

                break;
            case "Diamond":
                marker = SimpleMarkerStyle.Diamond;

                break;
            case "Square":
                marker = SimpleMarkerStyle.Square;

                break;
            case "X":
                marker = SimpleMarkerStyle.X;

                break;
            case "Triangle":
                marker = SimpleMarkerStyle.Triangle;

                break;
            case "Pushpin":
                marker = SimpleMarkerStyle.Pushpin;

                break;
            case "Star":
                marker = SimpleMarkerStyle.Star;

                break;
            case "RoundedSquare":
                marker = SimpleMarkerStyle.RoundedSquare;

                break;
            case "RoundedTriangle":
                marker = SimpleMarkerStyle.RoundedTriangle;

                break;
            case "Rod":
                marker = SimpleMarkerStyle.Rod;

                break;
            case "Rectangle":
                marker = SimpleMarkerStyle.Rectangle;

                break;
            case "RoundedRectangle":
                marker = SimpleMarkerStyle.RoundedRectangle;

                break;
            case "Hexagon":
                marker = SimpleMarkerStyle.Hexagon;

                break;
            case "StretchedHexagon":
                marker = SimpleMarkerStyle.StretchedHexagon;

                break;
            case "RaceTrack":
                marker = SimpleMarkerStyle.RaceTrack;

                break;
            case "HalfCircle":
                marker = SimpleMarkerStyle.HalfCircle;

                break;
            case "Cloud":
                marker = SimpleMarkerStyle.Cloud;

                break;
        }
        return marker;
    }

    protected static readonly Settings _settings = ZoomToCoordinatesModule.GetSettings();
    protected string _xCoordinateString = "";
    protected string _yCoordinateString = "";
    protected string _display = "";
    private string _errorMessage = "";
    protected MapPoint? _mapPoint;
    protected CoordinateFormatItem _selectedFormatItem = CoordinateFormats.First(f => f.Format == _settings.CoordinateFormat);
    protected LongLatDecimalDegrees _longLatDD = new();
    protected LongLatDegreesMinutesSeconds _longLatDMS = new();
    protected LongLatDegreesDecimalMinutes _longLatDDM = new();
    protected MgrsItem _mgrs = new();
    protected UtmItem _utm = new();
    protected bool _showFormattedCoordinates = _settings.ShowFormattedCoordinates;

    private CoordinateFormat _selectedFormat = _settings.CoordinateFormat;
    private string _xCoordinateLabel = "Longitude:";
    private string _yCoordinateLabel = "Latitude:";
    protected bool _showGraphic = _settings.ShowGraphic;
}
