﻿using ArcGIS.Core.CIM;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dymaptic.Pro.ZoomToCoordinates.MapTools;
internal class GetCoordinates : MapTool
{
    public GetCoordinates()
    {
        IsSketchTool = true;
        SketchType = SketchGeometryType.Rectangle;
        SketchOutputMode = SketchOutputMode.Map;
    }

    protected override Task OnToolActivateAsync(bool active)
    {
        return base.OnToolActivateAsync(active);
    }

    protected override Task<bool> OnSketchCompleteAsync(Geometry geometry)
    {
        return base.OnSketchCompleteAsync(geometry);
    }
}
