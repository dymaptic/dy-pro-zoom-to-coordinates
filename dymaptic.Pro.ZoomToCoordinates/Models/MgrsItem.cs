using ArcGIS.Core.Geometry;

namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class MgrsItem : GridSRBaseItem
{
    // Create MGRS coordinate using truncation, rather than rounding
    private readonly ToGeoCoordinateParameter mgrsParam = new(geoCoordType: GeoCoordinateType.MGRS, geoCoordMode: ToGeoCoordinateMode.MgrsNewStyle, numDigits: 5, rounding: false, addSpaces: false);

    // Default Constructor
    public MgrsItem()
        : base(0, "A", 0, 0)
    {
        _oneHundredKMGridID = "AA";
    }

    public MgrsItem(int zone, string latitudeBand, string oneHundredKMGridID, int easting, int northing)
        : base(zone, latitudeBand, easting, northing)
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

    public string ErrorMessage { get; set; } = "";

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
    ///     Updates the MgrsItem using the most recent MapPoint information.
    /// </summary>
    /// <param name="mapPoint"></param>
    public void Update(MapPoint mapPoint)
    {
        string geoCoordString = mapPoint.ToGeoCoordinateString(mgrsParam);

        _zone = int.Parse(geoCoordString[..2]);
        _latitudeBand = geoCoordString[2..3];
        _oneHundredKMGridID = geoCoordString[3..5];
        _easting = int.Parse(geoCoordString[5..10]);
        _northing = int.Parse(geoCoordString[10..]);
        _geoCoordinateString = geoCoordString;
    }

    /// <summary>
    ///     Updates the GeoCoordinateString after validating it first.
    /// </summary>
    protected override void UpdateGeoCoordinateString()
    {
        string initialGeoCoordinateString = $"{Zone}{LatitudeBand}{OneHundredKMGridID}{Easting:D5}{Northing:D5}";
        string result = ValidateGeoCoordinateString(initialGeoCoordinateString);
        if (result == "success!")
        {
            ErrorMessage = string.Empty;
            _geoCoordinateString = initialGeoCoordinateString;
        }
        else
        {
            ErrorMessage = $"{result} tried to create a MapPoint from this string: {initialGeoCoordinateString}";
        }
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
        try
        {
            MapPoint = MapPointBuilderEx.FromGeoCoordinateString(geoCoordString: geoCoordinateStringToValidate,
                                                                  spatialReference: SpatialReferences.WGS84,
                                                                  geoCoordType: GeoCoordinateType.MGRS,
                                                                  geoCoordMode: FromGeoCoordinateMode.MgrsNewStyle);
            Update(MapPoint);

            return "success!";
        }
        catch
        {
            return "GeoCoordinateString error!";
        }
    }
}