
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using SmartHomeUI.Data;
using SmartHomeUI.Models;
using SmartHomeUI.Services;

namespace SmartHomeUI.Views;

public partial class DevicesPage : UserControl
{
    private System.Collections.Generic.List<Device> _all = new System.Collections.Generic.List<Device>();

    public DevicesPage()
    {
        InitializeComponent();
        // Defer initial loading until the control is fully loaded.
        // During InitializeComponent some change events (e.g., ComboBox SelectionChanged)
        // can fire before named elements like List are assigned, causing NREs.
        this.Loaded += (_, __) =>
        {
            LoadDevices();
            ApplyFilter();
        };
    }

    private void LoadDevices()
    {
        List.ItemsSource = null;
        var user = AuthService.CurrentUser;
        if (user == null) return;
        using var db = new SmartHomeDbContext();
        _all = db.Devices.Where(d => d.UserId == user.Id).OrderBy(d => d.Name).ToList();
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        // If called too early during XAML initialization, List may not be wired yet.
        if (List == null) return;
        var text = SearchBox?.Text?.Trim() ?? string.Empty;

        System.Collections.Generic.IEnumerable<Device> q = _all;
        if (!string.IsNullOrWhiteSpace(text))
            q = q.Where(d => d.Name.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0);
        // Removed type/favorites/room filters for a simpler UI

        List.ItemsSource = q.ToList();
    }

    private void AddDevice_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var user = AuthService.CurrentUser;
            if (user == null)
            {
                MessageBox.Show("Please sign in first.");
                return;
            }
            var win = Window.GetWindow(this);
            var dlg = new AddDeviceDialog { Owner = win as Window };
            var result = dlg.ShowDialog();
            if (result == true && dlg.SelectedOption is not null)
            {
                using var db = new SmartHomeDbContext();
                var device = new Device { Name = dlg.SelectedOption.Name, IconKey = dlg.SelectedOption.IconKey, Type = dlg.SelectedOption.Type, UserId = user.Id };
                db.Devices.Add(device);
                db.SaveChanges();
            }
            LoadDevices();
            DeviceService.ReloadForCurrentUser();
            if (Window.GetWindow(this) is MainWindow mw) mw.RefreshMenuState();
        }
        catch (System.Exception ex)
        {
            try { System.IO.File.AppendAllText("error.log", $"[AddDevice] {System.DateTime.Now}: {ex}\n\n"); } catch { }
            MessageBox.Show($"Failed to add device:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Removed extra filters; keep only text search
    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void SaveDetails_Click(object sender, RoutedEventArgs e)
    {
        if (List.SelectedItem is Device dev)
        {
            using var db = new SmartHomeDbContext();
            var tracked = db.Devices.FirstOrDefault(d => d.Id == dev.Id);
            if (tracked is not null)
            {
                tracked.Room = RoomEdit.Text;
                tracked.Favorite = FavoriteEdit.IsChecked == true;
                db.SaveChanges();
            }
            LoadDevices();
        }
    }

    private void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Device dev)
        {
            DeviceService.TogglePersist(dev.Id);
            LoadDevices();
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Device dev)
        {
            if (MessageBox.Show($"Delete '{dev.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                using var db = new SmartHomeDbContext();
                var tracked = db.Devices.FirstOrDefault(d => d.Id == dev.Id);
                if (tracked is not null)
                {
                    db.Devices.Remove(tracked);
                    db.SaveChanges();
                }
                LoadDevices();
                DeviceService.ReloadForCurrentUser();
                if (Window.GetWindow(this) is MainWindow mw) mw.RefreshMenuState();
            }
        }
    }
}
