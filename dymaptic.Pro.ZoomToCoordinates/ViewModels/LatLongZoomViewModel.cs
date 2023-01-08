using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels
{
	public class LatLongZoomViewModel : PropertyChangedBase
	{
		// Private backing-fields to the public properties
		private double _latitude;
		private double _longitude;
		private double _scale;
		private bool _keepGraphic;
		private string _color;
		private string _font;

		// Properties
		public double Latitude
		{
			get => _latitude;
			set
			{
				if (value < -90 || value > 90)
				{
					ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Latitude must be between -90 and 90.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				else
				{
					SetProperty(ref _latitude, value);
				}
			}
		}
		public double Longitude
		{
			get => _longitude;
			set
			{
				if (value < -180 || value > 180)
				{
					ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Longitude must be between -180 and 180.", "ERROR", MessageBoxButton.OK, MessageBoxImage.Error);
				}
				else
				{
					SetProperty(ref _longitude, value);
				}
			}
		}

		public double Scale
		{
			get => _scale;
			set => SetProperty(ref _scale, value);
		}

		public bool KeepGraphic
		{
			get => _keepGraphic;
			set => SetProperty(ref _keepGraphic, value);
		}

		public List<string> ColorSchemes { get; set; } = new List<string> { "Black", "Gray", "White", "Red", "Green", "Blue"};
		public List<string> FontSchemes { get; set; } = new List<string> { "Arial", "Papyrus", "Tahoma", "Times New Roman"};

		public string Color 
		{
			get => _color;
			set => SetProperty(ref _color, value); 
		}

		public string Font
		{
			get => _font;
			set => SetProperty(ref _font, value);
		}

		/// <summary>
		/// Command to validate and zoom to coordinates
		/// </summary>
		public ICommand ZoomCommand { get; }

		// Constructor
		internal LatLongZoomViewModel()
		{
			// Starting values
			//Latitude = 40.1059757;
			//Longitude = -106.6340134;
			//Scale = 100_000;

			// Command is grayed out if there isn't an active map view or scale isn't set
			ZoomCommand = new RelayCommand(async () => { await ZoomToCoordinates(); }, () => MapView.Active != null && Scale != 0);
		}

		internal async Task ZoomToCoordinates()
		{
			await QueuedTask.Run(() =>
			{
				MapView mapView = MapView.Active;
				Camera camera = mapView.Camera;
				SpatialReference sr = camera.SpatialReference;

				// Create new camera & spatial reference objects, if active map isn't WGS84
				if (sr.Wkid != 4326)
				{
					sr = SpatialReferenceBuilder.CreateSpatialReference(4326);
					Camera newCamera = new(x: Longitude, y: Latitude, scale: Scale, heading: 0, spatialReference: sr);
					mapView.ZoomTo(newCamera, TimeSpan.Zero);
				}

				// Otherwise, just update the coordinates & scale of the existing camera
				else
				{
					camera.X = Longitude;
					camera.Y = Latitude;
					camera.Scale = Scale;
					mapView.ZoomTo(camera, TimeSpan.Zero);
				}

				// Create point graphic
				if (KeepGraphic)
				{
					Map map = MapView.Active.Map;
					CIMPointSymbol symbol = SymbolFactory.Instance.ConstructPointSymbol(color: GetColor(), size: 20, SimpleMarkerStyle.Pushpin);

					// 2D Map
					if (map.MapType == MapType.Map)
					{
						// Create the outer Group Layer container (if necessary) which is where point graphics will be placed in ArcGIS Pro Table of Contents
						string groupLyrName = "Coordinates (Graphics Layers)";
						var groupLyrContainer = map.GetLayersAsFlattenedList().OfType<GroupLayer>().Where(x => x.Name.StartsWith(groupLyrName)).FirstOrDefault();
						groupLyrContainer ??= LayerFactory.Instance.CreateGroupLayer(container: map, index: 0, layerName: groupLyrName);

						// Create point at specified coordinates & graphic for the map
						MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate2D(Longitude, Latitude), sr);
						CIMGraphic graphic = GraphicFactory.Instance.CreateSimpleGraphic(geometry: point, symbol: symbol);

						// Create the point container inside the group layer container and place graphic into it to create graphic element
						GraphicsLayerCreationParams lyrParams = new() { Name = $"{Longitude} {Latitude}" };
						GraphicsLayer pointGraphicContainer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(layerParams: lyrParams, container: groupLyrContainer);
						ElementFactory.Instance.CreateGraphicElement(elementContainer: pointGraphicContainer, cimGraphic: graphic, select: false);

						// Finally create a point label graphic & add it to the point container
						CIMTextGraphic label = new()
						{
							Symbol = SymbolFactory.Instance.ConstructTextSymbol(color:ColorFactory.Instance.BlackRGB, size:12, fontFamilyName:Font, fontStyleName:"Regular").MakeSymbolReference(),
							Text = $"    <b>{Longitude} {Latitude}</b>",
							Shape = point
						};

						pointGraphicContainer.AddElement(cimGraphic:label, select: false);
					}

					// 3D Map (only adds a Pushpin without a label, and without it being in a Graphics container in the ArcGIS Pro Table of Contents
					else if (map.IsScene)
					{
						MapPoint point = MapPointBuilderEx.CreateMapPoint(new Coordinate3D(x: Longitude, y: Latitude, z: 0), sr);
						mapView.AddOverlay(point, symbol.MakeSymbolReference());
					}
				}
			});
		}

		private CIMColor GetColor()
		{
			CIMColor color = ColorFactory.Instance.BlackRGB;
			switch (Color)
			{
				case "Black":
					color = ColorFactory.Instance.BlackRGB;

					break;
				case "Gray":
					color = ColorFactory.Instance.GreyRGB;

					break;
				case "White":
					color = ColorFactory.Instance.WhiteRGB;
				
					break;
				case "Red":
					color = ColorFactory.Instance.RedRGB;

					break;
				case "Green":
					color = ColorFactory.Instance.GreenRGB;

					break;
				case "Blue":
					color = ColorFactory.Instance.BlueRGB;
					
					break;
			}
			return color;
		}

	}
}
