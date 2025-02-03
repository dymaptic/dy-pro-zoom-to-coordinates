using ActiproSoftware.Windows.Controls.Ribbon.Controls;
using dymaptic.Pro.ZoomToCoordinates.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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

        // Attach KeyDown event
        this.PreviewKeyDown += OnProWindowKeyDown;
    }

    private void OnProWindowKeyDown(object sender, KeyEventArgs e)
    {
        // Check if Ctrl + C is pressed
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.C)
        {
            // Find the target TextBox (replace 'myTextBox' with the actual name)
            if (FullReference != null && !string.IsNullOrEmpty(FullReference.Text))
            {
                Clipboard.SetText(FullReference.Text);
                MessageBox.Show("Text copied to clipboard!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}
