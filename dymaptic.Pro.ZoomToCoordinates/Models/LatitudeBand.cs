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

    public string DisplayText => $"{Key}: {Value}";  // Format for View
}
