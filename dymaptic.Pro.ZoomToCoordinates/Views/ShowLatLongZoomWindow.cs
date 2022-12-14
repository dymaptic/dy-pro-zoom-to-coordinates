using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace dymaptic.Pro.ZoomToCoordinates.Views
{
	internal class ShowLatLongZoomWindow : Button
	{

		private LatLongZoomWindow _latlongzoomwindow = null;

		protected override void OnClick()
		{
			//already open?
			if (_latlongzoomwindow != null)
				return;
			_latlongzoomwindow = new LatLongZoomWindow();
			_latlongzoomwindow.Owner = FrameworkApplication.Current.MainWindow;
			_latlongzoomwindow.Closed += (o, e) => { _latlongzoomwindow = null; };
			_latlongzoomwindow.Show();
			//uncomment for modal
			//_latlongzoomwindow.ShowDialog();
		}

	}
}
