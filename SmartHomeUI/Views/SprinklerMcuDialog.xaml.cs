using System.Windows;
using System.Windows.Input;
using SmartHomeUI.Presentation.ViewModels;
using SmartHomeUI.Services;

namespace SmartHomeUI.Views;

public partial class SprinklerMcuDialog : Window
{
    private readonly SprinklerMcuViewModel _vm = new();

    public SprinklerMcuDialog()
    {
        InitializeComponent();
        DataContext = _vm;
        IrrigationMcuRuntime.SyncWithDevices();
    }

    private void ApplySettings_Click(object sender, RoutedEventArgs e)
    {
        _vm.ApplySettings();
        MessageBox.Show("Settings applied to the virtual sprinkler MCU.", "Saved", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
