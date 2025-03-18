namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class UtmItem : GridSRBaseItem
{
    /// <summary>
    ///     A friendly view of the UtmItem that includes spaces.
    /// </summary>
    public string Display => $"{Zone}{LatitudeBand} {Easting:D6} {Northing:D7}";
}
