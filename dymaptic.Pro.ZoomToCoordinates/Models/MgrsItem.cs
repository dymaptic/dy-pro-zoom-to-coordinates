using ArcGIS.Core.Geometry;
using System;
using System.Collections.Generic;

namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class MgrsItem : GridBaseItem
{
    // Create MGRS coordinate using truncation, rather than rounding
    private readonly ToGeoCoordinateParameter mgrsParam = new(geoCoordType: GeoCoordinateType.MGRS, geoCoordMode: ToGeoCoordinateMode.MgrsNewStyle, numDigits: 5, rounding: false, addSpaces: false);

    // Default Constructor
    public MgrsItem()
        : base(1, "C", 0, 0)
    {
        _oneHundredKMGridID = "AF";
    }

    public MgrsItem(int zone, string latitudeBand, string oneHundredKMGridID, int easting, int northing)
        : base(zone, latitudeBand, easting, northing)
    {
        _oneHundredKMGridID = oneHundredKMGridID;
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
                // Changing UTM zone will likely shift the column value of the 100 KM Grid possibilities
                List<string> originalGridIDs = GetMgrsGridIds(_zone, _latitudeBand);
                int index = originalGridIDs.IndexOf(_oneHundredKMGridID);

                _zone = value;

                // If UTM zone shift, creates different potential GridIDs, update our GridID too!
                List<string> newGridIDs = GetMgrsGridIds(_zone, _latitudeBand);
                if (!newGridIDs.Contains(_oneHundredKMGridID))
                {
                    _oneHundredKMGridID = newGridIDs[index];
                }

                UpdateGeoCoordinateString();
            }
        }
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
    ///     Gets the 100 KM MGRS Grid ID possibilities given a UTM Zone and latitude band.
    /// </summary>
    /// <param name="utmZone">The UTM Zone value of 1-60.</param>
    /// <param name="latitudeBand">The Latitude Band letter, C-X excluding I and O.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static List<string> GetMgrsGridIds(int utmZone, string latitudeBand)
    {
        // MGRS column possibilities depend on the UTM Zone
        string[] columnSets = ["ABCDEFGH", "JKLMNPQR", "STUVWXYZ"];
        int setIndex = (utmZone - 1) % 3;
        string columnSet = columnSets[setIndex];

        // MGRS row possibilities based on Latitude Band
        Dictionary<string, List<string>> rows = new()
        {
            // Southern Hemisphere
            { "C", new List<string> { "F", "E", "D", "C", "B", "A", "V", "U", "T", "S" } },
            { "D", new List<string> { "Q", "P", "N", "M", "L", "K", "J", "H", "G", "F" } },
            { "E", new List<string> { "C", "B", "A", "V", "U", "T", "S", "R", "Q", "P", "Q" } },
            { "F", new List<string> { "M", "L", "K", "J", "H", "G", "F", "E", "D", "C" } },
            { "G", new List<string> { "A", "V", "U", "T", "S", "R", "Q", "P", "N", "M" } },
            { "H", new List<string> { "K", "J", "H", "G", "F", "E", "D", "C", "B", "A" } },
            { "J", new List<string> { "U", "T", "S", "R", "Q", "P", "N", "M", "L", "K" } },
            { "K", new List<string> { "H", "G", "F", "E", "D", "C", "B", "A", "V", "U" } },
            { "L", new List<string> { "S", "R", "Q", "P", "N", "M", "L", "K", "J", "H" } },
            { "M", new List<string> { "E", "D", "C", "B", "A", "V", "U", "T", "S" } },

            // Northern Hemisphere
            { "N", new List<string> { "F", "G", "H", "J", "K", "L", "M", "N", "P" } },
            { "P", new List<string> { "P", "Q", "R", "S", "T", "U", "V", "A", "B", "C" } },
            { "Q", new List<string> { "C", "D", "E", "F", "G", "H", "J", "K", "L", "M" } },
            { "R", new List<string> { "M", "N", "P", "Q", "R", "S", "T", "U", "V", "A" } },
            { "S", new List<string> { "A", "B", "C", "D", "E", "F", "G", "H", "J", "K" } },
            { "T", new List<string> { "K", "L", "M", "N", "P", "Q", "R", "S", "T", "U" } },
            { "U", new List<string> { "U", "V", "A", "B", "C", "D", "E", "F", "G", "H" } },
            { "V", new List<string> { "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S" } },
            { "W", new List<string> { "S", "T", "U", "V", "A", "B", "C", "D", "E" } },
            { "X", new List<string> { "E", "F", "G", "H", "J", "K", "L", "M", "N", "P", "Q", "R", "S", "T", "U" } }
        };

        // Note: exception shouldn't ever be thrown b/c we constrain the Latitude Band possibilities that the user can select.
        if (!rows.TryGetValue(latitudeBand, out List<string> rowSet))
        {
            throw new ArgumentException($"Invalid latitude band: {latitudeBand}");
        }

        List<string> mgrsGridIds = [];

        foreach (char col in columnSet)
        {
            foreach (string row in rowSet)
            {
                mgrsGridIds.Add($"{col}{row}");
            }
        }

        return mgrsGridIds;
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
        try
        {
            MapPoint = MapPointBuilderEx.FromGeoCoordinateString(geoCoordString: initialGeoCoordinateString,
                                                                  spatialReference: SpatialReferences.WGS84,
                                                                  geoCoordType: GeoCoordinateType.MGRS,
                                                                  geoCoordMode: FromGeoCoordinateMode.MgrsNewStyle);
            Update(MapPoint);
            ErrorMessage = string.Empty;
        }
        catch
        {
            ErrorMessage = $"GeoCoordinateString error! tried to create a MapPoint from this string: {initialGeoCoordinateString}";
        }
    }
}