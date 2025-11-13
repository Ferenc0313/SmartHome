using System.Windows.Controls;
using SmartHomeUI.ViewModels;

namespace SmartHomeUI.Views;

public partial class DashboardPage : UserControl
{
    public DashboardPage()
    {
        InitializeComponent();
        var vm = new DashboardViewModel();
        DataContext = vm;
        vm.Load();
    }

    private DashboardViewModel? ViewModel => DataContext as DashboardViewModel;

    private void DeviceTile_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is SmartHomeUI.Models.Device dev)
        {
            SmartHomeUI.Services.DeviceService.TogglePersist(dev.Id);
        }
    }
    private void TilesScroll_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
    {
        if (sender is System.Windows.Controls.ScrollViewer sv)
        {
            var delta = e.Delta;
            var offset = sv.VerticalOffset - System.Math.Sign(delta) * 48;
            offset = System.Math.Max(0, System.Math.Min(offset, sv.ScrollableHeight));
            sv.ScrollToVerticalOffset(offset);
            e.Handled = true;
        }
    }

    private void DeviceListScroll_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is System.Windows.Controls.ScrollViewer sv)
        {
            sv.ScrollToTop();
        }
    }
    private void ValueSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
    {
        if (sender is System.Windows.Controls.Slider s && s.Tag is SmartHomeUI.Models.Device dev)
        {
            SmartHomeUI.Services.DeviceService.SetValue(dev.Id, e.NewValue);
        }
    }

    private void OpenWidgetPicker_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button { DataContext: AddWidgetPlaceholder placeholder })
        {
            ViewModel?.OpenWidgetPicker(placeholder);
        }
    }

    private void CloseWidgetPicker_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        ViewModel?.CloseWidgetPicker();
    }

    private void WidgetOption_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        if (sender is Button btn && btn.DataContext is WidgetOptionViewModel option)
        {
            ViewModel?.AddWidget(option.Kind);
        }
    }
}
