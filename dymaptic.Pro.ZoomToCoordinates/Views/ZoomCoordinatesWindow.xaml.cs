using dymaptic.Pro.ZoomToCoordinates.ViewModels;
using System.Windows.Controls;

namespace dymaptic.Pro.ZoomToCoordinates.Views;

/// <summary>
/// Interaction logic for ZoomCoordinatesWindow.xaml
/// </summary>
public partial class ZoomCoordinatesWindow : ArcGIS.Desktop.Framework.Controls.ProWindow
{
	public ZoomCoordinatesWindow()
	{
		InitializeComponent();
		DataContext = new ZoomCoordinatesViewModel();
	}

	private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
	{
		Close();
	}
}
