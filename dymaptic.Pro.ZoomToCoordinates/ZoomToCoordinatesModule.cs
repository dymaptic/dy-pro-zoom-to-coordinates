using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace dymaptic.Pro.ZoomToCoordinates;

internal class ZoomToCoordinatesModule : Module
{
	/// <summary>
	/// Retrieve the singleton instance to this module here
	/// </summary>
	public static ZoomToCoordinatesModule Current => _this ??= (ZoomToCoordinatesModule)FrameworkApplication.FindModule("ZoomToCoordinates_Module");

	public event EventHandler? SettingsLoaded;
	public event EventHandler? SettingsUpdated;

	public static Settings GetSettings()
	{
		_settings ??= new Settings();
		return _settings;
	}

	public static void SaveSettings(Settings settings)
	{
		_settings = settings;
		Project.Current.SetDirty();
		Current.SettingsUpdated?.Invoke(Current, EventArgs.Empty);
	}

	/// <summary>
	/// Gets the currently open Settings window, or null if not open
	/// </summary>
	public static Views.SettingsView? GetOpenSettingsWindow()
	{
		return _settingsWindow;
	}

	/// <summary>
	/// Sets the currently open Settings window reference
	/// </summary>
	public static void SetOpenSettingsWindow(Views.SettingsView? settingsWindow)
	{
		_settingsWindow = settingsWindow;
	}

	#region Overrides
	/// <summary>
	/// Called by Framework when ArcGIS Pro is closing
	/// </summary>
	/// <returns>False to prevent Pro from closing, otherwise True</returns>
	protected override bool CanUnload()
	{
		//TODO - add your business logic
		//return false to ~cancel~ Application close
		return true;
	}
	protected override Task OnReadSettingsAsync(ModuleSettingsReader settings)
	{
		string value = (string)settings.Get("ZoomToCoordinates.Settings");
		if (value != null) _settings = JsonConvert.DeserializeObject<Settings>(value) ?? new Settings();
		SettingsLoaded?.Invoke(this, EventArgs.Empty);

		return Task.FromResult(0);
	}

	protected override Task OnWriteSettingsAsync(ModuleSettingsWriter settings)
	{
		settings.Add("ZoomToCoordinates.Settings", JsonConvert.SerializeObject(_settings));

		return Task.FromResult(0);
	}
    #endregion Overrides

    private static ZoomToCoordinatesModule? _this;
    private static Settings? _settings;
	private static Views.SettingsView? _settingsWindow;
}
