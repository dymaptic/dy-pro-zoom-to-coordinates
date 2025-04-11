using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System;

namespace dymaptic.Pro.ZoomToCoordinates.Views;

internal class ShowAbout : Button
{
    protected override void OnClick()
    {
        //already open?
        if (_about != null)
            return;
        _about = new About
        {
            Owner = FrameworkApplication.Current.MainWindow
        };
        _about.Closed += OnAboutClosed;
        _about.Show();
    }

    private void OnAboutClosed(object? o, EventArgs e)
    {
        if (_about != null)
        {
            // explicitly unregister event handler from the Closed event to prevent memory leak
            _about.Closed -= OnAboutClosed;
            _about = null;
        }
    }
    private About? _about = null;
}
