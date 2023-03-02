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
- Open the directory with the GUID as name and double-click dymaptic.Pro.ZoomToCoordinates.esriAddinX.
	- Double-clicking .esriAddinX will copy the AddIn to your default ArcGIS Pro AddIn directory (C:\[user profile]\Documents\ArcGIS\AddIns\ArcGISPro)
	- The installed AddIn is also visible from ArcGIS Pro (ArcGIS Pro Project tab -> Add-In Manager -> My Add-Ins)
	- Ensure that ArcGIS Pro is configured to find your Add-In directory after double-clicking .esriAddinX (ArcGIS Pro Project tab -> Add-In Manager -> Options)
- The ZoomToCoordinates AddIn is now available for use from the ArcGIS Pro AddIn tab.