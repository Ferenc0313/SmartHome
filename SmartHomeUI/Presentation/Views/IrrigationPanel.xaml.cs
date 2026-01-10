using System.Windows.Controls;
using SmartHomeUI.Presentation.ViewModels;
using SmartHomeUI.Services;

namespace SmartHomeUI.Presentation.Views;

public partial class IrrigationPanel : UserControl
{
    private readonly SprinklerMcuViewModel _vm = new();

    public IrrigationPanel()
    {
        InitializeComponent();
        DataContext = _vm;
        IrrigationMcuRuntime.SyncWithDevices();
    }
}
