# dy-pro-zoom-to-coordinates
An open-source repository of an ArcGIS Pro v3.x Add-In with Zoom to Coordinates functionality in 2D maps and 3D scenes.


## Overview
An optional graphic can be created if requested and multiple default settings can be defined & saved by the user 
(i.e., marker style, marker color, font family, font style & font color). Subsequent times loading the Add-In will 
load the user's chosen settings, and any changes made from the settings window updates them. 

Note that if the user has an active scene (3D), an overlay is added rather than a graphic if a graphic is requested since 
graphics are 2D only. The overlay still respects the marker style and marker color, but the font settings are ignored since
the "label" is a text graphic.

## Screenshot of AddIn in ArcGIS Pro with both windows open
https://user-images.githubusercontent.com/32423158/220716443-6c9ba5a6-1f12-49e1-beda-ac375acc13d9.png


## Esri AddIn Install Documentation
https://support.esri.com/en/technical-article/000026259

## Install Instructions
- Download and unzip directory with the GUID as a name.
- Recommend placing folder in your default ArcGIS Pro AddIn directory (C:\[user profile]\Documents\ArcGIS\AddIns\ArcGISPro)
- Ensure that ArcGIS Pro is configured to find the directory from the step above (ArcGIS Pro -> Add-In Manager -> Options)
- Double-click the actual AddIn inside the directory, dymaptic.Pro.ZoomToCoordinates.esriAddinX
- You may receive an Esri ArcGIS Add-In Installation Utility dialog box saying that installation failed, but this is misleading. 
- Dismiss the dialog and AddIn will be available from the ArcGIS Pro AddIn tab & also viewable here (ArcGIS Pro -> Add-In Manager -> Add-Ins)
