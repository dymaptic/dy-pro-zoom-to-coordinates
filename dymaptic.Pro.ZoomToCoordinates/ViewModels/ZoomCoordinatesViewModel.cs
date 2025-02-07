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
    private string _xCoordinateString = _settings.Longitude.ToString();
    private string _yCoordinateString = _settings.Latitude.ToString();
	private double _xCoordinate = _settings.Latitude;
	private double _yCoordinate = _settings.Longitude;
    private bool _xCoordinateValidated = true;  // when tool loads, valid coordinates are put into the text boxes
	private bool _yCoordinateValidated = true;
    private double _scale = _settings.Scale;
	private bool _createGraphic = _settings.CreateGraphic;
    private CoordinateFormat _selectedFormat = _settings.CoordinateFormat;
    private MapPoint? _mapPoint;
    private GridSRItem _utm;
    private GridSRItem _mgrs;
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

                // TODO: Convert coordinates to selected format
                // Update coordinates if we have a point
                if (_mapPoint != null)
                {
                    UpdateCoordinates(_mapPoint);
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
        ZoomCommand = new RelayCommand(async () => { await ZoomToCoordinates(); }, () => MapView.Active != null);
	}

    public async static Task<MapPoint> CreateMapPointAsync(double x, double y, SpatialReference spatialReference)
    {
        return await QueuedTask.Run(() =>
        {
            // Create a MapPoint using the builder
            return MapPointBuilderEx.CreateMapPoint(x, y, spatialReference);
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
		switch (_selectedFormat)
		{
            case CoordinateFormat.DecimalDegrees:
                // Remove degree symbol if present
                string ddValue = coordinateValue.Replace("°", "").Trim();
                if (!double.TryParse(ddValue, out double parsedValue))
                {
                    return false; // Invalid input (not a number)
                }
				bool isValidDD = IsValidDecimalDegree(parsedValue, axis);
				if (isValidDD)
				{
                    if (axis == "X")
                        XCoordinate = parsedValue;
                    else
                        YCoordinate = parsedValue;
                }
				return isValidDD;

            case CoordinateFormat.DegreesDecimalMinutes:
                // Handle both space-separated and symbol formats
                string ddmValue = coordinateValue.Replace("°", " ").Replace("'", "").Trim();
                string[] partsDDM = ddmValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
				if (partsDDM.Length != 2)
				{
					return false;
				}

				// Convert each to a number
				if (!double.TryParse(partsDDM[0], out double degree) || !double.TryParse(partsDDM[1], out double decimalMinutes))
                {
                    return false; // Invalid input (not a number)
                }

				double decimalDegrees = degree + (decimalMinutes / 60);
                bool isValidDDM = IsValidDecimalDegree(decimalDegrees, axis);
                if (isValidDDM)
                {
                    if (axis == "X")
                        XCoordinate = decimalDegrees;
                    else
                        YCoordinate = decimalDegrees;
                }
                return isValidDDM;

            case CoordinateFormat.DegreesMinutesSeconds:
                // Handle both space-separated and symbol formats
                string dmsValue = coordinateValue.Replace("°", " ").Replace("'", " ").Replace("\"", "").Trim();
                string[] partsDMS = dmsValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);
                if (partsDMS.Length != 3)
                {
                    return false;
                }

                // Convert each to a number
                if (!double.TryParse(partsDMS[0], out double degrees) || !double.TryParse(partsDMS[1], out double minutes) || !double.TryParse(partsDMS[2], out double seconds))
                {
                    return false; // Invalid input (not a number)
                }

                double dd = degrees + (minutes / 60) + (seconds / 3600);
                bool isValidDMS = IsValidDecimalDegree(dd, axis);
                if (isValidDMS)
                {
                    if (axis == "X")
                        XCoordinate = dd;
                    else
                        YCoordinate = dd;
                }
                return isValidDMS;

            case CoordinateFormat.MGRS:
                if (!double.TryParse(coordinateValue, out double parsedMGRs))
                {
                    return false; // Invalid input (not a number)
                }
                else
                {
                    return true;
                }

            case CoordinateFormat.UTM:
                if (!double.TryParse(coordinateValue, out double parsedUTM))
                {
                    return false; // Invalid input (not a number)
                }
				else
				{
					return true;
				}

                
			default:
				return false;
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
                //Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                FormatDegreesDecimalMinutes(YCoordinate, XCoordinate, out string yDDM, out string xDDM);
                XCoordinateString = xDDM;
                YCoordinateString = yDDM;
                //Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                FormatDegreesMinutesSeconds(YCoordinate, XCoordinate, out string yDMS, out string xDMS);
                XCoordinateString = xDMS;
                YCoordinateString = yDMS;
                //Display = $"{XCoordinateString} {YCoordinateString}";
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
