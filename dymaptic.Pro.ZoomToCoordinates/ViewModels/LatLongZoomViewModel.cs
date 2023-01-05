using ArcGIS.Core.CIM;
using ArcGIS.Core.Geometry;
using ArcGIS.Core.Internal.CIM;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace dymaptic.Pro.ZoomToCoordinates.ViewModels
{
	internal class LatLongZoomViewModel : PropertyChangedBase
	{
		// Private backing-fields to the public properties
		private double _latitude;
		private double _longitude;
		private double _scale;
		private bool _keepGraphic;

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

		/// <summary>
		/// Command to validate and zoom to coordinates
		/// </summary>
		public ICommand ZoomCommand { get; }
		/* This is a WPF Command - defining the action one time, should it need to be used in multiple places (e.g., button, menu, toolbar, etc.)
         * https://wpf-tutorial.com/commands/introduction/
         * WPF toggles all the subscribing interface elements on or off automatically based on the current availability & state of the window/control (programmer not responsible for disabling/enabling user interface elements)
         */


		// Constructor
		internal LatLongZoomViewModel()
		{
			// Starting values
			Latitude = 40.1059757;
			Longitude = -106.6340134;
			Scale = 100_000;

			// Command is grayed out if there isn't an active map view or scale isn't set
			ZoomCommand = new RelayCommand(async () => { await ZoomToCoordinates(); }, () => MapView.Active != null && Scale != 0);
		}

		internal async Task ZoomToCoordinates()
		{
			await QueuedTask.Run(() =>
			{
				MapView mapView = MapView.Active;
				Camera camera = mapView.Camera;

				// Create new camera object with WGS84, if active map has different spatial reference 
				if (camera.SpatialReference.Wkid != 4326)
				{
					ArcGIS.Core.Geometry.SpatialReference sr = SpatialReferenceBuilder.CreateSpatialReference(4326);
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
					if (map.MapType != ArcGIS.Core.CIM.MapType.Map)
					{
						// https://github.com/Esri/arcgis-pro-sdk/wiki/ProConcepts-GraphicsLayers
						ArcGIS.Desktop.Framework.Dialogs.MessageBox.Show("Graphics are supported in 2D only.", "Error", MessageBoxButton.OK);
						return;
					}

					// map point dependent on map's spatial reference
					MapPoint mapPoint;
	
					// Container that will hold the point graphic
					GraphicsLayerCreationParams graphicsLayerParams = new() { Name = "Graphics Layer" };
					GraphicsLayer graphicsLayer = LayerFactory.Instance.CreateLayer<GraphicsLayer>(layerParams:graphicsLayerParams, container:map);

					if (camera.SpatialReference.Wkid != 4326)
					{
						ArcGIS.Core.Geometry.SpatialReference sr = SpatialReferenceBuilder.CreateSpatialReference(wkid:4326);
						mapPoint = MapPointBuilderEx.CreateMapPoint(new Coordinate2D(Longitude, Latitude), sr);
					}
					else 
					{
						mapPoint = MapPointBuilderEx.CreateMapPoint(new Coordinate2D(Longitude, Latitude), camera.SpatialReference);
					}

					//specify a text symbol
					var text_symbol = SymbolFactory.Instance.ConstructTextSymbol(ColorFactory.Instance.BlackRGB, 8.5, "Corbel", "Regular");
					
					// TODO how to add text/label to graphic
					TextSymbol text = new()
					{
						Text = $"{Longitude} {Latitude}"
					};

					var textGraphic = new CIMTextGraphic
					{
						Symbol = SymbolFactory.Instance.ConstructTextSymbol(ColorFactory.Instance.BlackRGB, 12, "Times New Roman", "Regular").MakeSymbolReference(),
						Text = $"  <b>{Longitude} {Latitude}</b>",
						Shape = mapPoint
					};

					graphicsLayer.AddElement(textGraphic, select:false, elementName:"added text");


					// Create symbol, actual graphic, put them together and add them to the GraphicsLayer container created above
					var pointSymbol = SymbolFactory.Instance.ConstructPointSymbol(color:ColorFactory.Instance.GreenRGB, size:8);
					var cimGraphicElement = GraphicFactory.Instance.CreateSimpleGraphic(geometry:mapPoint, symbol:pointSymbol);
					
					//cimGraphicElement.PopupHtmlText = new(); // "Hello World!";
					ElementFactory.Instance.CreateGraphicElement(elementContainer: graphicsLayer, cimGraphic:cimGraphicElement, elementName:"doesThisWork?", select:false);
				}
			});
		}
	}
}
