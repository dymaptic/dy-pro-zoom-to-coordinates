using dymaptic.Pro.ZoomToCoordinates.Models;

namespace dymaptic.Pro.ZoomToCoordinates;

public class Settings
{
    // Default settings when Add-In loaded for initial time; user's actual default choices as they modify the drop-downs get saved/updated by EventHandler in ZoomToCoordinatesModule.cs 
    public double Longitude = -122.4774494;
    public double Latitude = 37.8108275;
    public bool ShowFormattedCoordinates = false;
    public double Scale = 10_000;
    public bool ShowGraphic = false;
    public string Marker = "Pushpin";
    public string MarkerColor = "Green";
    public int MarkerSize = 15;
    public string FontFamily = "Tahoma";
    public int FontSize = 11;
    public string FontStyle = "Regular";
    public string FontColor = "Black";

    // Note: user doesn't get the option to change the default CoordinateFormat 
    public CoordinateFormat CoordinateFormat { get; } = CoordinateFormat.DecimalDegrees;
}
