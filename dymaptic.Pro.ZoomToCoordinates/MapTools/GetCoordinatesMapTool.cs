using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing.Controls;
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
    /// <summary>
    ///     Defines the ViewModel as a property since we're accessing it a bunch.
    /// </summary>
    private GetCoordinatesViewModel? ViewModel;

    /// <summary>
    ///     Tracks whether coordinate updates are frozen (paused)
    /// </summary>
    private bool _isFrozen = false;

    protected override Task OnToolActivateAsync(bool active)
    {
        // Always ensure the ProWindow opens when the MapTool is activated.
        if (_getCoordinatesWindow == null)
        {
            _getCoordinatesWindow = new GetCoordinatesWindow()
            {
                Owner = FrameworkApplication.Current.MainWindow
            };
            _getCoordinatesWindow.Closed += OnGetCoordinatesWindowClosed;

            FrameworkApplication.Current.Dispatcher.Invoke(() =>
            {
                _getCoordinatesWindow.Show();
            });
        }

        // Access the WPF UI thread to safely retrieve the ViewModel from the DataContext.
        // WPF elements (like DataContext) are bound to the UI thread and cannot be accessed from background threads.
        // This ensures thread-safe interaction from within the MapTool code.
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ViewModel = _getCoordinatesWindow?.DataContext as GetCoordinatesViewModel;
            if (ViewModel is not null)
            {
                ViewModel.Activated = true;
            }
        });

        return base.OnToolActivateAsync(active);
    }

    /// <summary>
    ///     When the tool deactivates, notify the ViewModel so that we can provide instructions to the user how to reactivate it.
    /// </summary>
    /// <param name="hasMapViewChanged"></param>
    /// <returns></returns>
    protected override Task OnToolDeactivateAsync(bool hasMapViewChanged)
    {
        if (ViewModel is not null)
        {
            ViewModel.Activated = false;
        }

        return base.OnToolDeactivateAsync(hasMapViewChanged);
    }


    protected override void OnToolMouseDown(MapViewMouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
            e.Handled = true; //Handle the event args to get the call to the corresponding async method
    }

    protected override void OnToolDoubleClick(MapViewMouseButtonEventArgs e)
    {
        if (e.ChangedButton == System.Windows.Input.MouseButton.Left)
        {
            // Toggle freeze state
            _isFrozen = !_isFrozen;

            // Update ViewModel with freeze state
            if (ViewModel is not null)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ViewModel.IsFrozen = _isFrozen;

                    // Copy coordinates to clipboard when freezing
                    if (_isFrozen)
                    {
                        ViewModel.CopyText();
                    }
                });
            }

            e.Handled = true;
        }
    }

    /// <summary>
    ///     As the user moves the mouse, update the coordinates.
    ///     Since this could trigger a ton, throttle it to improve performance.
    /// </summary>
    /// <param name="e"></param>
    protected async override void OnToolMouseMove(MapViewMouseEventArgs e)
    {
        // Don't update if coordinates are frozen
        if (_isFrozen)
            return;

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
        if (ViewModel is null) { return; }

        MapPoint? mapPoint = await ToWgs84Async(e.ClientPoint);

        if (mapPoint == null) { return; }

        ViewModel.MapPoint = mapPoint;
        ViewModel.UpdateCoordinates();
        if (ViewModel.ShowGraphic)
        {
            await QueuedTask.Run(() => ViewModel.CreateGraphic());
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

        // Reset frozen state when window is closed
        _isFrozen = false;

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

    private GetCoordinatesWindow? _getCoordinatesWindow = null;
    private readonly Stopwatch _throttleTimer = Stopwatch.StartNew();
    private readonly TimeSpan _throttleDelay = TimeSpan.FromMilliseconds(50);
}
