using ArcGIS.Core.Geometry;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace dymaptic.Pro.ZoomToCoordinates.Models;
public class MgrsItem : GridBaseItem
{
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
    public override int Zone
    {
        get => _zone;
        set
        {
            if (_zone != value)
            {
                // Changing UTM zone will likely shift the column value of the 100 KM Grid possibilities
                List<string> originalGridIDs = GetMgrsGridIds(_zone, _latitudeBand);
                string currentGridId = _oneHundredKMGridID;

                _zone = value;

                // If UTM zone shift, creates different potential GridIDs, update our GridID too!
                List<string> newGridIDs = GetMgrsGridIds(_zone, _latitudeBand);
                if (!newGridIDs.Contains(_oneHundredKMGridID))
                {
                    // Try to preserve the column of the original ID, fallback to first option if it doesn't exist
                    char originalColumn = currentGridId[0];

                    string fallback = newGridIDs.FirstOrDefault(id => id[0] == originalColumn)
                                      ?? newGridIDs.FirstOrDefault()
                                      ?? string.Empty;

                    _oneHundredKMGridID = fallback;
                }
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
                // Changing Latitude Band will likely change the 100 KM Grid possibilities
                List<string> originalGridIDs = GetMgrsGridIds(_zone, _latitudeBand);
                string currentGridId = _oneHundredKMGridID;

                _latitudeBand = value;

                // If Latitude Band shift, creates different potential GridIDs, update our GridID too!
                List<string> newGridIDs = GetMgrsGridIds(_zone, _latitudeBand);
                if (!newGridIDs.Contains(_oneHundredKMGridID))
                {
                    // Try to preserve the column of the original ID, fallback to first option if it doesn't exist
                    char originalColumn = currentGridId[0];

                    string fallback = newGridIDs.FirstOrDefault(id => id[0] == originalColumn)
                                      ?? newGridIDs.FirstOrDefault()
                                      ?? string.Empty;

                    _oneHundredKMGridID = fallback;
                }
                UpdateGeoCoordinateString();
            }
        }
    }

    /// <summary>
    ///     The 100 KM Square ID (2 characters total)
    /// </summary>
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
    ///     MGRS 100km square column identifiers cycle based on UTM zone modulo 3, and the valid columns also vary by latitude band.    
    /// </summary>
    public static Dictionary<string, Dictionary<int, string>> ColumnsByLatitudeBand { get; } = new()
    {
        // Southern Hemisphere
        ["C"] = new Dictionary<int, string>
        {
            [0] = "DEF",
            [1] = "MNP",
            [2] = "UVWX"
        },
        ["D"] = new Dictionary<int, string>
        {
            [0] = "CDEF",
            [1] = "LMNP",
            [2] = "UVWX"
        },
        ["E"] = new Dictionary<int, string>
        {
            [0] = "CDEF",
            [1] = "LMNP",
            [2] = "UVWX"
        },
        ["F"] = new Dictionary<int, string>
        {
            [0] = "BCDEFG",
            [1] = "KLMNPQ",
            [2] = "TUVWXY"
        },
        ["G"] = new Dictionary<int, string>
        {
            [0] = "BCDEFG",
            [1] = "KLMNPQ",
            [2] = "TUVWXY"
        },
        ["H"] = new Dictionary<int, string>
        {
            [0] = "BCDEFG",
            [1] = "KLMNPQ",
            [2] = "TUVWXY"
        },
        ["J"] = new Dictionary<int, string>
        {
            [0] = "ABCDEFGH",
            [1] = "JKLMNPQR",
            [2] = "STUVWXYZ"
        },
        ["K"] = new Dictionary<int, string>
        {
            [0] = "ABCDEFGH",
            [1] = "JKLMNPQR",
            [2] = "STUVWXYZ"
        },
        ["L"] = new Dictionary<int, string>
        {
            [0] = "ABCDEFGH",
            [1] = "JKLMNPQR",
            [2] = "STUVWXYZ"
        },
        ["M"] = new Dictionary<int, string>
        {
            [0] = "ABCDEFGH",
            [1] = "JKLMNPQR",
            [2] = "STUVWXYZ"
        },

        // Northern Hemisphere
        ["N"] = new Dictionary<int, string>
        {
            [0] = "ABCDEFGH",
            [1] = "JKLMNPQR",
            [2] = "STUVWXYZ"
        },
        ["P"] = new Dictionary<int, string>
        {
            [0] = "ABCDEFGH",
            [1] = "JKLMNPQR",
            [2] = "STUVWXYZ"
        },
        ["Q"] = new Dictionary<int, string>
        {
            [0] = "ABCDEFGH",
            [1] = "JKLMNPQR",
            [2] = "STUVWXYZ"
        },
        ["R"] = new Dictionary<int, string>
        {
            [0] = "ABCDEFGH",
            [1] = "JKLMNPQR",
            [2] = "STUVWXYZ"
        },
        ["S"] = new Dictionary<int, string>
        {
            [0] = "BCDEFG",
            [1] = "KLMNPQ",
            [2] = "TUVWXY"
        },
        ["T"] = new Dictionary<int, string>
        {
            [0] = "BCDEFG",
            [1] = "KLMNPQ",
            [2] = "TUVWXY"
        },
        ["U"] = new Dictionary<int, string>
        {
            [0] = "BCDEFG",
            [1] = "KLMNPQ",
            [2] = "TUVWXY"
        },
        ["V"] = new Dictionary<int, string>
        {
            [0] = "CDEF",
            [1] = "LMNP",
            [2] = "UVWX"
        },
        ["W"] = new Dictionary<int, string>
        {
            [0] = "CDEF",
            [1] = "LMNP",
            [2] = "UVWX"
        },
        ["X"] = new Dictionary<int, string>
        {
            [0] = "CDEF",
            [1] = "LMNP",
            [2] = "UVWX"
        }
    };

    /// <summary>
    ///     MGRS 100 KM square identification row possiblities alternate odd/even for UTM zone and vary by latitude band.
    /// </summary>
    public static Dictionary<string, Dictionary<string, List<string>>> RowsByLatitudeBand { get; } = new()
    {
        // Southern Hemisphere
        ["C"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["MNPRSTUVA"],
            ["Even"] = ["STUVABCDEF"]
        },
        ["D"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["ABCDEFGHJ"],
            ["Even"] = ["FGHJKLMNP"]
        },
        ["E"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["KLMNPQRST"],
            ["Even"] = ["QRSTUVABC"]
        },
        ["F"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["TUVABCDEFG"],
            ["Even"] = ["CDEFGHJKLM"]
        },
        ["G"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["GHJKLMNPQR"],
            ["Even"] = ["MNPQRSTUVA"]
        },
        ["H"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["RSTUVABCDE"],
            ["Even"] = ["ABCDEFGHJK"]
        },
        ["J"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["EFGHJKLMNP"],
            ["Even"] = ["KLMNPQRSTU"]
        },
        ["K"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["PQRSTUVABC"],
            ["Even"] = ["UVABCDEFGH"]
        },
        ["L"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["CDEFGHJKLM"],
            ["Even"] = ["HJKLMNPQRS"]
        },
        ["M"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["MNPQRSTUV"],
            ["Even"] = ["STUVABCDE"]
        },

        // Northern Hemisphere
        ["N"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["ABCDEFGHJ"],
            ["Even"] = ["FGHJKLMNP"]
        },
        ["P"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["JKLMNPQRST"],
            ["Even"] = ["PQRSUVABC"]
        },
        ["Q"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["TUVABCDEFG"],
            ["Even"] = ["CDEFGHJKLM"]
        },
        ["R"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["GHJKLMNPQR"],
            ["Even"] = ["MNPQRSTUVA"]
        },
        ["S"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["RSTUVABCDE"],
            ["Even"] = ["ABCDEFGHJK"]
        },
        ["T"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["EFGHJKLMNP"],
            ["Even"] = ["KLMNPQRSTU"]
        },
        ["U"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["PQRSTUVABC"],
            ["Even"] = ["UVABCDEFGH"]
        },
        ["V"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["CDEFGHJKL"],
            ["Even"] = ["HJKLMNPQR"]
        },
        ["W"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["LMNPQRSTUV"],
            ["Even"] = ["RSTUVABCDE"]
        },
        ["X"] = new Dictionary<string, List<string>>
        {
            ["Odd"] = ["VABCDEFGHJKLMNP"],
            ["Even"] = ["EFGHJKLMNPQRSTU"]
        }
    };

    /// <summary>
    ///     Gets the 100KM MGRS Grid ID possibilities and puts them into ObservableCollection<string>.
    /// </summary>
    /// <param name="utmZone">The UTM Zone value of 1-60.</param>
    /// <param name="latitudeBand">The Latitude Band letter, C-X excluding I and O.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static ObservableCollection<string> GetMgrsGridIdsObservable(int utmZone, string latitudeBand)
    {
        return BuildMgrsGridIds(utmZone, latitudeBand, () => new ObservableCollection<string>());
    }

    /// <summary>
    ///     Updates the MgrsItem using the most recent MapPoint information.
    /// </summary>
    /// <param name="mapPoint"></param>
    public void Update(MapPoint mapPoint)
    {
        string geoCoordString = mapPoint.ToGeoCoordinateString(mgrsParam);

        // Near pole, there isn't a numeric UTM Zone, it's a letter. 
        if (!int.TryParse(geoCoordString[..2], out _zone))
        {
            ErrorMessage = $"Polar coordinate - MGRS logic not implemented for {geoCoordString}. Choose a different latitude band.";
            return;
        }
        _latitudeBand = geoCoordString[2..3];
        _oneHundredKMGridID = geoCoordString[3..5];
        _easting = int.Parse(geoCoordString[5..10]);
        _northing = int.Parse(geoCoordString[10..]);
        _geoCoordinateString = geoCoordString;
        ErrorMessage = string.Empty;
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
            // If an exception is thrown, the coordinate is invalid (e.g., as lines of longitude converge at the poles, some 100 KM Grid IDs become super skinny).
            ErrorMessage = $"Invalid MGRS coordinate - {initialGeoCoordinateString}. Easting/Northing values exceed valid range for this zone/grid. Choose a different latitude band or reduce values.";
        }
    }

    /// <summary>
    ///     Creates the 100 KM Square Grid ID based on UTM Zone and Latitude band.
    /// </summary>
    /// <typeparam name="TCollection">Generic type placeholder for flexibility to return different types of ICollection<string>.</typeparam>
    /// <param name="utmZone">The UTM zone.</param>
    /// <param name="latitudeBand">The Latitude band.</param>
    /// <param name="collectionFactory"></param>
    /// <returns>Returns a generic </returns>
    /// <exception cref="ArgumentException"></exception>
    private static TCollection BuildMgrsGridIds<TCollection>(int utmZone, string latitudeBand, Func<TCollection> collectionFactory)
    where TCollection : ICollection<string>
    {
        int setIndex = (utmZone - 1) % 3;

        bool isUtmZoneOdd = utmZone % 2 == 1;
        string oddEvenRowKey = isUtmZoneOdd ? "Odd" : "Even";

        var gridIds = collectionFactory();

        if (ColumnsByLatitudeBand.TryGetValue(latitudeBand.ToUpper(), out var setDict) &&
            setDict.TryGetValue(setIndex, out var columnSet) &&
            RowsByLatitudeBand.TryGetValue(latitudeBand, out var zoneParityDict) &&
            zoneParityDict.TryGetValue(oddEvenRowKey, out List<string> rowSets))
        {
            // RowSets is a list with one string, extract the actual characters
            string rowSet = rowSets[0]; // safe since all your definitions use a single string in the list
            foreach (char col in columnSet)
            {
                foreach (char row in rowSet)
                {
                    gridIds.Add($"{col}{row}");
                }
            }
        }
        return gridIds;
    }

    /// <summary>
    ///     Gets the 100KM MGRS Grid ID possibilities and puts them into List<string>.
    /// </summary>
    /// <param name="utmZone">The UTM Zone value of 1-60.</param>
    /// <param name="latitudeBand">The Latitude Band letter, C-X excluding I and O.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    private static List<string> GetMgrsGridIds(int utmZone, string latitudeBand)
    {
        return BuildMgrsGridIds(utmZone, latitudeBand, () => new List<string>());
    }

    // Create MGRS coordinate using truncation, rather than rounding
    private readonly ToGeoCoordinateParameter mgrsParam = new(geoCoordType: GeoCoordinateType.MGRS, geoCoordMode: ToGeoCoordinateMode.MgrsNewStyle, numDigits: 5, rounding: false, addSpaces: false);
    private string _oneHundredKMGridID = "";
}