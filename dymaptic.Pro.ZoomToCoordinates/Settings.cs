namespace dymaptic.Pro.ZoomToCoordinates;

	public class Settings
	{
		// Default settings when Add-In loaded for initial time; user's actual default choices as they modify the drop-downs get saved/updated by EventHandler in ZoomToCoordinatesModule.cs 
		public double Longitude = -122.4774494;
		public double Latitude = 37.8108275;
		public double Scale = 10_000;
		public bool CreateGraphic = false;
		public string Marker = "Pushpin";
		public string MarkerColor = "Green";
		public string FontFamily = "Tahoma";
		public string FontStyle = "Regular";
		public string FontColor = "Black";
	}
