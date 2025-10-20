using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using dymaptic.Pro.ZoomToCoordinates.Models;
using System.Linq;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

public class GetCoordinatesViewModel : CoordinatesBaseViewModel
{
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

        OpenSettingsCommand = new RelayCommand(() =>
        {
            OpenSettings();
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
    ///     A tooltip is only provided for the GetCoordinates window if the MapTool is deactivated, providing instructions to the user how to reactivaate it.
    /// </summary>
    public string Tooltip
    {
        get => _tooltip;
        set => SetProperty(ref _tooltip, value);
    }

    /// <summary>
    ///     Tracks whether the MapTool is activated or not.
    /// </summary>
    public bool Activated
    {
        get => _activated;
        set
        {
            if (SetProperty(ref _activated, value))
            {
                Tooltip = _activated
                      ? "Move the cursor to get coordinates."
                      : "Tool is deactivated. To reactivate it, go to the dymaptic tab and click the \"Get Coordinates\" button on the toolbar.";
            } 
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
                _longLatDD.Update(_mapPoint);
                break;

            case CoordinateFormat.DegreesMinutesSeconds:
                _longLatDMS.Update(_mapPoint); 
                break;

            case CoordinateFormat.DegreesDecimalMinutes:
                _longLatDDM.Update(_mapPoint);
                break;

            case CoordinateFormat.MGRS:
                _mgrs.Update(_mapPoint);
                break;

            case CoordinateFormat.UTM:
                _utm.Update(_mapPoint);
                break;
        }
        UpdateDisplay();
    }

    private bool _enableFormatting;
    private bool _activated;
    private string _tooltip = "";
}
