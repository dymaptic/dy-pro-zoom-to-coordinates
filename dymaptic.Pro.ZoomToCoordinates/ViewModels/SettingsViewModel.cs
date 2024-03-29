﻿using ArcGIS.Desktop.Framework.Contracts;
using System;
using System.Collections.ObjectModel;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels
{
	internal class SettingsViewModel : Page
	{
		public SettingsViewModel()
		{
			ZoomToCoordinatesModule.Current.SettingsLoaded += Current_SettingsLoaded;
		}

		public ObservableCollection<string> MarkerSchemes { get; set; } = new ObservableCollection<string>() { "Circle", "Cross", "Diamond", "Square", "X", "Triangle", "Pushpin", "Star", "RoundedSquare", "RoundedTriangle", "Rod", "Rectangle", "RoundedRectangle", "Hexagon", "StretchedHexagon", "RaceTrack", "HalfCircle", "Cloud" };

		public ObservableCollection<string> ColorSchemes { get; set; } = new() { "Black", "Gray", "White", "Red", "Green", "Blue" };

		public ObservableCollection<string> FontFamilySchemes { get; set; } = new ObservableCollection<string> { "Arial", "Broadway", "Papyrus", "Tahoma", "Times New Roman" };

		public ObservableCollection<string> FontStyleSchemes { get; set; } = new ObservableCollection<string> { "Regular", "Bold", "Italic" };

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

		public bool CreateGraphic
		{
			get => _settings.CreateGraphic;
			set
			{
				if (_settings.CreateGraphic != value)
				{
					_settings.CreateGraphic = value;
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
			NotifyPropertyChanged(nameof(Marker));
			NotifyPropertyChanged(nameof(MarkerColor));
			NotifyPropertyChanged(nameof(FontFamily));
			NotifyPropertyChanged(nameof(FontStyle));
			NotifyPropertyChanged(nameof(FontColor));
		}

		private Settings _settings = ZoomToCoordinatesModule.GetSettings();
	}
}
