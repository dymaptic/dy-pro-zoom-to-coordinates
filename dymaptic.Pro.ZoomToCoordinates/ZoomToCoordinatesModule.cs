using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace dymaptic.Pro.ZoomToCoordinates
{
	internal class ZoomToCoordinatesModule : Module
	{
		/// <summary>
		/// Retrieve the singleton instance to this module here
		/// </summary>
		public static ZoomToCoordinatesModule Current => _this ??= (ZoomToCoordinatesModule)FrameworkApplication.FindModule("dymaptic.Pro.ZoomToCoordinates_Module");

		private static ZoomToCoordinatesModule _this;

		#region Overrides
		/// <summary>
		/// Called by Framework when ArcGIS Pro is closing
		/// </summary>
		/// <returns>False to prevent Pro from closing, otherwise True</returns>
		protected override bool CanUnload()
		{
			//TODO - add your business logic
			//return false to ~cancel~ Application close
			return true;
		}
		#endregion Overrides
	}
}
