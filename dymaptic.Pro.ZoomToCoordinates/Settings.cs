using dymaptic.Pro.ZoomToCoordinates.Models;

namespace dymaptic.Pro.ZoomToCoordinates;

public class Settings
{
    // Default settings when Add-In loaded for initial time; user's actual default choices as they modify the drop-downs get saved/updated by EventHandler in ZoomToCoordinatesModule.cs 
    public double Longitude { get; set; } = -122.4774494;
    public double Latitude { get; set; } = 37.8108275;
    public bool ShowFormattedCoordinates { get; set; } = false;
    public double Scale { get; set; } = 10_000;
    public bool ShowGraphic { get; set; } = false;
    public string Marker { get; set; } = "Pushpin";
    public string MarkerColor { get; set; } = "Green";
    public int MarkerSize { get; set; } = 15;
    public string FontFamily { get; set; } = "Tahoma";
    public int FontSize { get; set; } = 11;
    public string FontStyle { get; set; } = "Regular";
    public string FontColor { get; set; } = "Black";

    // Note: user doesn't get the option to change the default CoordinateFormat 
    public CoordinateFormat CoordinateFormat { get; } = CoordinateFormat.DecimalDegrees;
}
