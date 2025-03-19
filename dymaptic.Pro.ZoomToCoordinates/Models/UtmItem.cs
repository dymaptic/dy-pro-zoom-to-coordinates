namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class UtmItem : GridSRBaseItem
{
    // Default constructor
    public UtmItem()
        :base(0, "A", 0, 0) { }


    public UtmItem(int zone, string latitudeBand, int easting, int northing)
        : base(zone, latitudeBand, easting, northing) 
    {
        UpdateGeoCoordinateString();
    }

    /// <summary>
    ///     A friendly view of the UtmItem that includes spaces.
    /// </summary>
    public string Display => $"{Zone}{LatitudeBand} {Easting:D6} {Northing:D7}";

    public override string GeoCoordinateString
    {
        get => _geoCoordinateString;
        set => _geoCoordinateString = value;
    }

    protected override void UpdateGeoCoordinateString()
    {
        _geoCoordinateString = $"{Zone}{LatitudeBand}{Easting:D7}{Northing:D7}";
    }
}
