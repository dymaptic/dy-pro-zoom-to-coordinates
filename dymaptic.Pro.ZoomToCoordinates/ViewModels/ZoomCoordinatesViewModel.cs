using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class ZoomCoordinatesViewModel : CoordinatesBaseViewModel
{
    private static readonly char[] separator = [' '];
    private MapPoint? _mapPoint;
    private bool _xCoordinateValidated = true;  // when tool loads, valid coordinates are put into the text boxes
    private bool _yCoordinateValidated = true;

    /// <summary>
    ///     Regardless of selected coordinate format, we ALWAYS store a longitude value in decimal degrees.
    /// </summary>
    private double _longitude = _settings.Longitude;

    /// <summary>
    ///     Regardless of selected coordinate format, we ALWAYS store a latitude value in decimal degrees.
    /// </summary>
    private double _latitude = _settings.Latitude;

    // Private backing-fields to the public properties
    private string _xCoordinateString = "";
    private string _yCoordinateString = "";
    private string _errorMessage = "";
    private CoordinateFormatItem _selectedFormatItem = CoordinateFormats.First(f => f.Format == _settings.CoordinateFormat);
    private int _selectedZone;
    private LatitudeBand _selectedLatitudeBandItem;
    private string _selectedLatitudeBand = "";
    private string _oneHundredKMGridID = "";
    private bool _showUtmControls;
    private bool _showMgrsControl;
    private double _scale = _settings.Scale;
	private bool _createGraphic = _settings.CreateGraphic;

    public ICommand ZoomCommand { get; }

    // Constructor
    public ZoomCoordinatesViewModel()
    {
        // Create a MapPoint right off the bat with the default coordinates
        _mapPoint = MapPointBuilderEx.CreateMapPoint(_longitude, _latitude, SpatialReferences.WGS84);

        // Initialize GridSRItem objects and fields
        FormatAsUTM(_longitude, _latitude, out _utm);
        _selectedZone = _utm.Zone;
        _selectedLatitudeBand = _utm.LatitudeBand;
        _selectedLatitudeBandItem = LatitudeBands.First(b => b.Key == _utm.LatitudeBand);

        FormatAsMGRS(_longitude, _latitude, out _mgrs);
        _oneHundredKMGridID = _mgrs.MGRSquareID;

        // Set initially for the Display
        _xCoordinateString = $"{Math.Abs(_longitude):F6}° {(_longitude >= 0 ? "E" : "W")}";
        _yCoordinateString = $"{Math.Abs(_latitude):F6}° {(_latitude >= 0 ? "N" : "S")}";

        UpdateDisplay();
        UpdateCoordinateLabels();

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
            new LatitudeBand { Key = "X", Value = "72° to 84°" } // X spans 12 degrees instead of 8 degrees like the rest.
        ];

    private ObservableCollection<string> _mgrsGridIds = [];
    public ObservableCollection<string> MgrsGridIds
    {
        get => _mgrsGridIds;
        set => SetProperty(ref _mgrsGridIds, value);
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
                    FormatAsMGRS(_mapPoint!.X, _mapPoint.Y, out _mgrs);
                    SelectedZone = _mgrs.Zone;
                    SelectedLatitudeBand = _mgrs.LatitudeBand;
                    SelectedLatitudeBandItem = LatitudeBands.First(band => band.Key == _selectedLatitudeBand);
                    OneHundredKMGridID = _mgrs.MGRSquareID;
                    XCoordinateString = _mgrs.Easting.ToString();
                    YCoordinateString = _mgrs.Northing.ToString();
                    break;

                case CoordinateFormat.UTM:
                    FormatAsUTM(_mapPoint!.X, _mapPoint.Y, out _utm);
                    SelectedZone = _utm.Zone;
                    SelectedLatitudeBand = _utm.LatitudeBand;
                    SelectedLatitudeBandItem = LatitudeBands.First(band => band.Key == _selectedLatitudeBand);
                    XCoordinateString = _utm.Easting.ToString();
                    YCoordinateString = _utm.Northing.ToString();
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
                switch (SelectedFormat)
                {
                    case CoordinateFormat.MGRS:
                        _mgrs.Zone = _selectedZone;
                        _mgrs.GeoCoordinateString = _mgrs.Zone + _mgrs.GeoCoordinateString[2..];
                        UpdateMgrsGridIds();
                        break;
                    case CoordinateFormat.UTM:
                        _utm.Zone = _selectedZone;
                        _utm.GeoCoordinateString = _utm.Zone + _utm.GeoCoordinateString[2..];
                        break;
                    default:
                        break;
                }

                if (UpdateWGS84MapPoint())
                {
                    UpdateDisplay();
                }
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
            switch (SelectedFormat)
            {
                case CoordinateFormat.MGRS:
                    AdjustNorthingForLatitudeBandChange(_mgrs, _selectedLatitudeBand);
                    UpdateMgrsGridIds();
                    break;
                case CoordinateFormat.UTM:
                    AdjustNorthingForLatitudeBandChange(_utm, _selectedLatitudeBand);
                    break;
                default:
                    break;
            }

            if (UpdateWGS84MapPoint())
            {
                UpdateDisplay();
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
                // Update the GeoCoordinateString b/c that's how we update the WGS84MapPoint!
                _mgrs.MGRSquareID = _oneHundredKMGridID;
                _mgrs.GeoCoordinateString = _mgrs.GeoCoordinateString[..3] + _mgrs.MGRSquareID + _mgrs.GeoCoordinateString[5..];

                if (UpdateWGS84MapPoint())
                {
                    UpdateDisplay();
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
                    // Update the GeoCoordinateStrings, since those are used to update the WGS84MapPoint
                    switch (SelectedFormat)
                    {
                        case CoordinateFormat.MGRS:
                            _mgrs.Easting = int.Parse(_xCoordinateString);
                            _mgrs.GeoCoordinateString = _mgrs.GeoCoordinateString[..5] + _mgrs.Easting.ToString("D5") + _mgrs.GeoCoordinateString[10..];
                            break;

                        case CoordinateFormat.UTM:
                            _utm.Easting = int.Parse(_xCoordinateString);
                            _utm.GeoCoordinateString = _utm.GeoCoordinateString[..3] + _utm.Easting.ToString("D6") + _utm.GeoCoordinateString[9..];
                            break;
                        default:
                            break;
                    }

                    if (UpdateWGS84MapPoint())
                    {
                        UpdateDisplay();
                    }
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
                    // Update the GeoCoordinateStrings, since those are used to update the WGS84MapPoint
                    switch (SelectedFormat)
                    {
                        case CoordinateFormat.MGRS:
                            _mgrs.Northing = int.Parse(_yCoordinateString);
                            _mgrs.GeoCoordinateString = _mgrs.GeoCoordinateString[..10] + _mgrs.Northing.ToString("D5");
                            break;

                        case CoordinateFormat.UTM:

                            // Check if the change in Northing was large enough to place it in a new Latitude Band
                            UpdateLatitudeBandForNorthingChange(_utm, _yCoordinateString);
                            break;
                        default:
                            break;
                    }

                    if (UpdateWGS84MapPoint())
                    {
                        UpdateDisplay();
                    }
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
    ///     Updates the MGRS 100 KM Grid ID possibilities given the selected UTM zone and selected latitude band.
    /// </summary>
    private void UpdateMgrsGridIds()
    {
        // Store the value of the selected 100 KM MGRS Grid ID
        string temp = OneHundredKMGridID;

        // Reset the possibilities - sets the selected value to null
        _mgrsGridIds.Clear();
        MgrsGridIds = GetMgrsGridIds(SelectedZone, SelectedLatitudeBand);

        // Set selected value back to what it was (combobox will now be updated with valid possibilities).
        OneHundredKMGridID = temp;
    }

    /// <summary>
    ///     Gets the 100 KM MGRS Grid ID possibilities given a UTM Zone and latitude band.
    /// </summary>
    /// <param name="utmZone">The UTM Zone value of 1-60.</param>
    /// <param name="latitudeBand">The Latitude Band letter, C-X excluding I and O.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static ObservableCollection<string> GetMgrsGridIds(int utmZone, string latitudeBand)
    {
        // MGRS column possibilities depend on the UTM Zone
        string[] columnSets = ["ABCDEFGH", "JKLMNPQR", "STUVWXYZ"];
        int setIndex = (utmZone - 1) % 3;
        string columnSet = columnSets[setIndex];

        // MGRS row possibilities based on Latitude Band
        Dictionary<string, List<string>> rows = new()
        {
            // Southern Hemisphere
            { "C", new List<string> { "F", "E", "D", "C", "B", "A", "V", "U", "T", "S" } },
            { "D", new List<string> { "Q", "P", "N", "M", "L", "K", "J", "H", "G", "F" } },
            { "E", new List<string> { "C", "B", "A", "V", "U", "T", "S", "R", "Q", "P", "Q" } },
            { "F", new List<string> { "M", "L", "K", "J", "H", "G", "F", "E", "D", "C" } },
            { "G", new List<string> { "A", "V", "U", "T", "S", "R", "Q", "P", "N", "M" } },
            { "H", new List<string> { "K", "J", "H", "G", "F", "E", "D", "C", "B", "A" } },
            { "J", new List<string> { "U", "T", "S", "R", "Q", "P", "N", "M", "L", "K" } },
            { "K", new List<string> { "H", "G", "F", "E", "D", "C", "B", "A", "V", "U" } },
            { "L", new List<string> { "S", "R", "Q", "P", "N", "M", "L", "K", "J", "H" } },
            { "M", new List<string> { "E", "D", "C", "B", "A", "V", "U", "T", "S" } },

            // Northern Hemisphere
            { "N", new List<string> { "F", "G", "H", "J", "K", "L", "M", "N", "P" } },
            { "P", new List<string> { "P", "Q", "R", "S", "T", "U", "V", "A", "B", "C" } },
            { "Q", new List<string> { "C", "D", "E", "F", "G", "H", "J", "K", "L", "M" } },
            { "R", new List<string> { "M", "N", "P", "Q", "R", "S", "T", "U", "V", "A" } },
            { "S", new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K" } },
            { "T", new List<string> { "K", "L", "M", "N", "P", "Q", "R", "S", "T", "U" } },
            { "U", new List<string> { "U", "V", "A", "B", "C", "D", "E", "F", "G", "H" } },
            { "V", new List<string> { "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S" } },
            { "W", new List<string> { "S", "T", "U", "V", "A", "B", "C", "D", "E" } },
            { "X", new List<string> { "E", "F", "G", "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S", "T", "U" } }
        };

        // Note: exception shouldn't ever be thrown b/c we constrain the Latitude Band possibilities that the user can select.
        if (!rows.TryGetValue(latitudeBand, out List<string> rowSet))
        {
            throw new ArgumentException($"Invalid latitude band: {latitudeBand}");
        }

        ObservableCollection<string> mgrsGridIds = [];

        foreach (char col in columnSet)
        {
            foreach (string row in rowSet)
            {
                mgrsGridIds.Add($"{col}{row}");
            }
        }

        return mgrsGridIds;
    }

    private static readonly Dictionary<string, int> LatitudeBandStartDegrees = new()
{
    { "C", -80 }, { "D", -72 }, { "E", -64 }, { "F", -56 }, { "G", -48 }, { "H", -40 },
    { "J", -32 }, { "K", -24 }, { "L", -16 }, { "M", -8 }, { "N", 0 }, { "P", 8 },
    { "Q", 16 }, { "R", 24 }, { "S", 32 }, { "T", 40 }, { "U", 48 }, { "V", 56 },
    { "W", 64 }, { "X", 72 }
};

    /// <summary>
    ///     When the Latitude Band changes, adjust the Northing value to follow a better UI experience where all other UTM parametes 
    ///     remaining the same, the new point would be placed in the correct Latitude Band.
    /// </summary>
    /// <param name="gridItem">Either the UTM or MGRS objects.</param>
    /// <param name="newBand">The potentially new latitude band value.</param>
    private void AdjustNorthingForLatitudeBandChange(GridSRItem gridItem, string newBand)
    {
        // Grab the original
        string oldBand = gridItem.LatitudeBand;
        if (oldBand == newBand)
        {
            return;
        }
        
        // Update our UTM object
        gridItem.LatitudeBand = newBand;

        int oldLatitude = LatitudeBandStartDegrees[oldBand];
        int newLatitude = LatitudeBandStartDegrees[newBand];

        // Calculate latitude difference
        int latitudeDifference = newLatitude - oldLatitude;

        // Convert to meters (approx. 111,000 meters per degree)
        int northingOffset = latitudeDifference * 111000;

        // Apply adjustment
        gridItem.Northing += northingOffset;
        if (String.IsNullOrEmpty(gridItem.MGRSquareID))
        {
            gridItem.GeoCoordinateString = gridItem.Zone + gridItem.LatitudeBand + gridItem.Easting.ToString("D6") + gridItem.Northing.ToString("D7");
        }
        else
        {
            gridItem.GeoCoordinateString = gridItem.Zone + gridItem.LatitudeBand + gridItem.MGRSquareID + gridItem.Easting.ToString("D5") + gridItem.Northing.ToString("D5");
        }

        // Lastly, trigger a UI refresh
        YCoordinateString = gridItem.Northing.ToString();
    }

    /// <summary>
    ///     When the Northing value changes enough to put it into a new Latitude Band, update the Latitude Band. 
    /// </summary>
    /// <param name="gridItem">Either the UTM or MGRS objects.</param>
    /// <param name="yCoord">The Northing value.</param>
    private void UpdateLatitudeBandForNorthingChange(GridSRItem gridItem, string yCoord)
    {
        _utm.Northing = int.Parse(yCoord);

        // Convert Northing to Latitude (Approximation)
        double newLatitude = gridItem.Northing / 111000.0; // Approximate meters per degree

        // Find the correct latitude band
        string newLatitudeBand = LatitudeBandStartDegrees
            .Where(band => newLatitude >= band.Value) // Find the highest band that matches
            .OrderByDescending(band => band.Value)
            .Select(band => band.Key)
            .First();

        // Did the new Northing input, trigger a Latitude Band change?  
        if (newLatitudeBand != gridItem.LatitudeBand)
        {
            gridItem.LatitudeBand = newLatitudeBand;

            // Don't trigger the setter logic here
            _selectedLatitudeBand = newLatitudeBand;

            // But do trigger UI refresh!
            SelectedLatitudeBandItem = LatitudeBands
                .Where(x => x.Key == _selectedLatitudeBand)
                .First();
        }

        // Lastly, regardless of the magnitude of the Northing change, always update the GeoCoordinateString since it includes Northing too and it's used for updating the WGS84MapPoint
        if (string.IsNullOrEmpty(gridItem.MGRSquareID))
        {
            gridItem.GeoCoordinateString = gridItem.Zone + gridItem.LatitudeBand +
                                           gridItem.Easting.ToString("D6") + gridItem.Northing.ToString("D7");
        }
        else
        {
            gridItem.GeoCoordinateString = gridItem.Zone + gridItem.LatitudeBand + gridItem.MGRSquareID +
                                           gridItem.Easting.ToString("D5") + gridItem.Northing.ToString("D5");
        }
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
            _longitude = degrees;
        else
            _latitude = degrees;

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
            _longitude = decimalDegrees;
        else
            _latitude = decimalDegrees;

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
            _longitude = decimalDegrees;
        else
            _latitude = decimalDegrees;

        return true;
    }

    /// <summary>
    ///     Updates the Display property with the formatted X and Y coordinate information.
    /// </summary>
    private void UpdateDisplay()
    {
        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
            case CoordinateFormat.DegreesDecimalMinutes:
            case CoordinateFormat.DegreesMinutesSeconds:
                Display = $"{XCoordinateString} {YCoordinateString}";
                break;

            case CoordinateFormat.MGRS:
                Display = _mgrs.Display;
                break;

            case CoordinateFormat.UTM:
                Display = _utm.Display;
                break;
        }
    }

    private bool UpdateWGS84MapPoint()
    {
        try
        {
            if (SelectedFormat == CoordinateFormat.MGRS)
            {
                _mapPoint = MapPointBuilderEx.FromGeoCoordinateString(geoCoordString: _mgrs.GeoCoordinateString, 
                                                                      spatialReference:SpatialReferences.WGS84, 
                                                                      geoCoordType:GeoCoordinateType.MGRS,
                                                                      geoCoordMode:FromGeoCoordinateMode.MgrsNewStyle);
                _longitude = _mapPoint.X;
                _latitude = _mapPoint.Y;
            }
            else if (SelectedFormat == CoordinateFormat.UTM)
            {
                _mapPoint = MapPointBuilderEx.FromGeoCoordinateString(_utm.GeoCoordinateString, SpatialReferences.WGS84, GeoCoordinateType.UTM); 
                _longitude = _mapPoint.X;
                _latitude = _mapPoint.Y;
            }
            else  // Handle decimal degrees formats
            {
                _mapPoint = MapPointBuilderEx.CreateMapPoint(_longitude, _latitude, SpatialReferences.WGS84);
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
				Camera newCamera = new(x: _longitude, y: _latitude, scale: Scale, heading: 0, spatialReference: SpatialReferences.WGS84);
				mapView.ZoomTo(newCamera, TimeSpan.Zero);
			}

			// Otherwise, just update the coordinates & scale of the existing camera
			else
			{
				camera.X = _longitude;
				camera.Y = _latitude;
				camera.Scale = _scale;
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
					MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate2D(_longitude, _latitude), SpatialReferences.WGS84);
					CIMGraphic graphic = GraphicFactory.Instance.CreateSimpleGraphic(geometry: point, symbol: symbol);

					// Create the point container inside the group layer container and place graphic into it to create graphic element
					GraphicsLayerCreationParams lyrParams = new() { Name = $"{_display}" };
					GraphicsLayer pointGraphicContainer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(layerParams: lyrParams, container: groupLyrContainer);
					ElementFactory.Instance.CreateGraphicElement(elementContainer: pointGraphicContainer, cimGraphic: graphic, select: false);

					// Finally create a point label graphic & add it to the point container
					CIMTextGraphic label = new()
					{
						Symbol = SymbolFactory.Instance.ConstructTextSymbol(color: GetColor(_settings.FontColor), size:12, fontFamilyName:_settings.FontFamily, fontStyleName: _settings.FontStyle).MakeSymbolReference(),
						Text = $"    <b>{_display}</b>",
						Shape = point
					};

					pointGraphicContainer.AddElement(cimGraphic:label, select: false);
				}

				// 3D Map (adds an overlay without a label, since CIMTextGraphic is a graphic & graphics are 2D only. Therefore, there isn't a graphics container in the ArcGIS Pro Table of Contents).
				else if (map.IsScene)
				{
					MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate3D(x: _longitude, y: _latitude, z: 0), sr);
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
