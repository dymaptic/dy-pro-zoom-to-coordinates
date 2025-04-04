namespace dymaptic.Pro.ZoomToCoordinates.Models;

/// <summary>
///     Class to support a friendly description of latitude bands in the View's combobox; e.g., "C: -80° to -72°"
/// </summary>
public class LatitudeBand
{
    /// <summary>
    ///     One of the following letters "CDEFGHJKLMNPQRSTUVWXX" - excludes 'I' and 'O'.
    /// </summary>
    public string Key { get; set; } = "";

    /// <summary>
    ///     The latitude band range associated with the particular key.
    /// </summary>
    public string Value { get; set; } = "";

    /// <summary>
    ///     Latitude bands occur in the northern and southern hemisphere. If the latitude band is 32 to 40 degrees northern hemisphere,
    ///     the OppositeHemisphereKey is the Key that corresponds to the -32 to -40 degrees latitude band in the southern hemisphere.
    /// </summary>
    public string OppositeHemisphereKey { get; set; } = "";

    public string DisplayText => $"{Key}: {Value}";  // Format for View
}
