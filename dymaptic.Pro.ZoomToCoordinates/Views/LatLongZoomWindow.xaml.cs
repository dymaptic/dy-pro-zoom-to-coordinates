using dymaptic.Pro.ZoomToCoordinates.ViewModels;
using System.Windows.Controls;

namespace dymaptic.Pro.ZoomToCoordinates.Views;

/// <summary>
/// Interaction logic for LatLongZoomWindow.xaml
/// </summary>
public partial class LatLongZoomWindow : ArcGIS.Desktop.Framework.Controls.ProWindow
{
	public LatLongZoomWindow()
	{
		InitializeComponent();
		DataContext = new LatLongZoomViewModel();
	}

	private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
	{
		Close();
	}
}
