﻿namespace dymaptic.Pro.ZoomToCoordinates.Models;

/// <summary>
///     Stores UTM or MGRS information (note: MGRS is an extension of UTM).
/// </summary>
public abstract class GridSRBaseItem
{
    protected int _zone;
    protected string _latitudeBand = "";
    protected int _easting;
    protected int _northing;
    protected string _geoCoordinateString = "";

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
    ///     UTM and MGRS stores latitude band, one of "CDEFGHJKLMNPQRSTUVWXX" Excludes 'I' and 'O' (1 character total) 
    /// </summary>
    public string LatitudeBand 
    {
        get => _latitudeBand;
        set
        {
            if (_latitudeBand != value)
            {
                _latitudeBand = value;
                UpdateGeoCoordinateString();
            }
        }
    }
    
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
    ///     The GeoCoordinateString needs to be kept updated b/c it's how the WGS84MapPoint is kept updated.
    /// </summary>
    protected abstract void UpdateGeoCoordinateString();
}
