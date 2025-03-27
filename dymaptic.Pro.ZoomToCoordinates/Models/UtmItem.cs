using ArcGIS.Core.Geometry;

namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class UtmItem : GridBaseItem
{
    private readonly ToGeoCoordinateParameter utmParam = new(geoCoordType: GeoCoordinateType.UTM);

    // Default constructor
    public UtmItem()
        :base(1, "C", 0, 0) { }


    public UtmItem(int zone, string latitudeBand, int easting, int northing)
        : base(zone, latitudeBand, easting, northing) 
    {
        UpdateGeoCoordinateString();
    }

    /// <summary>
    ///     The UTM zone.
    /// </summary>
    public int Zone
    {
        get => _zone;
        set
        {
            if (_zone != value)
            {
                _zone = value;
                UpdateGeoCoordinateString();
            }
        }
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

    /// <summary>
    ///     Updates the UtmItem using the most recent MapPoint information.
    /// </summary>
    /// <param name="mapPoint"></param>
    public void Update(MapPoint mapPoint)
    {
        string geoCoordString = mapPoint.ToGeoCoordinateString(utmParam);

        string[] parts = geoCoordString.Split(" ");
        _zone = int.Parse(parts[0][..2]);
        _latitudeBand = parts[0][2..3];
        _easting = int.Parse(parts[1]);
        _northing = int.Parse(parts[2]);
        _geoCoordinateString = geoCoordString.Replace(" ", "");
    }

    protected override void UpdateGeoCoordinateString()
    {
        _geoCoordinateString = $"{Zone}{LatitudeBand}{Easting:D6}{Northing:D7}";

        MapPoint = MapPointBuilderEx.FromGeoCoordinateString(_geoCoordinateString, SpatialReferences.WGS84, GeoCoordinateType.UTM);
        Update(MapPoint);
    }
}
