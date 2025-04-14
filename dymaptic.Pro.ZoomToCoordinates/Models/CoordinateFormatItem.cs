namespace dymaptic.Pro.ZoomToCoordinates.Models;

/// <summary>
///     Provides us the ability to store both the CoordinateFormat enum as well as a friendlier version of it that includes
///     spaces for the View (e.g., CoordinateFormat.DecimalDegrees and "Decimal Degrees").
/// </summary>
public class CoordinateFormatItem
{
    public CoordinateFormat Format { get; set; }
    public string DisplayName { get; set; } = "";

    public override string ToString()
    {
        return DisplayName!;
    }
}
