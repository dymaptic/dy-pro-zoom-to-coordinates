using dymaptic.Pro.ZoomToCoordinates.ViewModels;

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
			get => (LatLongZoomViewModel)DataContext;
			set => DataContext = value;
		}

		private void CloseButton_Click(object sender, System.Windows.RoutedEventArgs e)
		{
			Close();
		}
	}
}
