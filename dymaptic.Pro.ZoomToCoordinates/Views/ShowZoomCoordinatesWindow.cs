using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System;

namespace dymaptic.Pro.ZoomToCoordinates.Views;

internal class ShowZoomCoordinatesWindow : Button
{
	protected override void OnClick()
	{
		//already open?
		if (_zoomCoordinatesWindow != null)
			return;
		_zoomCoordinatesWindow = new ZoomCoordinatesWindow();
		_zoomCoordinatesWindow.Owner = FrameworkApplication.Current.MainWindow;
		_zoomCoordinatesWindow.Closed += OnZoomClosed;
		_zoomCoordinatesWindow.Show();
	}

    private void OnZoomClosed(object? o, EventArgs e)
    {
        if (_zoomCoordinatesWindow != null)
        {
            _zoomCoordinatesWindow.Closed -= OnZoomClosed;
            _zoomCoordinatesWindow = null;
        }
    }
    private ZoomCoordinatesWindow? _zoomCoordinatesWindow = null;
}
