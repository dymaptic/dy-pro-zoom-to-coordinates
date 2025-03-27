using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using dymaptic.Pro.ZoomToCoordinates.Models;
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
    private bool _xCoordinateValidated = true;  // when tool loads, valid coordinates are put into the text boxes
    private bool _yCoordinateValidated = true;
    private string _errorMessage = "";
    private int _selectedUTMZone;
    private LatitudeBand _selectedLatitudeBandItem;
    private string _selectedLatitudeBand = "";
    private string _oneHundredKMGridID = "";
    private bool _showUtmControls;
    private bool _showMgrsControl;
    private double _scale = _settings.Scale;
    private bool _isUpdatingGridIds = false;

    /// <summary>
    ///     Regardless of selected coordinate format, we ALWAYS store a longitude value in decimal degrees.
    /// </summary>
    private double _longitude = _settings.Longitude;

    /// <summary>
    ///     Regardless of selected coordinate format, we ALWAYS store a latitude value in decimal degrees.
    /// </summary>
    private double _latitude = _settings.Latitude;

    public ICommand ZoomCommand { get; }

    // Constructor
    public ZoomCoordinatesViewModel()
    {
        // Create a MapPoint right off the bat with the default coordinates
        _longLatItem.Update(_longitude, _latitude);
        _mapPoint = _longLatItem.MapPoint;

        // Set initially for the Display
        _xCoordinateString = _longLatItem.Longitude.ToString("F6");
        _yCoordinateString = _longLatItem.Latitude.ToString("F6");

        // Prevent null reference
        _selectedLatitudeBandItem = LatitudeBands.First(band => band.Key == "C");

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
    public ObservableCollection<int> UTMZones { get; } = [.. Enumerable.Range(1, 60)];

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

    /// <summary>
    ///     The MapPoint of the current coordinates. 
    ///     In the ViewModel, it's populated by the individual Coordinate Format classes, so we know it will never be null.
    /// </summary>
    public MapPoint MapPoint
    {
        get => _mapPoint!;
        set
        {
            _mapPoint = value;
            _longitude = _mapPoint.X;
            _latitude = _mapPoint.Y;
        }
    }

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
                case CoordinateFormat.DegreesMinutesSeconds:
                case CoordinateFormat.DegreesDecimalMinutes:
                    _longLatItem.Update(_mapPoint!);
                    break;

                case CoordinateFormat.MGRS:
                    _mgrs.Update(_mapPoint!);
                    _selectedUTMZone = _mgrs.Zone;
                    _selectedLatitudeBand = _mgrs.LatitudeBand;
                    _selectedLatitudeBandItem = LatitudeBands.First(band => band.Key == _selectedLatitudeBand);
                    _oneHundredKMGridID = _mgrs.OneHundredKMGridID;
                    UpdateMgrsGridIds();
                    break;

                case CoordinateFormat.UTM:
                    _utm.Update(_mapPoint!);
                    _selectedUTMZone = _utm.Zone;
                    _selectedLatitudeBand = _utm.LatitudeBand;
                    _selectedLatitudeBandItem = LatitudeBands.First(band => band.Key == _selectedLatitudeBand);
                    break;

                default:
                    break;
            }

            // Don't call the setter logic for any of these
            NotifyPropertyChanged(nameof(SelectedUTMZone));
            NotifyPropertyChanged(nameof(SelectedLatitudeBandItem));
            NotifyPropertyChanged(nameof(XCoordinateToolTip));
            NotifyPropertyChanged(nameof(YCoordinateToolTip));

            UpdateDisplay();
        }
    }

    /// <summary>
    ///     Selected UTM Zone (visible for UTM and MGRS only)
    /// </summary>
    public int SelectedUTMZone
    {
        get => _selectedUTMZone;
        set
        {
            if (_selectedUTMZone == value) return;
            
            SetProperty(ref _selectedUTMZone, value);
            if (_xCoordinateValidated && _yCoordinateValidated)
            {
                switch (SelectedFormat)
                {
                    case CoordinateFormat.MGRS:
                        _mgrs.Zone = _selectedUTMZone;
                        if (_mgrs.ErrorMessage != String.Empty)
                        {
                            ErrorMessage = _mgrs.ErrorMessage;
                        }
                        else
                        {
                            ErrorMessage = String.Empty;
                            UpdateMgrsGridIds();
                        }
                        break;
                    case CoordinateFormat.UTM:
                        _utm.Zone = _selectedUTMZone;
                        break;
                    default:
                        break;
                }

                UpdateMapPoint();
                UpdateDisplay();
            }   
        }
    }

    public LatitudeBand SelectedLatitudeBandItem
    {
        get => _selectedLatitudeBandItem;
        set
        {
            SetProperty(ref _selectedLatitudeBandItem, value);
            SelectedLatitudeBand = _selectedLatitudeBandItem.Key;
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
            if (_selectedLatitudeBand == value) return;

            SetProperty(ref _selectedLatitudeBand, value);
            switch (SelectedFormat)
            {
                case CoordinateFormat.MGRS:
                    _mgrs.LatitudeBand = _selectedLatitudeBand;
                    if (_mgrs.ErrorMessage != String.Empty)
                    {
                        ErrorMessage = _mgrs.ErrorMessage;
                    }
                    else
                    {
                        ErrorMessage = String.Empty;
                        UpdateMgrsGridIds();
                    }
                    break;
                case CoordinateFormat.UTM:
                    _utm.LatitudeBand = _selectedLatitudeBand;
                    break;
                default:
                    break;
            }

            UpdateMapPoint();
            UpdateDisplay();
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
            // Avoid setter logic if the value doesn't change and while the MgrsGridIds ObservableCollection is updating.
            if (_oneHundredKMGridID == value || _isUpdatingGridIds) return;

            SetProperty(ref _oneHundredKMGridID, value);
            if (_xCoordinateValidated && _yCoordinateValidated)
            {
                _mgrs.OneHundredKMGridID = _oneHundredKMGridID;
                UpdateMapPoint();
                UpdateDisplay();
            }
        }
    }

    /// <summary>
    ///     The Longitude value for DD/DDM/DMS or Easting value for UTM/MGRS.
    /// </summary>
    public override string XCoordinateString
    {
        get => _xCoordinateString;
        set
        {
            if (_xCoordinateString == value) return;

            _xCoordinateValidated = ValidateCoordinate(value, CoordinateAxis.X);
            if (_xCoordinateValidated)
            {
                SetProperty(ref _xCoordinateString, value);
                if (_yCoordinateValidated)
                {
                    switch (SelectedFormat)
                    {
                        case CoordinateFormat.DecimalDegrees:
                        case CoordinateFormat.DegreesMinutesSeconds:
                        case CoordinateFormat.DegreesDecimalMinutes:
                            _longLatItem.Update(_longitude, _latitude);
                            break;

                        case CoordinateFormat.MGRS:
                            _mgrs.Easting = int.Parse(_xCoordinateString);
                            break;

                        case CoordinateFormat.UTM:
                            _utm.Easting = int.Parse(_xCoordinateString);
                            break;
                        default:
                            break;
                    }

                    UpdateMapPoint();
                    UpdateDisplay();
                }
            }
        }
    }

    /// <summary>
    ///     The Latitude value for DD/DDM/DMS or Northing value for UTM/MGRS.
    /// </summary>
    public override string YCoordinateString
	{
		get => _yCoordinateString;
		set
		{
            if (_yCoordinateString == value) return;

            _yCoordinateValidated = ValidateCoordinate(value, CoordinateAxis.Y);
			if (_yCoordinateValidated)
			{
                SetProperty(ref _yCoordinateString, value);
				if (_xCoordinateValidated)
				{
                    switch (SelectedFormat)
                    {
                        case CoordinateFormat.DecimalDegrees:
                        case CoordinateFormat.DegreesMinutesSeconds:
                        case CoordinateFormat.DegreesDecimalMinutes:
                            _longLatItem.Update(_longitude, _latitude);
                            break;

                        case CoordinateFormat.MGRS:
                            _mgrs.Northing = int.Parse(_yCoordinateString);
                            break;

                        case CoordinateFormat.UTM:
                            _utm.Northing= int.Parse(_yCoordinateString);
                            break;
                        default:
                            break;
                    }

                    UpdateMapPoint();
                    UpdateDisplay();
                }
            }
		}
	}

    public string XCoordinateToolTip =>
        SelectedFormat switch
        {
            CoordinateFormat.MGRS or CoordinateFormat.UTM => 
                "Easting coordinate in meters. Automatically adjusts and may change UTM zones when crossing zone boundaries.",

            CoordinateFormat.DecimalDegrees => 
                "Longitude coordinate in decimal degrees (e.g., -120.5 or 120.5 W)",

            CoordinateFormat.DegreesMinutesSeconds => 
                "Longitude coordinate in degrees minutes seconds (e.g., -120° 30' 15\" W)",

            CoordinateFormat.DegreesDecimalMinutes => 
                "Longitude coordinate in degrees decimal minutes (e.g., -120° 30.5' W)",

            _ => "X coordinate value."  // Default case to ensure a return value
        };

    public string YCoordinateToolTip =>
        SelectedFormat switch
        {
            CoordinateFormat.MGRS or CoordinateFormat.UTM =>
                "Northing coordinate in meters. Automatically adjusts and may change Latitude Band when crossing band boundaries.",

            CoordinateFormat.DecimalDegrees =>
                "Latitude coordinate in decimal degrees (e.g., 43.5 or 43.5° N)",

            CoordinateFormat.DegreesMinutesSeconds =>
                "Latitude coordinate in degrees minutes seconds (e.g., 43 30 0 or 43° 30' 0.00\" N)",

            CoordinateFormat.DegreesDecimalMinutes =>
                "Latitude coordinate in degrees decimal minutes (e.g., 43 30 or 43° 30.00' N)",

            _ => "Y coordinate value."  // Default case to ensure a return value
        };

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
    ///     Gets the 100 KM MGRS Grid ID possibilities given a UTM Zone and latitude band.
    /// </summary>
    /// <param name="utmZone">The UTM Zone value of 1-60.</param>
    /// <param name="latitudeBand">The Latitude Band letter, C-X excluding I and O.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static ObservableCollection<string> GetMgrsGridIds(int utmZone, string latitudeBand)
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

    /// <summary>
    ///     Updates the MapPoint in the ZoomCoordinatesViewModel from the MapPoint from the various coordinate classes (LongLatItem, MGRS or UTM).
    /// </summary>
    /// <returns></returns>
    private void UpdateMapPoint()
    {
        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
            case CoordinateFormat.DegreesMinutesSeconds:
            case CoordinateFormat.DegreesDecimalMinutes:
                MapPoint = _longLatItem.MapPoint;
                break;

            case CoordinateFormat.MGRS:
                MapPoint = _mgrs.MapPoint;

                // Trigger UI refreshes only as necessary
                if (_mgrs.Zone != _selectedUTMZone)
                {
                    _selectedUTMZone = _mgrs.Zone;
                    UpdateMgrsGridIds();
                    NotifyPropertyChanged(nameof(SelectedUTMZone));
                }
                if (_mgrs.LatitudeBand != _selectedLatitudeBand)
                {
                    UpdateMgrsGridIds();
                    SelectedLatitudeBandItem = LatitudeBands.Where(x => x.Key == _mgrs.LatitudeBand).First();
                }
                if (_mgrs.OneHundredKMGridID != _oneHundredKMGridID)
                {
                    _oneHundredKMGridID = _mgrs.OneHundredKMGridID;
                    NotifyPropertyChanged(nameof(OneHundredKMGridID));
                }
                if (_mgrs.Easting != int.Parse(_xCoordinateString))
                {
                    _xCoordinateString = _mgrs.Easting.ToString();
                    NotifyPropertyChanged(nameof(XCoordinateString));
                }
                if (_mgrs.Northing != int.Parse(_yCoordinateString))
                {
                    _yCoordinateString = _mgrs.Northing.ToString();
                    NotifyPropertyChanged(nameof(YCoordinateString));
                }
                break;

            case CoordinateFormat.UTM:
                MapPoint = _utm.MapPoint;

                // Trigger UI refreshes only as necessary
                if (_utm.Zone != _selectedUTMZone)
                {
                    _selectedUTMZone = _utm.Zone;
                    NotifyPropertyChanged(nameof(SelectedUTMZone));
                }
                if (_utm.LatitudeBand != _selectedLatitudeBand)
                {
                    SelectedLatitudeBandItem = LatitudeBands.Where(x => x.Key == _utm.LatitudeBand).First();
                }
                if (_utm.Easting != int.Parse(_xCoordinateString))
                {
                    _xCoordinateString = _utm.Easting.ToString();
                    NotifyPropertyChanged(nameof(XCoordinateString));
                }
                if (_utm.Northing != int.Parse(_yCoordinateString))
                {
                    _yCoordinateString = _utm.Northing.ToString();
                    NotifyPropertyChanged(nameof(YCoordinateString));
                }
                break;

            default:
                break;
        }
    }

    /// <summary>
    ///     Updates the MGRS 100 KM Grid ID possibilities given the selected UTM zone and selected latitude band.
    /// </summary>
    private void UpdateMgrsGridIds()
    {
        if (_mgrsGridIds.Count == 0)
        {
            foreach (var id in GetMgrsGridIds(_selectedUTMZone, _selectedLatitudeBand))
            {
                _mgrsGridIds.Add(id);
            }
        }
        else
        {
            // flag to prevent OneHundredKMGridID setter logic (it's the selected MgrsGridId in that observable collection).
            _isUpdatingGridIds = true;
            _mgrsGridIds.Clear();
            foreach (var id in GetMgrsGridIds(_selectedUTMZone, _selectedLatitudeBand))
            {
                _mgrsGridIds.Add(id);
            }

            // Set selected value back to what it was (combobox will now be updated with valid possibilities).
            _oneHundredKMGridID = _mgrs.OneHundredKMGridID;
            _isUpdatingGridIds = false;
        }
        NotifyPropertyChanged(nameof(OneHundredKMGridID));
    }

    /// <summary>
    ///     Validates coordinate according to which coordinate system format is selected.
    /// </summary>
    /// <param name="coordinateValue">The coordinate string (it can be in one of five formats: DD, DDM, DMS, MGRS, or UTM).</param>
    /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
    /// <returns></returns>
    private bool ValidateCoordinate(string coordinateValue, CoordinateAxis axis)
    {
        if (string.IsNullOrWhiteSpace(coordinateValue))
            return false;

        // Only DD/DDM/DDS allow non-numeric characters
        bool isNegative = false;
        string cleanedLatLongValue = LongLatItem.CleanLatLongCoordinateString(coordinateValue, axis, ref isNegative);

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
                    if (axis == CoordinateAxis.X)
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
    ///     Validates a decimal degrees coordinate string.
    /// </summary>
    /// <param name="value">The latitude or longitude value as a string.</param>
    /// <param name="axis">"X" or "Y" for Longitude and Latitude respectively.</param>
    /// <param name="isNegative">If <c>true</c>, the value is converted to a negative number.</param>
    /// <returns></returns>
    private bool ValidateDecimalDegrees(string value, CoordinateAxis axis, bool isNegative)
    {
        if (!double.TryParse(value, out double degrees))
            return false;

        if (isNegative)
            degrees *= -1;

        if (!LongLatItem.IsValidDecimalDegree(degrees, axis))
            return false;

        if (axis == CoordinateAxis.X)
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
    private bool ValidateDegreesDecimalMinutes(string value, CoordinateAxis axis, bool isNegative)
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

        if (!LongLatItem.IsValidDecimalDegree(decimalDegrees, axis))
            return false;

        if (axis == CoordinateAxis.X)
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
    private bool ValidateDegreesMinutesSeconds(string value, CoordinateAxis axis, bool isNegative)
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

        if (!LongLatItem.IsValidDecimalDegree(decimalDegrees, axis))
            return false;

        if (axis == CoordinateAxis.X)
            _longitude = decimalDegrees;
        else
            _latitude = decimalDegrees;

        return true;
    }

    private async Task ZoomToCoordinates()
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

			if (ShowGraphic)
			{
                CreateGraphic();
			}
		});
	}
}
