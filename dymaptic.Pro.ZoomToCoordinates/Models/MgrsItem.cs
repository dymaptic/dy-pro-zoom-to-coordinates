namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class MgrsItem : GridSRBaseItem
{
    /// <summary>
    ///     The 100 KM Square ID (2 characters total)
    /// </summary>
    private string _mgrSquareId = "";
    public string MGRSquareID 
    {
        get => _mgrSquareId;
        set
        {
            if (_mgrSquareId != value)
            {
                _mgrSquareId = value;
                UpdateGeoCoordinateString();
            }
        }
    }

    /// <summary>
    ///     A friendly view of the MgrsItem that includes spaces.
    /// </summary>
    public string Display => $"{Zone}{LatitudeBand}{MGRSquareID} {Easting:D5} {Northing:D5}";

    public override string GeoCoordinateString
    {
        get => _geoCoordinateString;
        set => _geoCoordinateString = value;
    }
    protected override void UpdateGeoCoordinateString()
    {
        _geoCoordinateString = $"{Zone}{LatitudeBand}{MGRSquareID}{Easting:D5}{Northing:D5}";
    }
}