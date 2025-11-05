using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using System;

namespace dymaptic.Pro.ZoomToCoordinates.Views;

internal class ShowSettingsButton : Button
{
	protected override void OnClick()
	{
		//already open?
		var existingWindow = ZoomToCoordinatesModule.GetOpenSettingsWindow();
		if (existingWindow != null)
		{
			// Bring existing window to front
			existingWindow.Activate();
			return;
		}

		var settingsView = new SettingsView
        {
            Owner = FrameworkApplication.Current.MainWindow
        };
        settingsView.Closed += OnSettingsClosed;
		ZoomToCoordinatesModule.SetOpenSettingsWindow(settingsView);
		settingsView.Show();
	}

    private void OnSettingsClosed(object? o, EventArgs e)
    {
		var settingsView = ZoomToCoordinatesModule.GetOpenSettingsWindow();
        if (settingsView != null)
        {
            settingsView.Closed -= OnSettingsClosed;
			ZoomToCoordinatesModule.SetOpenSettingsWindow(null);
        }
    }
}
