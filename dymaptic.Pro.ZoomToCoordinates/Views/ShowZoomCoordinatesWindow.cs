using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System;

namespace dymaptic.Pro.ZoomToCoordinates.Views;

	internal class ShowZoomCoordinatesWindow : Button
	{

		private ZoomCoordinatesWindow? _zoomCoordinateswindow = null;

		protected override void OnClick()
		{
			//already open?
			if (_zoomCoordinateswindow != null)
				return;
			_zoomCoordinateswindow = new ZoomCoordinatesWindow();
			_zoomCoordinateswindow.Owner = FrameworkApplication.Current.MainWindow;
			_zoomCoordinateswindow.Closed += OnZoomClosed;
			_zoomCoordinateswindow.Show();
			//uncomment for modal
			//_zoomCoordinateswindow.ShowDialog();
		}

    private void OnZoomClosed(object? o, EventArgs e)
    {
        if (_zoomCoordinateswindow != null)
        {
            _zoomCoordinateswindow.Closed -= OnZoomClosed;
            _zoomCoordinateswindow = null;
        }
    }
}
