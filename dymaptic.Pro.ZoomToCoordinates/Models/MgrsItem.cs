using ArcGIS.Core.Geometry;

namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class MgrsItem : GridSRBaseItem
{
    private MapPoint _mapPoint = MapPointBuilderEx.CreateMapPoint(0, 0, SpatialReferences.WGS84);
    
    // Default Constructor
    public MgrsItem()
        : base(0, "A", 0, 0)
    {
        _oneHundredKMGridID = "AA";
    }

    public MgrsItem(int zone, string latitudeBand, string oneHundredKMGridID, int easting, int northing)
        :base(zone, latitudeBand, easting, northing)
    {
        _oneHundredKMGridID = oneHundredKMGridID;
        UpdateGeoCoordinateString();
    }


    /// <summary>
    ///     The 100 KM Square ID (2 characters total)
    /// </summary>
    private string _oneHundredKMGridID = "";
    public string OneHundredKMGridID 
    {
        get => _oneHundredKMGridID;
        set
        {
            if (_oneHundredKMGridID != value)
            {
                _oneHundredKMGridID = value;
                UpdateGeoCoordinateString();
            }
        }
    }

    /// <summary>
    ///     A friendly view of the MgrsItem that includes spaces.
    /// </summary>
    public string Display => $"{Zone}{LatitudeBand}{OneHundredKMGridID} {Easting:D5} {Northing:D5}";

    public override string GeoCoordinateString
    {
        get => _geoCoordinateString;
        set => _geoCoordinateString = value;
    }

    /// <summary>
    ///     Updates the GeoCoordinateString after validating it first.
    /// </summary>
    protected override void UpdateGeoCoordinateString()
    {
        string initialGeoCoordinateString = $"{Zone}{LatitudeBand}{OneHundredKMGridID}{Easting:D5}{Northing:D5}";
        string validatedGeoCoordinateString = ValidateGeoCoordinateString(initialGeoCoordinateString);

        _geoCoordinateString = validatedGeoCoordinateString;
    }

    /// <summary>
    ///     Attempts to create a MapPoint from the provided geoCoordinateString. If attempt is successful, it returns the 
    ///     GeoCoordinateString unmodified.  However, if it fails, it iteratively changes the Easting and Northing until the 
    ///     geocoordinate string is valid.
    /// </summary>
    /// <param name="geoCoordinateStringToValidate"></param>
    /// <returns></returns>
    private string ValidateGeoCoordinateString(string geoCoordinateStringToValidate)
    {
        int easting = Easting;
        int northing = Northing;

        try
        {
            _mapPoint = MapPointBuilderEx.FromGeoCoordinateString(geoCoordString: geoCoordinateStringToValidate,
                                                                  spatialReference: SpatialReferences.WGS84,
                                                                  geoCoordType: GeoCoordinateType.MGRS,
                                                                  geoCoordMode: FromGeoCoordinateMode.MgrsNewStyle);
            return geoCoordinateStringToValidate;
        }
        catch
        {
            return geoCoordinateStringToValidate;
        }
    }
}