# dy-pro-zoom-to-coordinates
An open-source repository of an ArcGIS Pro v3.3+ Add-In.

## 📍 Coordinates Add-In for ArcGIS Pro
The Coordinates Add-In enhances your ArcGIS Pro experience by enabling precise coordinate input, dynamic coordinate retrieval, and highly customizable map graphics—all with support for multiple coordinate formats. Whether you’re zooming to a location or extracting coordinates on the fly, this add-in offers accuracy, flexibility, and ease of use.

### 🔍 Zoom To Coordinates
Navigate directly to a location by entering coordinates in your preferred format:

- Choose from five supported coordinate formats:
	- Decimal Degrees
	- Degrees/Minutes/Seconds
	- Degrees Decimal Minutes
	- UTM
	- MGRS (Military Grid Reference System)
- When using UTM or MGRS, dropdowns for UTM Zone and Latitude Band appear, guiding accurate input and providing helpful context.
- For MGRS, a dynamic 100 km Grid ID dropdown is also available, updating in real time based on your selections.
- Coordinates are automatically converted when switching between formats.
- Optionally toggle between formatted and unformatted coordinate views.
- Enable the Create Graphic option to drop a labeled point on the map when zooming.

### 🗺️ Get Coordinates
Quickly retrieve live coordinates by moving the mouse across the map:
- Display coordinates in any of the five supported formats listed above.
- As you drag your mouse across the map, the coordinates update instantly.
- Convert coordinates on the fly by selecting a different format.
- Toggle a formatted view for more readable output.
- Enable Create Graphic mode to drop a labeled graphic at the clicked location.

### ⚙️ Coordinate Settings
Customize the tool to match your preferences and workflow:

- Location Settings:
	- Set a default starting location (latitude and longitude)
    	- Define a default zoom scale
	- Choose whether to create graphics by default

- Graphic Settings:
	- Customize graphic appearance including:
		- Style, size, and color
		- Font family, size, style, and color
	- Choose whether to show formatted coordinates by default

Settings are saved with your ArcGIS Pro project and persist across sessions as long as the project is saved.

### 🏢 About dymaptic
The Coordinates Add-In is developed by [dymaptic](https://dymaptic.com) —a leading, woman-owned GIS software development company and proud Esri Gold Partner.

We specialize in crafting innovative, tailored GIS solutions, including custom Add-In development for ArcGIS Pro and ArcGIS Enterprise.

🔗 Visit our [website](https://dymaptic.com) to learn more about our services and explore our growing portfolio of cutting-edge GIS tools.


---


##### Screenshot of AddIn in ArcGIS Pro
<img width="899" alt="Coordinates Addin" src="https://github.com/user-attachments/assets/b6f806b4-4449-4fe5-8ba8-b75bffdc98b4" />

---

##### Esri AddIn Install Documentation
https://support.esri.com/en/technical-article/000026259

##### Install Instructions
- Download and unzip directory with the GUID as a name.
- Open the directory with the GUID as name and double-click dymaptic.Pro.ZoomToCoordinates.esriAddinX.
	- Double-clicking .esriAddinX will copy the AddIn to your default ArcGIS Pro AddIn directory (C:\[user profile]\Documents\ArcGIS\AddIns\ArcGISPro)
	- The installed AddIn is also visible from ArcGIS Pro (ArcGIS Pro Project tab -> Add-In Manager -> My Add-Ins)
	- Ensure that ArcGIS Pro is configured to find your Add-In directory after double-clicking .esriAddinX (ArcGIS Pro Project tab -> Add-In Manager -> Options)
- The ZoomToCoordinates AddIn is now available for use from the ArcGIS Pro AddIn tab.
