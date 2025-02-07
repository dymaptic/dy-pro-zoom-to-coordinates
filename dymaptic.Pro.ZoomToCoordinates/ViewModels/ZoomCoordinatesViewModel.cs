using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;
using Newtonsoft.Json.Linq;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class ZoomCoordinatesViewModel : CoordinatesBaseViewModel
{
    // Private backing-fields to the public properties
    private string _xCoordinateLabel = "Longitude:";
    private string _yCoordinateLabel = "Latitude:";
    private string _xCoordinateString;
    private string _yCoordinateString;
	private double _xCoordinate = _settings.Longitude;
    private double _yCoordinate = _settings.Latitude;
    private bool _xCoordinateValidated = true;  // when tool loads, valid coordinates are put into the text boxes
	private bool _yCoordinateValidated = true;
    private double _scale = _settings.Scale;
	private bool _createGraphic = _settings.CreateGraphic;
    private CoordinateFormat _selectedFormat = _settings.CoordinateFormat;
    private MapPoint? _mapPoint;
    private GridSRItem _utm;
    private GridSRItem _mgrs;
	private CoordinateFormatItem _selectedFormatItem;
    private int _selectedZone = 1;
    private string _selectedLatitudeBand = "C";
    private bool _showUtmControls;
    private string _display;


    public ObservableCollection<int> Zones { get; } = new(Enumerable.Range(1, 60));
    public ObservableCollection<string> LatitudeBands { get; } = new ObservableCollection<string>("CDEFGHJKLMNPQRSTUVWX".Select(c => c.ToString()));

    public string Display
    {
        get => _display;
        private set => SetProperty(ref _display, value);
    }

    public int SelectedZone
    {
        get => _selectedZone;
        set => SetProperty(ref _selectedZone, value);
    }

    public string SelectedLatitudeBand
    {
        get => _selectedLatitudeBand;
        set => SetProperty(ref _selectedLatitudeBand, value);
    }

    public bool ShowUtmControls
    {
        get => _showUtmControls;
        set => SetProperty(ref _showUtmControls, value);
    }

    public CoordinateFormatItem SelectedFormatItem
    {
        get => _selectedFormatItem;
        set
        {
            if (value != null && SetProperty(ref _selectedFormatItem, value))
            {
                _selectedFormat = value.Format;
                UpdateCoordinateLabels();
                ShowUtmControls = _selectedFormat == CoordinateFormat.UTM;

                // Update coordinates if we have a point
                // (allows us to convert from one coordinate system to another)
                if (_mapPoint != null)
                {
                    UpdateCoordinates();
                    UpdateFormattedCoordinates();
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

    public string XCoordinateString
    {
        get => _xCoordinateString;
        set
        {
            _xCoordinateValidated = ValidateCoordinate(value, "X");
            if (_xCoordinateValidated)
            {
                SetProperty(ref _xCoordinateString, value);
                if (_yCoordinateValidated)
                {
                    if (_selectedFormat != CoordinateFormat.UTM || _selectedFormat != CoordinateFormat.MGRS)
                    {
                        SpatialReference wgs84 = SpatialReferenceBuilder.CreateSpatialReference(WGS84_EPSG);
                        _mapPoint = MapPointBuilderEx.CreateMapPoint(XCoordinate, YCoordinate, wgs84);
                    }
                }
            }
			else
			{
				_mapPoint = null;
			}
        }
    }

    public string YCoordinateString
	{
		get => _yCoordinateString;
		set
		{
            _yCoordinateValidated = ValidateCoordinate(value, "Y");
			if (_yCoordinateValidated)
			{
                SetProperty(ref _yCoordinateString, value);
				if (_xCoordinateValidated)
				{
					if (_selectedFormat != CoordinateFormat.UTM || _selectedFormat != CoordinateFormat.MGRS)
					{
						SpatialReference wgs84 = SpatialReferenceBuilder.CreateSpatialReference(WGS84_EPSG);
                        _mapPoint = MapPointBuilderEx.CreateMapPoint(XCoordinate, YCoordinate, wgs84);
					}
				}
            }
			else
			{
				_mapPoint = null;
			}
		}
	}

    public double XCoordinate
    {
        get => _xCoordinate;
        set => SetProperty(ref _xCoordinate, value);
    }

    public double YCoordinate
    {
		get => _yCoordinate;
        set => SetProperty(ref _yCoordinate, value);
    }

	public double Scale
	{
		get => _scale;
		set => SetProperty(ref _scale, value);
	}

	public bool CreateGraphic
	{
		get => _createGraphic;
		set => SetProperty(ref _createGraphic, value);
	}

    public ICommand CopyTextCommand { get; }

    public ICommand ZoomCommand { get; }
		
	// Constructor
	public ZoomCoordinatesViewModel()
	{
		// On startup, set property values from settings
		_selectedFormatItem = CoordinateFormats.First(f => f.Format == _settings.CoordinateFormat);
		UpdateCoordinateLabels();

        SpatialReference wgs84 = SpatialReferenceBuilder.CreateSpatialReference(WGS84_EPSG);
        _mapPoint = MapPointBuilderEx.CreateMapPoint(XCoordinate, YCoordinate, wgs84);
		UpdateFormattedCoordinates();

        // Command is grayed out if there isn't an active map view
        ZoomCommand = new RelayCommand(async () => 
        { 
            await ZoomToCoordinates(); 
        }, () => MapView.Active != null);

        // Bind the command
        CopyTextCommand = new RelayCommand(() =>
        {
            CopyText();
        });
    }

	public static bool IsValidDecimalDegree(double value, string axis)
	{
		// "X" is Longitude -180 to 180
		// "Y" is Latitude -90 to 90
		double min = axis == "X" ? -180 : -90;
		double max = axis == "X" ? 180 : 90;

		return value >= min && value <= max;
	}

    private bool ValidateCoordinate(string coordinateValue, string axis)
    {
        if (string.IsNullOrWhiteSpace(coordinateValue))
            return false;

        bool isNegative = false;
        string cleanedValue = CleanCoordinateString(coordinateValue, axis, ref isNegative);

        switch (_selectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                return ValidateDecimalDegrees(cleanedValue, axis, isNegative);

            case CoordinateFormat.DegreesDecimalMinutes:
                return ValidateDegreesDecimalMinutes(cleanedValue, axis, isNegative);

            case CoordinateFormat.DegreesMinutesSeconds:
                return ValidateDegreesMinutesSeconds(cleanedValue, axis, isNegative);

            case CoordinateFormat.MGRS:
            case CoordinateFormat.UTM:
                return ValidateNumericValue(cleanedValue);

            default:
                return false;
        }
    }

    private static string CleanCoordinateString(string value, string axis, ref bool isNegative)
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

    private bool ValidateDecimalDegrees(string value, string axis, bool isNegative)
    {
        if (!double.TryParse(value, out double degrees))
            return false;

        if (isNegative)
            degrees *= -1;

        if (!IsValidDecimalDegree(degrees, axis))
            return false;

        if (axis == "X")
            XCoordinate = degrees;
        else
            YCoordinate = degrees;

        return true;
    }

    private bool ValidateDegreesDecimalMinutes(string value, string axis, bool isNegative)
    {
        string[] parts = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
            return false;

        if (!double.TryParse(parts[0], out double degrees) || 
            !double.TryParse(parts[1], out double decimalMinutes))
            return false;

        if (decimalMinutes >= 60)
            return false;

        double decimalDegrees = degrees + (decimalMinutes / 60);
        if (isNegative)
            decimalDegrees *= -1;

        if (!IsValidDecimalDegree(decimalDegrees, axis))
            return false;

        if (axis == "X")
            XCoordinate = decimalDegrees;
        else
            YCoordinate = decimalDegrees;

        return true;
    }

    private bool ValidateDegreesMinutesSeconds(string value, string axis, bool isNegative)
    {
        string[] parts = value.Split(separator, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3)
            return false;

        if (!double.TryParse(parts[0], out double degrees) || 
            !double.TryParse(parts[1], out double minutes) || 
            !double.TryParse(parts[2], out double seconds))
            return false;

        if (minutes >= 60 || seconds >= 60)
            return false;

        double decimalDegrees = degrees + (minutes / 60) + (seconds / 3600);
        if (isNegative)
            decimalDegrees *= -1;

        if (!IsValidDecimalDegree(decimalDegrees, axis))
            return false;

        if (axis == "X")
            XCoordinate = decimalDegrees;
        else
            YCoordinate = decimalDegrees;

        return true;
    }

    private static bool ValidateNumericValue(string value)
    {
        return double.TryParse(value, out _);
    }

    private void CopyText()
    {
        if (!string.IsNullOrEmpty(Display))
        {
            Clipboard.SetText(Display);
        }
    }

    private void UpdateCoordinateLabels()
    {
        if (_selectedFormat == CoordinateFormat.UTM || _selectedFormat == CoordinateFormat.MGRS)
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

    public void UpdateCoordinates()
    {
        switch (_selectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
            case CoordinateFormat.DegreesDecimalMinutes:
            case CoordinateFormat.DegreesMinutesSeconds:

                // Check if the point is already in WGS84 (SpatialReference WKID 4326)
                if (_mapPoint.SpatialReference.Wkid != 4326)
                {
                    // Reproject to WGS84 if necessary
                    _mapPoint = (MapPoint)GeometryEngine.Instance.Project(_mapPoint, SpatialReferences.WGS84);
                }

                XCoordinate = _mapPoint.X;
                YCoordinate = _mapPoint.Y;
                break;

            case CoordinateFormat.MGRS:
                ConvertToMGRS(_mapPoint.X, _mapPoint.Y, out GridSRItem mgrs);
				MGRSPoint = mgrs;
                XCoordinate = mgrs.Easting;
                YCoordinate = mgrs.Northing;
                break;

            case CoordinateFormat.UTM:
                ConvertToUTM(_mapPoint.X, _mapPoint.Y, out GridSRItem utm);
                UTMPoint = utm;
                SelectedZone = utm.Zone;
                SelectedLatitudeBand = utm.GridID;
                XCoordinate = utm.Easting;
                YCoordinate = utm.Northing;
                Display = utm.Display;
                break;
        }
    }

    private void UpdateFormattedCoordinates()
    {
        switch (_selectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                XCoordinateString = $"{Math.Abs(XCoordinate):F6}° {(XCoordinate >= 0 ? "E" : "W")}";
                YCoordinateString = $"{Math.Abs(YCoordinate):F6}° {(YCoordinate >= 0 ? "N" : "S")}";
                Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                FormatDegreesDecimalMinutes(YCoordinate, XCoordinate, out string yDDM, out string xDDM);
                XCoordinateString = xDDM;
                YCoordinateString = yDDM;
                Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                FormatDegreesMinutesSeconds(YCoordinate, XCoordinate, out string yDMS, out string xDMS);
                XCoordinateString = xDMS;
                YCoordinateString = yDMS;
                Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.MGRS:
                XCoordinateString = MGRSPoint.Easting.ToString();
                YCoordinateString = MGRSPoint.Northing.ToString();
                break;

            case CoordinateFormat.UTM:
                XCoordinateString = UTMPoint.Easting.ToString();
                YCoordinateString = UTMPoint.Northing.ToString();
                break;
        }
    }

    internal async Task ZoomToCoordinates()
	{
		await QueuedTask.Run(() =>
		{
			MapView mapView = MapView.Active;
			Camera camera = mapView.Camera;
			SpatialReference sr = camera.SpatialReference;

			// Create new camera & spatial reference objects, if active map isn't WGS84
			if (sr.Wkid != 4326)
			{
				sr = SpatialReferenceBuilder.CreateSpatialReference(4326);
				Camera newCamera = new(x: XCoordinate, y: YCoordinate, scale: Scale, heading: 0, spatialReference: sr);
				mapView.ZoomTo(newCamera, TimeSpan.Zero);
			}

			// Otherwise, just update the coordinates & scale of the existing camera
			else
			{
				camera.X = XCoordinate;
				camera.Y = YCoordinate;
				camera.Scale = Scale;
				mapView.ZoomTo(camera, TimeSpan.Zero);
			}

			if (CreateGraphic)
			{
				Map map = MapView.Active.Map;
				CIMPointSymbol symbol = SymbolFactory.Instance.ConstructPointSymbol(color: GetColor(_settings.MarkerColor), size: 20, style: GetMarkerStyle(_settings.Marker));

				// 2D Map
				if (map.MapType == MapType.Map)
				{
					// Create the outer Group Layer container (if necessary) which is where point graphics will be placed in ArcGIS Pro Table of Contents
					string groupLyrName = "Coordinates (Graphics Layers)";
					var groupLyrContainer = map.GetLayersAsFlattenedList().OfType<GroupLayer>().Where(x => x.Name.StartsWith(groupLyrName)).FirstOrDefault();
					groupLyrContainer ??= LayerFactory.Instance.CreateGroupLayer(container: map, index: 0, layerName: groupLyrName);

					// Create point at specified coordinates & graphic for the map
					MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate2D(XCoordinate, YCoordinate), sr);
					CIMGraphic graphic = GraphicFactory.Instance.CreateSimpleGraphic(geometry: point, symbol: symbol);

					// Create the point container inside the group layer container and place graphic into it to create graphic element
					GraphicsLayerCreationParams lyrParams = new() { Name = $"{XCoordinateString} {YCoordinateString}" };
					GraphicsLayer pointGraphicContainer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(layerParams: lyrParams, container: groupLyrContainer);
					ElementFactory.Instance.CreateGraphicElement(elementContainer: pointGraphicContainer, cimGraphic: graphic, select: false);

					// Finally create a point label graphic & add it to the point container
					CIMTextGraphic label = new()
					{
						Symbol = SymbolFactory.Instance.ConstructTextSymbol(color: GetColor(_settings.FontColor), size:12, fontFamilyName:_settings.FontFamily, fontStyleName: _settings.FontStyle).MakeSymbolReference(),
						Text = $"    <b>{XCoordinateString} {YCoordinateString}</b>",
						Shape = point
					};

					pointGraphicContainer.AddElement(cimGraphic:label, select: false);
				}

				// 3D Map (adds an overlay without a label, since CIMTextGraphic is a graphic & graphics are 2D only. Therefore, there isn't a graphics container in the ArcGIS Pro Table of Contents).
				else if (map.IsScene)
				{
					MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate3D(x: XCoordinate, y: YCoordinate, z: 0), sr);
					mapView.AddOverlay(point, symbol.MakeSymbolReference());
				}
			}
		});
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

	private static readonly Settings _settings = ZoomToCoordinatesModule.GetSettings();
    private static readonly char[] separator = [' '];
}
