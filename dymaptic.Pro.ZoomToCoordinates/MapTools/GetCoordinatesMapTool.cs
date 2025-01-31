using ArcGIS.Core.CIM;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Catalog;
using ArcGIS.Desktop.Core;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Extensions;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Dialogs;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Layouts;
using ArcGIS.Desktop.Mapping;
using dymaptic.Pro.ZoomToCoordinates.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
    {
        return base.OnSketchCompleteAsync(geometry);
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
