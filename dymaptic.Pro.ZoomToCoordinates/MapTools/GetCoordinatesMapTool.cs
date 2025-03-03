using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using dymaptic.Pro.ZoomToCoordinates.ViewModels;
using dymaptic.Pro.ZoomToCoordinates.Views;
using System;
using System.Threading.Tasks;

namespace dymaptic.Pro.ZoomToCoordinates.MapTools;
internal class GetCoordinatesMapTool : MapTool
{
    private GetCoordinatesWindow? _getCoordinatesWindow = null;

    public GetCoordinatesMapTool()
    {
    }

    protected override Task OnToolActivateAsync(bool active)
    {
        // Always ensure the ProWindow opens when the MapTool is activated.
        if (_getCoordinatesWindow == null)
        {
            _getCoordinatesWindow = new()
            {
                Owner = FrameworkApplication.Current.MainWindow
            };
            _getCoordinatesWindow.Closed += OnGetCoordinatesWindowClosed;

            FrameworkApplication.Current.Dispatcher.Invoke(() =>
            {
                _getCoordinatesWindow.Show();
            });
        }
        return base.OnToolActivateAsync(active);
    }


    protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            e.Handled = true; //Handle the event args to get the call to the corresponding async method
    }

    protected async override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
    {
        if (_getCoordinatesWindow?.DataContext is GetCoordinatesViewModel viewModel)
        {
            // When the map is clicked, pass the MapPoint to the GetCoordinatesViewModel
            MapPoint mapPoint = await QueuedTask.Run(() =>
            {
                MapPoint point = MapView.Active.ClientToMap(e.ClientPoint);

                // Check if the point is already in WGS84 (SpatialReference WKID 4326)
                if (point?.SpatialReference?.Wkid != 4326)
                {
                    // Reproject to WGS84 if necessary
                    point = (MapPoint)GeometryEngine.Instance.Project(point, SpatialReferences.WGS84);
                }

                return point;
            });

            if (mapPoint != null)
            {
                viewModel.MapPoint = mapPoint;
                viewModel.UpdateCoordinates();
            }
        }
    }

    private void OnGetCoordinatesWindowClosed(object? o, EventArgs e)
    {
        if (_getCoordinatesWindow != null)
        {
            _getCoordinatesWindow.Closed -= OnGetCoordinatesWindowClosed;
            _getCoordinatesWindow = null;
        }

        // Deactivate the map tool if user closes the Pro Window
        FrameworkApplication.SetCurrentToolAsync("esri_mapping_exploreTool");
    }
}
