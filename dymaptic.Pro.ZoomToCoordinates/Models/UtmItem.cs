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
    public override int Zone
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
    ///     One of "CDEFGHJKLMNPQRSTUVWXX" Excludes 'I' and 'O' (1 character total) 
    /// </summary>
    public override string LatitudeBand
    {
        get => _latitudeBand;
        set
        {
            if (_latitudeBand != value)
            {
                int updatedNorthing = LatitudeBandHelper.AdjustNorthing(_northing, fromBand: _latitudeBand.ToCharArray()[0], toBand: value.ToCharArray()[0]);
                _northing = updatedNorthing;

                _latitudeBand = value;
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

        string zoneBand = parts[0];

        // Near pole, there isn't a numeric UTM Zone, it's a letter. 
        if (!int.TryParse(zoneBand[..^1], out _zone))
        {
            ErrorMessage = $"Polar coordinate - UTM logic not implemented for {geoCoordString}. Choose a different latitude band.";
            return;
        }
        _latitudeBand = parts[0][2..3];
        _easting = int.Parse(parts[1]);
        _northing = int.Parse(parts[2]);
        _geoCoordinateString = geoCoordString.Replace(" ", "");
        ErrorMessage = string.Empty;
    }

    protected override void UpdateGeoCoordinateString()
    {
        _geoCoordinateString = $"{Zone}{LatitudeBand}{Easting:D6}{Northing:D7}";

        MapPoint = MapPointBuilderEx.FromGeoCoordinateString(_geoCoordinateString, SpatialReferences.WGS84, GeoCoordinateType.UTM);
        Update(MapPoint);
    }
}
