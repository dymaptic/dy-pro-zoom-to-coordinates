using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System;

namespace dymaptic.Pro.ZoomToCoordinates.Views;

	internal class ShowLatLongZoomWindow : Button
	{

		private LatLongZoomWindow? _latlongzoomwindow = null;

		protected override void OnClick()
		{
			//already open?
			if (_latlongzoomwindow != null)
				return;
			_latlongzoomwindow = new LatLongZoomWindow();
			_latlongzoomwindow.Owner = FrameworkApplication.Current.MainWindow;
			_latlongzoomwindow.Closed += OnZoomClosed;
			_latlongzoomwindow.Show();
			//uncomment for modal
			//_latlongzoomwindow.ShowDialog();
		}

    private void OnZoomClosed(object? o, EventArgs e)
    {
        if (_latlongzoomwindow != null)
        {
            _latlongzoomwindow.Closed -= OnZoomClosed;
            _latlongzoomwindow = null;
        }
    }
}
