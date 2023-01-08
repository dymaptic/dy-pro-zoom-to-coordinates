﻿using dymaptic.Pro.ZoomToCoordinates.ViewModels;
using System.Windows.Controls;

namespace dymaptic.Pro.ZoomToCoordinates.Views
{
	/// <summary>
	/// Interaction logic for LatLongZoomWindow.xaml
	/// </summary>
	public partial class LatLongZoomWindow : ArcGIS.Desktop.Framework.Controls.ProWindow
	{
		public LatLongZoomWindow()
		{
			InitializeComponent();
			ViewModel = new LatLongZoomViewModel();
		}

		internal LatLongZoomViewModel ViewModel
		{
			get => DataContext as LatLongZoomViewModel;
			set => DataContext = value;
		}

		private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			Close();
		}

		private void ColorSchemeDropdown_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ViewModel.Color = ((ComboBox)sender).SelectedItem as string;
		}

		private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			ViewModel.Font = ((ComboBox)sender).SelectedItem as string;
		}
	}
}
