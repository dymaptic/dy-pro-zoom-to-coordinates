namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class MgrsItem : GridSRBaseItem
{
    /// <summary>
    ///     The 100 KM Square ID (2 characters total)
    /// </summary>
    public string MGRSquareID { get; set; } = "";

    /// <summary>
    ///     A friendly view of the MgrsItem that includes spaces.
    /// </summary>
    public string Display => $"{Zone}{LatitudeBand}{MGRSquareID} {Easting:D5} {Northing:D5}";
}