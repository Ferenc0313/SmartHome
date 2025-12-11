using System.Windows.Controls;
using SmartHomeUI.Presentation.ViewModels;

namespace SmartHomeUI.Presentation.Views;

public partial class CoSafetyPanel : UserControl
{
    public CoSafetyPanel()
    {
        InitializeComponent();
        DataContext = new CoSafetyViewModel();
    }
}
