﻿using ArcGIS.Desktop.Framework.Contracts;
using System;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels;

internal class SettingsViewModel : Page
{
	public SettingsViewModel()
	{
		ZoomToCoordinatesModule.Current.SettingsLoaded += Current_SettingsLoaded;
    }

	public string[] MarkerSchemes { get; set; } = ["Circle", "Cross", "Diamond", "Square", "X", "Triangle", "Pushpin", "Star", "RoundedSquare", "RoundedTriangle", "Rod", "Rectangle", "RoundedRectangle", "Hexagon", "StretchedHexagon", "RaceTrack", "HalfCircle", "Cloud"];

	public string[] ColorSchemes { get; set; } = ["Black", "Gray", "White", "Red", "Green", "Blue", "Pink", "Orange", "Yellow", "Purple"];

	public string[] FontFamilySchemes { get; set; } = ["Arial", "Broadway", "Papyrus", "Tahoma", "Times New Roman", "Comic Sans MS"];

	public string[] FontStyleSchemes { get; set; } = ["Regular", "Bold", "Italic"];

	public double Longitude
	{
		get => _settings.Longitude;
		set
		{
			if (_settings.Longitude != value)
			{
				_settings.Longitude = value;
				ZoomToCoordinatesModule.SaveSettings(_settings);
				NotifyPropertyChanged();
			}
		}
	}

	public double Latitude
	{
		get => _settings.Latitude;
		set
		{
			if (_settings.Latitude != value)
			{
				_settings.Latitude = value;
				ZoomToCoordinatesModule.SaveSettings(_settings);
				NotifyPropertyChanged();
			}
		}
	}

	public double Scale
	{
		get => _settings.Scale;
        set
		{
			if (_settings.Scale != value)
			{
				_settings.Scale = value;
				ZoomToCoordinatesModule.SaveSettings(_settings);
				NotifyPropertyChanged();
			}
		}
	}

    public bool ShowFormattedCoordinates
    {
        get => _settings.ShowFormattedCoordinates;
        set
        {
            if (_settings.ShowFormattedCoordinates != value)
            {
                _settings.ShowFormattedCoordinates = value;
                ZoomToCoordinatesModule.SaveSettings(_settings);
                NotifyPropertyChanged();
            }
        }
    }

    public bool ShowGraphic
	{
		get => _settings.ShowGraphic;
		set
		{
			if (_settings.ShowGraphic != value)
			{
				_settings.ShowGraphic = value;
				ZoomToCoordinatesModule.SaveSettings(_settings);
				NotifyPropertyChanged();
			}
		}
	}

	public string Marker
	{
		get => _settings.Marker;
		set
		{
			if (_settings.Marker != value)
			{
				_settings.Marker = value;
				ZoomToCoordinatesModule.SaveSettings(_settings);
				NotifyPropertyChanged();
			}
		}
	}

	public string MarkerColor
	{
		get => _settings.MarkerColor;
		set
		{
			if (_settings.MarkerColor != value)
			{
				_settings.MarkerColor = value;
				ZoomToCoordinatesModule.SaveSettings(_settings);
				NotifyPropertyChanged();
			}
		}
	}

    public int MarkerSize
    {
        get => _settings.MarkerSize;
        set
        {
            if (_settings.MarkerSize != value)
            {
                _settings.MarkerSize = value;
                ZoomToCoordinatesModule.SaveSettings(_settings);
                NotifyPropertyChanged();
            }
        }
    }

    public string FontFamily
	{
		get => _settings.FontFamily;
		set
		{
			if (_settings.FontFamily != value)
			{
				_settings.FontFamily = value;
				ZoomToCoordinatesModule.SaveSettings(_settings);
				NotifyPropertyChanged();
			}
		}
	}

    public int FontSize
    {
        get => _settings.FontSize;
        set
        {
            if (_settings.FontSize != value)
            {
                _settings.FontSize = value;
                ZoomToCoordinatesModule.SaveSettings(_settings);
                NotifyPropertyChanged();
            }
        }
    }

    public string FontStyle
	{
		get => _settings.FontStyle;
		set
		{
			if (_settings.FontStyle != value)
			{
				_settings.FontStyle = value;
				ZoomToCoordinatesModule.SaveSettings(_settings);
				NotifyPropertyChanged();
			}
		}
	}

	public string FontColor
	{
		get => _settings.FontColor;
		set
		{
			if (_settings.FontColor != value)
			{
				_settings.FontColor = value;
				ZoomToCoordinatesModule.SaveSettings(_settings);
				NotifyPropertyChanged();
			}
		}
	}

	private void Current_SettingsLoaded(object sender, EventArgs e)
	{
		_settings = ZoomToCoordinatesModule.GetSettings();

		// Location-related
		NotifyPropertyChanged(nameof(Latitude));
		NotifyPropertyChanged(nameof(Longitude));
		NotifyPropertyChanged(nameof(Scale));
		NotifyPropertyChanged(nameof(ShowFormattedCoordinates));

		// Graphic-related
		NotifyPropertyChanged(nameof(Marker));
		NotifyPropertyChanged(nameof(MarkerColor));
		NotifyPropertyChanged(nameof(MarkerSize));
		NotifyPropertyChanged(nameof(FontFamily));
		NotifyPropertyChanged(nameof(FontSize));
		NotifyPropertyChanged(nameof(FontStyle));
		NotifyPropertyChanged(nameof(FontColor));
		NotifyPropertyChanged(nameof(ShowGraphic));
	}

	private Settings _settings = ZoomToCoordinatesModule.GetSettings();
}
