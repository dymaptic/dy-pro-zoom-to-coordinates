namespace dymaptic.Pro.ZoomToCoordinates.Models;

/// <summary>
///     Stores UTM or MGRS information (note: MGRS is an extension of UTM).
/// </summary>
public class GridSRBaseItem
{
    /// <summary>
    ///     The UTM zone.
    /// </summary>
    public int Zone { get; set; }

    /// <summary>
    ///     UTM and MGRS stores latitude band, one of "CDEFGHJKLMNPQRSTUVWXX" Excludes 'I' and 'O' (1 character total) 
    /// </summary>
    public string LatitudeBand { get; set; } = "";
    
    /// <summary>
    ///     The Easting (X-coordinate value) which is a positive number with a maximum of 6 digits when UTM (5 max for MGRS).
    /// </summary>
    public int Easting { get; set; }

    /// <summary>
    ///     The Northing (Y-coordinate value) which is a positive number with a maximum of 7 digits when UTM (5 max for MGRS).
    /// </summary>
    public int Northing { get; set; }

    /// <summary>
    ///     Can be used to easily convert from one coordinate format to another (it doesn't include spaces).
    /// </summary>
    public string GeoCoordinateString { get; set; } = "";
}
