using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System;

namespace dymaptic.Pro.ZoomToCoordinates.Views;

internal class ShowSettingsButton : Button
{
	protected override void OnClick()
	{
		//already open?
		if (_settingsview != null)
			return;
        _settingsview = new SettingsView
        {
            Owner = FrameworkApplication.Current.MainWindow
        };
        _settingsview.Closed += OnSettingsClosed;
		_settingsview.Show();
	}

    private void OnSettingsClosed(object? o, EventArgs e)
    {
        if (_settingsview != null)
        {
            _settingsview.Closed -= OnSettingsClosed;
            _settingsview = null;
        }
    }

    private SettingsView? _settingsview = null;
}
