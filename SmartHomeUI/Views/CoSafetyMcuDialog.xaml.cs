using System.Windows;
using System.Windows.Input;
using SmartHomeUI.Presentation.ViewModels;

namespace SmartHomeUI.Views;

public partial class CoSafetyMcuDialog : Window
{
    private readonly CoSafetyMcuViewModel _vm = new();

    public CoSafetyMcuDialog()
    {
        InitializeComponent();
        DataContext = _vm;
    }

    private void ApplySettings_Click(object sender, RoutedEventArgs e)
    {
        _vm.ApplySettings();
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
