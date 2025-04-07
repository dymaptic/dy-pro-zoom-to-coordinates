using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using dymaptic.Pro.ZoomToCoordinates.ViewModels;
using dymaptic.Pro.ZoomToCoordinates.Views;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace dymaptic.Pro.ZoomToCoordinates.MapTools;
internal class GetCoordinatesMapTool : MapTool
{
    private GetCoordinatesWindow? _getCoordinatesWindow = null;
    private readonly Stopwatch _throttleTimer = Stopwatch.StartNew();
    private readonly TimeSpan _throttleDelay = TimeSpan.FromMilliseconds(50);

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

    /// <summary>
    ///     As the user moves the mouse, update the coordinates.  
    ///     Since this could trigger a ton, throttle it to improve performance.
    /// </summary>
    /// <param name="e"></param>
    protected async override void OnToolMouseMove(MapViewMouseEventArgs e)
    {
        if (_throttleTimer.Elapsed < _throttleDelay)
            return; // too soon, skip

        _throttleTimer.Restart(); // otherwise reset timer

        if (_getCoordinatesWindow?.DataContext is not GetCoordinatesViewModel viewModel) { return; }

        MapPoint? mapPoint = await ToWgs84Async(e.ClientPoint);

        if (mapPoint == null) { return; }

        // Guard that user scrolled over the map
        if (mapPoint.X >= -180 && mapPoint.X <= 180 && mapPoint.Y >= -90 && mapPoint.Y <= 90)
        {
            viewModel.MapPoint = mapPoint;
            viewModel.UpdateCoordinates();
        }
    }

    /// <summary>
    ///     Creates a MapPoint when the user clicks the map.
    /// </summary>
    /// <returns></returns>
    protected async override Task HandleMouseDownAsync(MapViewMouseButtonEventArgs e)
    {
        if (_getCoordinatesWindow?.DataContext is not GetCoordinatesViewModel viewModel) { return; }

        MapPoint? mapPoint = await ToWgs84Async(e.ClientPoint);

        if (mapPoint == null) { return; }

        viewModel.MapPoint = mapPoint;
        viewModel.UpdateCoordinates();
        if (viewModel.ShowGraphic)
        {
            await QueuedTask.Run(() => viewModel.CreateGraphic());
        } 
    }

    /// <summary>
    ///     Deactivate the map tool if user closes the Pro Window.
    /// </summary>
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

    /// <summary>
    /// Converts a screen point to a MapPoint in WGS84.
    /// </summary>
    private static Task<MapPoint?> ToWgs84Async(System.Windows.Point clientPoint)
    {
        return QueuedTask.Run(() =>
        {
            var point = MapView.Active?.ClientToMap(clientPoint);
            if (point == null) return null;

            return point.SpatialReference?.Wkid == 4326
                ? point
                : (MapPoint)GeometryEngine.Instance.Project(point, SpatialReferences.WGS84);
        });
    }
}
