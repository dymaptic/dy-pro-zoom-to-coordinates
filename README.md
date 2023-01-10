# dy-pro-zoom-to-coordinates
An open-source repository of an ArcGIS Pro v3.x Add-In with Zoom to Coordinates functionality in 2D maps and 3D scenes.


## Overview
An optional graphic can be created if requested and multiple default settings can be defined & saved by the user 
(i.e., marker style, marker color, font family, font style & font color). Subsequent times loading the Add-In will 
load the user's chosen settings, and any changes made from the settings window updates them. 

Note that if the user has an active scene (3D), an overlay is added rather than a graphic if a graphic is requested since 
graphics are 2D only. The overlay still respects the marker style and marker color, but the font settings are ignored since
the "label" is a text graphic.
