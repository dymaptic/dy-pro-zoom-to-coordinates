using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dymaptic.Pro.ZoomToCoordinates;
using System;


public class LatitudeBandHelper
{
    // Approximate number of meters per latitude band (8 degrees latitude)
    private const int BandHeightMeters = 888000;

    // Latitude bands (C to X, excluding I and O)
    private static readonly string LatitudeBands = "CDEFGHJKLMNPQRSTUVWX";
    private static char SouthernmostBand => 'C';
    private static char NorthernmostBand => 'X';

    // Returns true if band is in the Northern Hemisphere
    private static bool IsNorthernHemisphere(char band)
    {
        return band >= 'N'; // 'N' is the first band above the Equator
    }

    public static Dictionary<char, int> GetLatitudeBandStartNorthings()
    {
        var bandNorthings = new Dictionary<char, int>();

        for (int i = 0; i < LatitudeBands.Length; i++)
        {
            char band = LatitudeBands[i];

            if (IsNorthernHemisphere(band))
            {
                // Northern Hemisphere: start at 0m (Equator), increase northward
                int northBandIndex = i - LatitudeBands.IndexOf('N');
                int northing = northBandIndex * BandHeightMeters;
                bandNorthings[band] = northing;
            }
            else
            {
                // Southern Hemisphere: start at 10,000,000m (Equator), decrease southward
                int southBandIndex = LatitudeBands.IndexOf('M') - i + 1;
                int northing = 10000000 - (southBandIndex * BandHeightMeters);
                bandNorthings[band] = northing;
            }
        }

        return bandNorthings;
    }

    /// <summary>
    /// Adjusts the Northing when switching from one latitude band to another.
    /// If the selected band is the southernmost ('C') or northernmost ('X'), it resets
    /// the Northing to the start of that band to avoid invalid coordinates.
    /// </summary>
    public static int AdjustNorthing(int currentNorthing, char fromBand, char toBand)
    {
        var bandNorthings = GetLatitudeBandStartNorthings();

        // Special case: if the new band is the extreme C or X, reset to start of band
        if (toBand == SouthernmostBand || toBand == NorthernmostBand)
        {
            return bandNorthings[toBand];
        }

        int fromBase = bandNorthings[fromBand];
        int toBase = bandNorthings[toBand];

        int offset = toBase - fromBase;

        return currentNorthing + offset;
    }
}