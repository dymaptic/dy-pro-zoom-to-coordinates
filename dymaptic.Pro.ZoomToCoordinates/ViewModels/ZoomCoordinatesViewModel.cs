using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using dymaptic.Pro.ZoomToCoordinates.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class ZoomCoordinatesViewModel : CoordinatesBaseViewModel, IDataErrorInfo
{
    private static readonly char[] separator = [' '];
    private bool _xCoordinateValidated = true;  // when tool loads, valid coordinates are put into the text boxes
    private bool _yCoordinateValidated = true;
    private string _xErrorMessage = "";
    private string _yErrorMessage = "";
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

    public ObservableCollection<string> Hemispheres { get; } = ["Northern", "Southern"];

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

                    // Display Latitude above Longitude to follow convention folks are used to
                    XRowIndex = 4;
                    YRowIndex = 3;
                    break;

                case CoordinateFormat.MGRS:
                    _mgrs.Update(_mapPoint!);

                    // Track initial value
                    string initialOneHundredKMGridId = _oneHundredKMGridID;

                    _selectedUTMZone = _mgrs.Zone;
                    _selectedLatitudeBand = _mgrs.LatitudeBand;
                    _selectedLatitudeBandItem = LatitudeBands.First(band => band.Key == _selectedLatitudeBand);
                    _oneHundredKMGridID = _mgrs.OneHundredKMGridID;

                    // If OneHundredKMGridID changed, we know we need to update the possibilities
                    if (initialOneHundredKMGridId != _oneHundredKMGridID)
                    {
                        UpdateMgrsGridIds();
                    }

                    XRowIndex = 3;
                    YRowIndex = 4;
                    break;

                case CoordinateFormat.UTM:
                    _utm.Update(_mapPoint!);
                    _selectedUTMZone = _utm.Zone;
                    _selectedLatitudeBand = _utm.LatitudeBand;
                    _selectedLatitudeBandItem = LatitudeBands.First(band => band.Key == _selectedLatitudeBand);

                    XRowIndex = 3;
                    YRowIndex = 4;
                    break;

                default:
                    break;
            }

            // Ensure UI updates without calling the setter logic for any of these
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

            SetProperty(ref _xCoordinateString, value);
            _xCoordinateValidated = ValidateCoordinate(_xCoordinateString, CoordinateAxis.X);
            if (_xCoordinateValidated)
            {
                XErrorMessage = "";
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
            else
            {
                XErrorMessage = CreateErrorMessage(CoordinateAxis.X);
            }
        }
    }

    public string XErrorMessage
    {
        get => _xErrorMessage;
        set => SetProperty(ref _xErrorMessage, value);
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

            SetProperty(ref _yCoordinateString, value);
            _yCoordinateValidated = ValidateCoordinate(value, CoordinateAxis.Y);
            if (_yCoordinateValidated)
            {
                YErrorMessage = "";
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
                            _utm.Northing = int.Parse(_yCoordinateString);
                            break;
                        default:
                            break;
                    }

                    UpdateMapPoint();
                    UpdateDisplay();
                }
            }
            else
            {
                YErrorMessage = CreateErrorMessage(CoordinateAxis.Y);
            }
        }
    }

    public string YErrorMessage
    {
        get => _yErrorMessage;
        set => SetProperty(ref _yErrorMessage, value);
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

    private int _xRowIndex = 4;
    public int XRowIndex
    {
        get => _xRowIndex;
        set => SetProperty(ref _xRowIndex, value);
    }

    private int _yRowIndex = 3;
    public int YRowIndex
    {
        get => _yRowIndex;
        set => SetProperty(ref _yRowIndex, value);
    }

    /// <summary>
    ///     (IDataErrorInfo interface)
    /// </summary>
    public string Error => null;  // not used

    /// <summary>
    ///     (IDataErrorInfo interface)
    /// </summary>
    /// <param name="columnName"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public string? this[string columnName]
    {
        get
        {
            if (columnName == nameof(XCoordinateString))
            {
                return string.IsNullOrEmpty(XErrorMessage) ? null : XErrorMessage;
            }
            else if (columnName == nameof(YCoordinateString))
            {
                return string.IsNullOrEmpty(YErrorMessage) ? null : YErrorMessage;
            }
            return null;
        }
    }

    private string CreateErrorMessage(CoordinateAxis axis)
    {
        string errorMessage = "";

        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                if (axis == CoordinateAxis.X)
                {
                    errorMessage = ShowFormattedCoordinates
                        ? "Invalid longitude value. Valid example: -107.23433° or -107.23433"
                        : "Invalid longitude value. Valid example: -107.23433";
                }
                else
                {
                    errorMessage = ShowFormattedCoordinates
                        ? "Invalid latitude value. Valid example: 43.23433° or 43.23433"
                        : "Invalid latitude value. Valid example: 43.23433";
                }
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                if (axis == CoordinateAxis.X)
                {
                    errorMessage = ShowFormattedCoordinates
                        ? "Invalid longitude (DMS) value. Valid example: 107° 14' 3\"W or 107 14 3"
                        : "Invalid longitude (DMS) value. Valid example: 107 14 3";
                }
                else
                {
                    errorMessage = ShowFormattedCoordinates
                        ? "Invalid latitude (DMS) value. Valid example: 43° 12' 5\"N or 43 12 5"
                        : "Invalid latitude (DMS) value. Valid example: 43 12 5";
                }
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                if (axis == CoordinateAxis.X)
                {
                    errorMessage = ShowFormattedCoordinates
                        ? "Invalid longitude (DDM) value. Valid example: 107° 14.050'W or 107 14.050"
                        : "Invalid longitude (DDM) value. Valid example: 107 14.050";
                }
                else
                {
                    errorMessage = ShowFormattedCoordinates
                        ? "Invalid latitude (DDM) value. Valid example: 43° 12.567'N or 43 12.567"
                        : "Invalid latitude (DDM) value. Valid example: 43 12.567";
                }
                break;

            case CoordinateFormat.MGRS:
                if (axis == CoordinateAxis.X)
                {
                    errorMessage = "Invalid MGRS Easting. Valid example: 44825";
                }
                else
                {
                    errorMessage = "Invalid MGRS Northing. Valid example: 44825";
                }
                break;

            case CoordinateFormat.UTM:
                if (axis == CoordinateAxis.X)
                {
                    errorMessage = "Invalid UTM Easting. Valid example: 448251";
                }
                else
                {
                    errorMessage = "Invalid UTM Northing. Valid example: 5412345";
                }
                break;

            default:
                break;
        }
        return errorMessage;
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
            foreach (var id in MgrsItem.GetMgrsGridIdsObservable(_selectedUTMZone, _selectedLatitudeBand))
            {
                _mgrsGridIds.Add(id);
            }
        }
        else
        {
            // flag to prevent OneHundredKMGridID setter logic (it's the selected MgrsGridId in that observable collection).
            _isUpdatingGridIds = true;
            _mgrsGridIds.Clear();
            foreach (var id in MgrsItem.GetMgrsGridIdsObservable(_selectedUTMZone, _selectedLatitudeBand))
            {
                _mgrsGridIds.Add(id);
            }
            // Set selected value back to what it was (combobox will now be updated with valid possibilities).
            _oneHundredKMGridID = _mgrsGridIds.FirstOrDefault(x => x.Equals(_mgrs.OneHundredKMGridID)) ?? "";
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
        string cleanedLatLongValue = LongLatItem.CleanLatLongCoordinateString(coordinateValue, axis);

        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
                return ValidateDecimalDegrees(cleanedLatLongValue, axis);

            case CoordinateFormat.DegreesDecimalMinutes:
                return ValidateDegreesDecimalMinutes(cleanedLatLongValue, axis);

            case CoordinateFormat.DegreesMinutesSeconds:
                return ValidateDegreesMinutesSeconds(cleanedLatLongValue, axis);

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
    /// <returns></returns>
    private bool ValidateDecimalDegrees(string value, CoordinateAxis axis)
    {
        if (!double.TryParse(value, out double degrees))
            return false;

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
    /// <returns></returns>
    private bool ValidateDegreesDecimalMinutes(string value, CoordinateAxis axis)
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
    private bool ValidateDegreesMinutesSeconds(string value, CoordinateAxis axis)
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
