using dymaptic.Pro.ZoomToCoordinates.ViewModels;
using System.Windows.Controls;

namespace dymaptic.Pro.ZoomToCoordinates.Views
{
	/// <summary>
	/// Interaction logic for SettingsView.xaml
	/// </summary>
	public partial class SettingsView : ArcGIS.Desktop.Framework.Controls.ProWindow
	{
		public SettingsView()
		{
			InitializeComponent(); InitializeComponent();
			ViewModel = new SettingsViewModel();
		}

		internal SettingsViewModel ViewModel
		{
			get => DataContext as SettingsViewModel;
			set => DataContext = value;
		}
	}
}
