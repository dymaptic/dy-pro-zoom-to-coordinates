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
    private string _errorMessage = "";
	private double _longitude = _settings.Longitude;
    private double _latitude = _settings.Latitude;
    private bool _xCoordinateValidated = true;  // when tool loads, valid coordinates are put into the text boxes
	private bool _yCoordinateValidated = true;

    private CoordinateFormatItem _selectedFormatItem = CoordinateFormats.First(f => f.Format == _settings.CoordinateFormat);
    private int _selectedZone = 1;
    private LatitudeBand _selectedLatitudeBandItem;
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
        _mapPoint = MapPointBuilderEx.CreateMapPoint(Longitude, Latitude, SpatialReferences.WGS84);
        _selectedLatitudeBandItem = LatitudeBands.First();
        UpdateCoordinateLabels();
        UpdateDisplay(updateXYCoordinateStrings: true);

        // Command is grayed out if there isn't an active map view
        ZoomCommand = new RelayCommand(async () =>
        {
            await ZoomToCoordinates();
        }, () => MapView.Active != null);

        CopyTextCommand = new RelayCommand(() =>
        {
            CopyText();
        });
    }

    /// <summary>
    ///     UTM Zones 1-60.
    /// </summary>
    public ObservableCollection<int> Zones { get; } = new(Enumerable.Range(1, 60));

    /// <summary>
    ///     A collection of all the latitude bands which span 8° latitude except for X, which spans 12° (UTM/MGRS omit the letters O and I).
    /// </summary>
    public ObservableCollection<LatitudeBand> LatitudeBands { get; } =
        [
            new LatitudeBand { Key = "C", Value = "-80° to -72°" },
            new LatitudeBand { Key = "D", Value = "-72° to -64°" },
            new LatitudeBand { Key = "E", Value = "-64° to -56°" },
            new LatitudeBand { Key = "F", Value = "-56° to -48°" },
            new LatitudeBand { Key = "G", Value = "-48° to -40°" },
            new LatitudeBand { Key = "H", Value = "-40° to -32°" },
            new LatitudeBand { Key = "J", Value = "-32° to -24°" },
            new LatitudeBand { Key = "K", Value = "-24° to -16°" },
            new LatitudeBand { Key = "L", Value = "-16° to -8°" },
            new LatitudeBand { Key = "M", Value = "-8° to 0°" },
            new LatitudeBand { Key = "N", Value = "0° to 8°" },
            new LatitudeBand { Key = "P", Value = "8° to 16°" },
            new LatitudeBand { Key = "Q", Value = "16° to 24°" },
            new LatitudeBand { Key = "R", Value = "24° to 32°" },
            new LatitudeBand { Key = "S", Value = "32° to 40°" },
            new LatitudeBand { Key = "T", Value = "40° to 48°" },
            new LatitudeBand { Key = "U", Value = "48° to 56°" },
            new LatitudeBand { Key = "V", Value = "56° to 64°" },
            new LatitudeBand { Key = "W", Value = "64° to 72°" },
            new LatitudeBand { Key = "X", Value = "72° to 84°" } // X spans 12 degrees
        ];

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
            SetProperty(ref _selectedFormatItem, value);
            SelectedFormat = value.Format;
            UpdateCoordinateLabels();

            // MGRS is a superset of UTM
            ShowUtmControls = SelectedFormat == CoordinateFormat.UTM || SelectedFormat == CoordinateFormat.MGRS;

            // MGRS builds upon UTM by also including 100 km grid zone designations 
            ShowMgrsControl = SelectedFormat == CoordinateFormat.MGRS;

            // Automatic formatting conversions!
            switch (SelectedFormat)
            {
                case CoordinateFormat.DecimalDegrees:
                    XCoordinateString = $"{Math.Abs(_mapPoint!.X):F6}° {(_mapPoint.X >= 0 ? "E" : "W")}";
                    YCoordinateString = $"{Math.Abs(_mapPoint.Y):F6}° {(_mapPoint.Y >= 0 ? "N" : "S")}";
                    break;

                case CoordinateFormat.DegreesDecimalMinutes:
                    FormatAsDegreesDecimalMinutes(_mapPoint!.X, _mapPoint.Y, out string xDDM, out string yDDM);
                    XCoordinateString = xDDM;
                    YCoordinateString = yDDM;
                    break;

                case CoordinateFormat.DegreesMinutesSeconds:
                    FormatAsDegreesMinutesSeconds(_mapPoint!.X, _mapPoint.Y, out string xDMS, out string yDMS);
                    XCoordinateString = xDMS;
                    YCoordinateString = yDMS;
                    break;

                case CoordinateFormat.MGRS:
                    FormatAsMGRS(_mapPoint!.X, _mapPoint.Y, out GridSRItem mgrs);
                    MGRSPoint = mgrs;
                    SelectedZone = mgrs.Zone;
                    SelectedLatitudeBand = mgrs.LatitudeBand;
                    SelectedLatitudeBandItem = LatitudeBands.First(band => band.Key == SelectedLatitudeBand);
                    OneHundredKMGridID = mgrs.MGRSquareID;
                    XCoordinateString = mgrs.Easting.ToString();
                    YCoordinateString = mgrs.Northing.ToString();
                    break;

                case CoordinateFormat.UTM:
                    FormatAsUTM(_mapPoint!.X, _mapPoint.Y, out GridSRItem utm);
                    UTMPoint = utm;
                    SelectedZone = utm.Zone;
                    SelectedLatitudeBand = utm.LatitudeBand;
                    SelectedLatitudeBandItem = LatitudeBands.First(band => band.Key == SelectedLatitudeBand);
                    XCoordinateString = utm.Easting.ToString();
                    YCoordinateString = utm.Northing.ToString();
                    break;

                default:
                    break;
            }

            UpdateDisplay();
        }
    }

    /// <summary>
    ///     Selected UTM Zone (visible for UTM and MGRS only)
    /// </summary>
    public int SelectedZone
    {
        get => _selectedZone;
        set
        {
            SetProperty(ref _selectedZone, value);
            if (_xCoordinateValidated && _yCoordinateValidated)
            {
                if (SelectedFormat == CoordinateFormat.MGRS)
                {
                    MGRSPoint.Zone = SelectedZone;
                    MGRSPoint.GeoCoordinateString = MGRSPoint.Zone + MGRSPoint.GeoCoordinateString[2..];
                }
                else if (SelectedFormat == CoordinateFormat.UTM)
                {
                    UTMPoint.Zone = SelectedZone;
                    UTMPoint.GeoCoordinateString = UTMPoint.Zone + UTMPoint.GeoCoordinateString[2..];
                }

                bool success = UpdateWGS84MapPoint();
                if (success) { UpdateDisplay(); }
            }
        }
    }

    public LatitudeBand SelectedLatitudeBandItem
    {
        get => _selectedLatitudeBandItem;
        set
        {
            SetProperty(ref _selectedLatitudeBandItem, value);
            SelectedLatitudeBand = value.Key;
        }
    }

    /// <summary>
    ///     Selected Latitude band (visible for UTM and MGRS only)
    /// </summary>
    public string SelectedLatitudeBand
    {
        get => _selectedLatitudeBand;
        set
        {
            SetProperty(ref _selectedLatitudeBand, value);
            if (_xCoordinateValidated && _yCoordinateValidated)
            {
                if (SelectedFormat == CoordinateFormat.MGRS)
                {
                    MGRSPoint.LatitudeBand = SelectedLatitudeBand;
                    MGRSPoint.GeoCoordinateString = MGRSPoint.GeoCoordinateString[..2] + MGRSPoint.LatitudeBand + MGRSPoint.GeoCoordinateString[3..];
                }
                else if (SelectedFormat == CoordinateFormat.UTM)
                {
                    UTMPoint.LatitudeBand = SelectedLatitudeBand;
                    UTMPoint.GeoCoordinateString = UTMPoint.GeoCoordinateString[..2] + UTMPoint.LatitudeBand + UTMPoint.GeoCoordinateString[3..];
                }

                bool success = UpdateWGS84MapPoint();
                if (success) { UpdateDisplay(); }
            }
        }
    }

    /// <summary>
    ///     100 KM Grid ID (visible for MGRS only)
    /// </summary>
    public string OneHundredKMGridID
    {
        get => _oneHundredKMGridID;
        set
        {
            SetProperty(ref _oneHundredKMGridID, value);
            if (_xCoordinateValidated && _yCoordinateValidated)
            {
                MGRSPoint.MGRSquareID = OneHundredKMGridID;
                bool success = UpdateWGS84MapPoint();
                if (success) { UpdateDisplay(); }
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
                    bool success = UpdateWGS84MapPoint();
                    if (success) { UpdateDisplay(); }
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
                    bool success = UpdateWGS84MapPoint();
                    if (success) { UpdateDisplay(); }
                }
            }
		}
	}

    /// <summary>
    ///     Provide an informative error message for invalid input.
    /// </summary>
    public string ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
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
                    return x >= 0 && x <= 99_999;  // 5 digits max
                }
                return false;

            case CoordinateFormat.UTM:
                if (double.TryParse(coordinateValue, out double y))
                {
                    if (axis == "X")
                    {
                        return y >= 0 && y <= 999_999;  // 6 digits max for Easting
                    }
                    else
                    {
                        return y >= 0 && y <= 9_999_999;  // 7 digits max for Northing
                    }
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

    /// <summary>
    ///     Updates the Display property and also updates XCoordinateString and YCoordinateString (since the latter two are shared amongst the 5 coordinate formats)
    /// </summary>
    private void UpdateDisplay(bool updateXYCoordinateStrings=false)
    {
        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                if (updateXYCoordinateStrings)
                {
                    XCoordinateString = $"{Math.Abs(Longitude):F6}° {(Longitude >= 0 ? "E" : "W")}";
                    YCoordinateString = $"{Math.Abs(Latitude):F6}° {(Latitude >= 0 ? "N" : "S")}";
                }
                Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                FormatAsDegreesDecimalMinutes(Longitude, Latitude, out string xDDM, out string yDDM);
                if (updateXYCoordinateStrings)
                {
                    XCoordinateString = xDDM;
                    YCoordinateString = yDDM;
                }
                Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                FormatAsDegreesMinutesSeconds(Longitude, Latitude, out string xDMS, out string yDMS);
                if (updateXYCoordinateStrings)
                {
                    XCoordinateString = xDMS;
                    YCoordinateString = yDMS;
                }
                Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.MGRS:
                if (updateXYCoordinateStrings)
                {
                    XCoordinateString = MGRSPoint.Easting.ToString();
                    YCoordinateString = MGRSPoint.Northing.ToString();
                }
                Display = MGRSPoint.Display;
                break;

            case CoordinateFormat.UTM:
                if (updateXYCoordinateStrings)
                {
                    XCoordinateString = UTMPoint.Easting.ToString();
                    YCoordinateString = UTMPoint.Northing.ToString();
                }
                Display = UTMPoint.Display;
                break;
        }
    }

    private bool UpdateWGS84MapPoint()
    {
        try
        {
            if (SelectedFormat == CoordinateFormat.MGRS)
            {
                _mapPoint = MapPointBuilderEx.FromGeoCoordinateString(MGRSPoint.GeoCoordinateString, SpatialReferences.WGS84, GeoCoordinateType.MGRS);
                Longitude = _mapPoint.X;
                Latitude = _mapPoint.Y;
            }
            else if (SelectedFormat == CoordinateFormat.UTM)
            {
                _mapPoint = MapPointBuilderEx.FromGeoCoordinateString(UTMPoint.GeoCoordinateString, SpatialReferences.WGS84, GeoCoordinateType.UTM);
                Longitude = _mapPoint.X;
                Latitude = _mapPoint.Y;
            }
            else  // Handle decimal degrees formats
            {
                _mapPoint = MapPointBuilderEx.CreateMapPoint(Longitude, Latitude, SpatialReferences.WGS84);
            }
            return true;
        }
        catch
        {
            return false;
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
					MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate2D(Longitude, Latitude), SpatialReferences.WGS84);
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

    /// <summary>
    ///     Class to support a friendly description of latitude bands in the View's combobox; e.g., "C: -80° to -72°"
    /// </summary>
    public class LatitudeBand
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";

        public string DisplayText => $"{Key}: {Value}";  // Format for ComboBox
    }
}
