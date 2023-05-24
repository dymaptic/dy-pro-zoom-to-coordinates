using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;

namespace dymaptic.Pro.ZoomToCoordinates.Views
{
    internal class ShowAbout : Button
    {

        private About? _about = null;

        protected override void OnClick()
        {
            //already open?
            if (_about != null)
                return;
            _about = new About
            {
                Owner = FrameworkApplication.Current.MainWindow
            };
            _about.Closed += (o, e) => { _about = null; };
            _about.Show();
            //uncomment for modal
            //_about.ShowDialog();
        }
    }
}
