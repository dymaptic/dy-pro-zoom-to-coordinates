using System.Diagnostics;
using System.Windows.Navigation;

namespace dymaptic.Pro.ZoomToCoordinates.Views
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : ArcGIS.Desktop.Framework.Controls.ProWindow
    {
        public About()
        {
            InitializeComponent();
        }

        // Opens hyperlink in user's default web browser
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            };
            Process.Start(psi);

            e.Handled = true;
        }
    }
}
