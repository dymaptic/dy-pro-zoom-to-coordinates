using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class ZoomCoordinatesViewModel : CoordinatesBaseViewModel
{
    private static readonly char[] separator = [' '];
    private MapPoint? _mapPoint;

    // Private backing-fields to the public properties
    private string _xCoordinateString = "";
    private string _yCoordinateString = "";
	private double _longitude = _settings.Longitude;
    private double _latitude = _settings.Latitude;
    private bool _xCoordinateValidated = true;  // when tool loads, valid coordinates are put into the text boxes
	private bool _yCoordinateValidated = true;

    private CoordinateFormatItem _selectedFormatItem;
    private int _selectedZone = 1;
    private string _selectedLatitudeBand = "C";
    private string _oneHundredKMGridID = "AB";
    private bool _showUtmControls;
    private bool _showMgrsControl;
    private double _scale = _settings.Scale;
	private bool _createGraphic = _settings.CreateGraphic;

    public ICommand ZoomCommand { get; }

    // Constructor
    public ZoomCoordinatesViewModel()
    {
        // On startup, set property values from settings
        _selectedFormatItem = CoordinateFormats.First(f => f.Format == _settings.CoordinateFormat);
        UpdateCoordinateLabels();

        _mapPoint = MapPointBuilderEx.CreateMapPoint(Longitude, Latitude, SpatialReferences.WGS84);
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

    public ObservableCollection<int> Zones { get; } = new(Enumerable.Range(1, 60));
    public ObservableCollection<string> LatitudeBands { get; } = new ObservableCollection<string>("CDEFGHJKLMNPQRSTUVWX".Select(c => c.ToString()));

    /// <summary>
    ///     Selected UTM Zone
    /// </summary>
    public int SelectedZone
    {
        get => _selectedZone;
        set
        {
            SetProperty(ref _selectedZone, value);
            if (_xCoordinateValidated && _yCoordinateValidated)
            {
                UpdateWGS84MapPointFromCoordinates();
            }
        }
    }

    /// <summary>
    ///     Selected Latitude band (UTM and MGRS only)
    /// </summary>
    public string SelectedLatitudeBand
    {
        get => _selectedLatitudeBand;
        set
        {
            SetProperty(ref _selectedLatitudeBand, value);
            if (_xCoordinateValidated && _yCoordinateValidated)
            {
                UpdateWGS84MapPointFromCoordinates();
            }
        }
    }

    /// <summary>
    ///     100 KM Grid ID (MGRS only)
    /// </summary>
    public string OneHundredKMGridID
    {
        get => _oneHundredKMGridID;
        set
        {
            SetProperty(ref _oneHundredKMGridID, value);
            if (_xCoordinateValidated && _yCoordinateValidated)
            {
                UpdateWGS84MapPointFromCoordinates();
            }
        }
    }

    /// <summary>
    ///     Control whether UTM and several MGRS controls get shown in the view (MGRS is an extension of UTM).
    /// </summary>
    public bool ShowUtmControls
    {
        get => _showUtmControls;
        set => SetProperty(ref _showUtmControls, value);
    }

    /// <summary>
    ///     Control whether the 100 KM Grid ID gets shown in the view (MGRS only).
    /// </summary>
    public bool ShowMgrsControl
    {
        get => _showMgrsControl;
        set => SetProperty(ref _showMgrsControl, value);
    }

    /// <summary>
    ///     The selected coordinate reference system.
    /// </summary>
    public CoordinateFormatItem SelectedFormatItem
    {
        get => _selectedFormatItem;
        set
        {
            // When selected format changes, do automatic coordinate conversions
            if (SetProperty(ref _selectedFormatItem, value))
            {
                UpdateCoordinateLabels();
                SelectedFormat = value.Format;
                ShowUtmControls = SelectedFormat == CoordinateFormat.UTM || SelectedFormat == CoordinateFormat.MGRS;
                ShowMgrsControl = SelectedFormat == CoordinateFormat.MGRS;

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

    /// <summary>
    ///     The Longitude value for DD/DDM/DMS or Easting value for UTM/MGRS.
    /// </summary>
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
                    UpdateWGS84MapPointFromCoordinates();
                }
            }
        }
    }

    /// <summary>
    ///     The Latitude value for DD/DDM/DMS or Northing value for UTM/MGRS.
    /// </summary>
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
					UpdateWGS84MapPointFromCoordinates();
                }
            }
		}
	}

    /// <summary>
    ///     Regardless of selected coordinate format, we ALWAYS store a longitude value in decimal degrees.
    /// </summary>
    private double Longitude
    {
        get => _longitude;
        set => SetProperty(ref _longitude, value);
    }

    /// <summary>
    ///     Regardless of selected coordinate format, we ALWAYS store a latitude value in decimal degrees.
    /// </summary>
    private double Latitude
    {
		get => _latitude;
        set => SetProperty(ref _latitude, value);
    }

    /// <summary>
    ///     The scale controls how far in the zoom occurs.
    /// </summary>
	public double Scale
	{
		get => _scale;
		set => SetProperty(ref _scale, value);
	}

    /// <summary>
    ///     Indicates if user wants to create a graphic after the zoom.
    /// </summary>
	public bool CreateGraphic
	{
		get => _createGraphic;
		set => SetProperty(ref _createGraphic, value);
	}

    /// <summary>
    ///     Validates that the decimal degrees entered are valid.
    /// </summary>
    /// <param name="value">The latitude or longitude value as a double.</param>
    /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
    /// <returns></returns>
	private static bool IsValidDecimalDegree(double value, string axis)
	{
		// "X" is Longitude -180 to 180
		// "Y" is Latitude -90 to 90
		double min = axis == "X" ? -180 : -90;
		double max = axis == "X" ? 180 : 90;

		return value >= min && value <= max;
	}

    /// <summary>
    ///     Validates coordinate according to which coordinate system format is selected.
    /// </summary>
    /// <param name="coordinateValue">The coordinate string (it can be in one of five formats: DD, DDM, DMS, MGRS, or UTM).</param>
    /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
    /// <returns></returns>
    private bool ValidateCoordinate(string coordinateValue, string axis)
    {
        if (string.IsNullOrWhiteSpace(coordinateValue))
            return false;

        // Only DD/DDM/DDS allow non-numeric characters
        bool isNegative = false;
        string cleanedLatLongValue = CleanLatLongCoordinateString(coordinateValue, axis, ref isNegative);

        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                return ValidateDecimalDegrees(cleanedLatLongValue, axis, isNegative);

            case CoordinateFormat.DegreesDecimalMinutes:
                return ValidateDegreesDecimalMinutes(cleanedLatLongValue, axis, isNegative);

            case CoordinateFormat.DegreesMinutesSeconds:
                return ValidateDegreesMinutesSeconds(cleanedLatLongValue, axis, isNegative);

            case CoordinateFormat.MGRS:
                if (double.TryParse(coordinateValue, out double x))
                {
                    return x >= 0 && x <= 99999;  // 5 digits max
                }
                return false;

            case CoordinateFormat.UTM:
                if (double.TryParse(coordinateValue, out double y))
                {
                    return y >= 0 && y <= 999999;  // 6 digits max
                }
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    ///     Removes expected (allowed) non-numeric characters for latitude/longitude values. 
    /// </summary>
    /// <param name="value">The latitude or longitude value as a string which may have degree symbols, minute and seconds symbols as well as notation for hemisphere.</param>
    /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
    /// <param name="isNegative">A reference parameter that is set to <c>true</c> if the coordinate is in the western or southern hemisphere; 
    /// otherwise, remains <c>false</c>.</param>
    /// <returns></returns>
    private static string CleanLatLongCoordinateString(string value, string axis, ref bool isNegative)
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

    /// <summary>
    ///     Validates a decimal degrees coordinate string.
    /// </summary>
    /// <param name="value">The latitude or longitude value as a string.</param>
    /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
    /// <param name="isNegative">If <c>true</c>, the value is converted to a negative number.</param>
    /// <returns></returns>
    private bool ValidateDecimalDegrees(string value, string axis, bool isNegative)
    {
        if (!double.TryParse(value, out double degrees))
            return false;

        if (isNegative)
            degrees *= -1;

        if (!IsValidDecimalDegree(degrees, axis))
            return false;

        if (axis == "X")
            Longitude = degrees;
        else
            Latitude = degrees;

        return true;
    }

    /// <summary>
    ///     Validates a degrees decimal minutes coordinate string.
    /// </summary>
    /// <param name="value">The latitude or longitude value as a string.</param>
    /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
    /// <param name="isNegative">If <c>true</c>, the value is converted to a negative number.</param>
    /// <returns></returns>
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
            Longitude = decimalDegrees;
        else
            Latitude = decimalDegrees;

        return true;
    }

    /// <summary>
    ///     Validates a degrees minutes seconds coordinate string.
    /// </summary>
    /// <param name="value">The latitude or longitude value as a string.</param>
    /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
    /// <param name="isNegative">If <c>true</c>, the value is converted to a negative number.</param>
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
            Longitude = decimalDegrees;
        else
            Latitude = decimalDegrees;

        return true;
    }

    public void UpdateCoordinates()
    {
        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
            case CoordinateFormat.DegreesDecimalMinutes:
            case CoordinateFormat.DegreesMinutesSeconds:

                // Check if the point is already in WGS84 (SpatialReference WKID 4326)
                if (_mapPoint!.SpatialReference.Wkid != 4326)
                {
                    // Reproject to WGS84 if necessary
                    _mapPoint = (MapPoint)GeometryEngine.Instance.Project(_mapPoint, SpatialReferences.WGS84);
                }

                Longitude = _mapPoint.X;
                Latitude = _mapPoint.Y;
                break;

            case CoordinateFormat.MGRS:
                ConvertToMGRS(_mapPoint!.X, _mapPoint.Y, out GridSRItem mgrs);
				MGRSPoint = mgrs;
                SelectedZone = mgrs.Zone;
                SelectedLatitudeBand = mgrs.LatitudeBand;
                OneHundredKMGridID = mgrs.MGRSquareID;
                Longitude = _mapPoint.X;
                Latitude = _mapPoint.Y;
                Display = mgrs.Display;
                break;

            case CoordinateFormat.UTM:
                ConvertToUTM(_mapPoint!.X, _mapPoint.Y, out GridSRItem utm);
                UTMPoint = utm;
                SelectedZone = utm.Zone;
                SelectedLatitudeBand = utm.LatitudeBand;
                Longitude = _mapPoint.X;
                Latitude = _mapPoint.Y;
                Display = utm.Display;
                break;
        }
    }

    private void UpdateFormattedCoordinates()
    {
        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                XCoordinateString = $"{Math.Abs(Longitude):F6}° {(Longitude >= 0 ? "E" : "W")}";
                YCoordinateString = $"{Math.Abs(Latitude):F6}° {(Latitude >= 0 ? "N" : "S")}";
                Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                FormatDegreesDecimalMinutes(Latitude, Longitude, out string yDDM, out string xDDM);
                XCoordinateString = xDDM;
                YCoordinateString = yDDM;
                Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                FormatDegreesMinutesSeconds(Latitude, Longitude, out string yDMS, out string xDMS);
                XCoordinateString = xDMS;
                YCoordinateString = yDMS;
                Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.MGRS:
                XCoordinateString = MGRSPoint.Easting.ToString();
                YCoordinateString = MGRSPoint.Northing.ToString();
                Display = MGRSPoint.Display;
                break;

            case CoordinateFormat.UTM:
                XCoordinateString = UTMPoint.Easting.ToString();
                YCoordinateString = UTMPoint.Northing.ToString();
                Display = UTMPoint.Display;
                break;
        }
    }

    private void UpdateWGS84MapPointFromCoordinates()
    {
        if (SelectedFormat == CoordinateFormat.MGRS)
        {
            _mapPoint = MapPointBuilderEx.FromGeoCoordinateString(MGRSPoint.GeoCoordinateString, SpatialReferences.WGS84, GeoCoordinateType.MGRS);
        }
        else if (SelectedFormat == CoordinateFormat.UTM)
        {
            _mapPoint = MapPointBuilderEx.FromGeoCoordinateString(UTMPoint.GeoCoordinateString, SpatialReferences.WGS84, GeoCoordinateType.UTM);
        }
        else  // Handle decimal degrees formats
        {
            _mapPoint = MapPointBuilderEx.CreateMapPoint(Longitude, Latitude, SpatialReferences.WGS84);
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
				Camera newCamera = new(x: Longitude, y: Latitude, scale: Scale, heading: 0, spatialReference: SpatialReferences.WGS84);
				mapView.ZoomTo(newCamera, TimeSpan.Zero);
			}

			// Otherwise, just update the coordinates & scale of the existing camera
			else
			{
				camera.X = Longitude;
				camera.Y = Latitude;
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
					MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate2D(Longitude, Latitude), sr);
					CIMGraphic graphic = GraphicFactory.Instance.CreateSimpleGraphic(geometry: point, symbol: symbol);

					// Create the point container inside the group layer container and place graphic into it to create graphic element
					GraphicsLayerCreationParams lyrParams = new() { Name = $"{Display}" };
					GraphicsLayer pointGraphicContainer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(layerParams: lyrParams, container: groupLyrContainer);
					ElementFactory.Instance.CreateGraphicElement(elementContainer: pointGraphicContainer, cimGraphic: graphic, select: false);

					// Finally create a point label graphic & add it to the point container
					CIMTextGraphic label = new()
					{
						Symbol = SymbolFactory.Instance.ConstructTextSymbol(color: GetColor(_settings.FontColor), size:12, fontFamilyName:_settings.FontFamily, fontStyleName: _settings.FontStyle).MakeSymbolReference(),
						Text = $"    <b>{Display}</b>",
						Shape = point
					};

					pointGraphicContainer.AddElement(cimGraphic:label, select: false);
				}

				// 3D Map (adds an overlay without a label, since CIMTextGraphic is a graphic & graphics are 2D only. Therefore, there isn't a graphics container in the ArcGIS Pro Table of Contents).
				else if (map.IsScene)
				{
					MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate3D(x: Longitude, y: Latitude, z: 0), sr);
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
}
