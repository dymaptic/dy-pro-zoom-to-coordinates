using dymaptic.Pro.ZoomToCoordinates.ViewModels;

namespace dymaptic.Pro.ZoomToCoordinates.Views;
/// <summary>
/// Interaction logic for GetCoordinatesView.xaml
/// </summary>
public partial class GetCoordinatesWindow : ArcGIS.Desktop.Framework.Controls.ProWindow
{
    public GetCoordinatesWindow()
    {
        InitializeComponent();
        DataContext = new GetCoordinatesViewModel();
    }
}
