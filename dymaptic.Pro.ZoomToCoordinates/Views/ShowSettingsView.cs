using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace dymaptic.Pro.ZoomToCoordinates.Views
{
	internal class ShowSettingsView : Button
	{

		private SettingsView? _settingsview = null;

		protected override void OnClick()
		{
			//already open?
			if (_settingsview != null)
				return;
			_settingsview = new SettingsView();
			_settingsview.Owner = FrameworkApplication.Current.MainWindow;
			_settingsview.Closed += (o, e) => { _settingsview = null; };
			_settingsview.Show();
			//uncomment for modal
			//_settingsview.ShowDialog();
		}
	}
}
