using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using dymaptic.Pro.ZoomToCoordinates.Models;
using System.Linq;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class GetCoordinatesViewModel : CoordinatesBaseViewModel
{
    private bool _enableFormatting;

    // Constructor
    public GetCoordinatesViewModel()
    {
        Settings settings = ZoomToCoordinatesModule.GetSettings();
        SelectedFormat = settings.CoordinateFormat;
        _selectedFormatItem = CoordinateFormats.First(f => f.Format == settings.CoordinateFormat);
        UpdateCoordinateLabels();

        // Bind the command
        CopyTextCommand = new RelayCommand(() =>
        {
            CopyText();
        });
    }

    public CoordinateFormatItem SelectedFormatItem
    {
        get => _selectedFormatItem;
        set
        {
            SetProperty(ref _selectedFormatItem, value);
            SelectedFormat = value.Format;

            UpdateCoordinateLabels();
            UpdateCoordinates();
        }
    }

    /// <summary>
    ///     Formatting isn't enabled until the user has clicked the map to avoid the settings Lat/Long values being 
    ///     populated if user clicks the format checkbox prior to clicking the map.
    /// </summary>
    public bool EnableFormatting
    {
        get => _enableFormatting;
        set => SetProperty(ref _enableFormatting, value);
    }

    /// <summary>
    ///     The MapPoint that is created when the user clicks that map.
    /// </summary>
    public MapPoint? MapPoint
    {
        get => _mapPoint;
        set
        {
            SetProperty(ref _mapPoint, value);
            EnableFormatting = true;
        }
    }

    /// <summary>
    ///     The Y coordinate value as a string.
    /// </summary>
    public override string YCoordinateString
    {
        get => _yCoordinateString;
        set => SetProperty(ref _yCoordinateString, value);
    }

    /// <summary>
    ///     The X coordinate value as a string.
    /// </summary>
    public override string XCoordinateString
    {
        get => _xCoordinateString;
        set => SetProperty(ref _xCoordinateString, value);
    }

    public void UpdateCoordinates()
    {
        // Exit early if user hasn't clicked the map
        if (_mapPoint == null) return;

        switch (SelectedFormat)
        {
            case CoordinateFormat.DecimalDegrees:
            case CoordinateFormat.DegreesMinutesSeconds:
            case CoordinateFormat.DegreesDecimalMinutes:
                _longLatItem.Update(MapPoint!);
                break;


            case CoordinateFormat.MGRS:
                _mgrs.Update(MapPoint!);
                break;

            case CoordinateFormat.UTM:
                _utm.Update(MapPoint!);
                break;
        }
        UpdateDisplay();
    }
}
