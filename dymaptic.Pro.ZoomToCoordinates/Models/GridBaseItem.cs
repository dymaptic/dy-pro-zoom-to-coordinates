using ArcGIS.Core.Geometry;

namespace dymaptic.Pro.ZoomToCoordinates.Models;

/// <summary>
///     Stores UTM or MGRS information (note: MGRS is an extension of UTM).
/// </summary>
public abstract class GridBaseItem(int zone, string latitudeBand, int easting, int northing)
{
    protected int _zone = zone;
    protected string _latitudeBand = latitudeBand;
    protected int _easting = easting;
    protected int _northing = northing;
    protected string _geoCoordinateString = "";
    public MapPoint MapPoint { get; protected set;} = MapPointBuilderEx.CreateMapPoint(0, 0, SpatialReferences.WGS84);

    /// <summary>
    ///     The UTM zone.
    /// </summary>
    public abstract int Zone { get; set; }
    //{
    //    get => _zone;
    //    set
    //    {
    //        if (_zone != value)
    //        {
    //            _zone = value;
    //            UpdateGeoCoordinateString();
    //        }
    //    }
    //}

    /// <summary>
    ///     UTM and MGRS stores latitude band, one of "CDEFGHJKLMNPQRSTUVWXX" Excludes 'I' and 'O' (1 character total) 
    /// </summary>
    public abstract string LatitudeBand { get; set; }
    //{
    //    get => _latitudeBand;
    //    set
    //    {
    //        if (_latitudeBand != value)
    //        {
    //            int updatedNorthing = LatitudeBandHelper.AdjustNorthing(_northing, fromBand: _latitudeBand.ToCharArray()[0], toBand: value.ToCharArray()[0]);
    //            _northing = updatedNorthing;

    //            _latitudeBand = value;
    //            UpdateGeoCoordinateString();
    //        }
    //    }
    //}

    /// <summary>
    ///     The Easting (X-coordinate value) which is a positive number with a maximum of 6 digits when UTM (5 max for MGRS).
    /// </summary>
    public int Easting 
    {
        get => _easting;
        set
        {
            if ( _easting != value)
            {
                _easting = value;
                UpdateGeoCoordinateString();
            }
        }
    }

    /// <summary>
    ///     The Northing (Y-coordinate value) which is a positive number with a maximum of 7 digits when UTM (5 max for MGRS).
    /// </summary>
    public int Northing
    {
        get => _northing;
        set
        {
            if (_northing != value)
            {
                _northing = value;
                UpdateGeoCoordinateString();
            }
        }
    }

    /// <summary>
    ///     The coordinate format that allows easy conversion from one format to another and for auto-updates.
    /// </summary>
    public abstract string GeoCoordinateString { get; set; }

    /// <summary>
    ///     The GeoCoordinateString needs to be kept updated b/c it's how the MapPoint is kept updated.
    /// </summary>
    protected abstract void UpdateGeoCoordinateString();
}
