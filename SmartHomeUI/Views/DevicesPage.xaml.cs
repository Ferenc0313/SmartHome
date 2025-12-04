
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using SmartHomeUI.Data;
using SmartHomeUI.Models;
using SmartHomeUI.Services;
using SmartHomeUI.ViewModels;

namespace SmartHomeUI.Views;

public partial class DevicesPage : UserControl
{
    private readonly System.Collections.Generic.List<DeviceListItem> _all = new System.Collections.Generic.List<DeviceListItem>();
    private readonly IDeviceTypeResolver _typeResolver = new DeviceTypeResolver();
    private readonly IDeviceIconResolver _iconResolver = new DeviceIconResolver();
    private readonly SmartThingsDeviceService _stDeviceService = new SmartThingsDeviceService(new HttpClient());

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
        var virtuals = db.Devices.Where(d => d.UserId == user.Id).OrderBy(d => d.Name).ToList();
        _all.Clear();
        foreach (var d in virtuals)
        {
            _all.Add(new DeviceListItem
            {
                DbId = d.Id,
                IsPhysical = false,
                Name = d.Name,
                IconKey = d.IconKey,
                Type = d.Type,
                Room = d.Room,
                Favorite = d.Favorite,
                IsOn = d.IsOn,
                DeviceType = DeviceType.Unknown // legacy virtual types
            });
        }
        ApplyFilter();
    }

    private void ApplyFilter()
    {
        // If called too early during XAML initialization, List may not be wired yet.
        if (List == null) return;
        var text = SearchBox?.Text?.Trim() ?? string.Empty;

        System.Collections.Generic.IEnumerable<DeviceListItem> q = _all;
        if (!string.IsNullOrWhiteSpace(text))
            q = q.Where(d => d.Name.IndexOf(text, System.StringComparison.OrdinalIgnoreCase) >= 0);

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
        if (List.SelectedItem is DeviceListItem item && item.IsPhysical == false && item.DbId is int dbId)
        {
            using var db = new SmartHomeDbContext();
            var tracked = db.Devices.FirstOrDefault(d => d.Id == dbId);
            if (tracked is not null)
            {
                tracked.Room = RoomEdit.Text;
                tracked.Favorite = FavoriteEdit.IsChecked == true;
                db.SaveChanges();
            }
            LoadDevices();
        }
    }

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DeviceListItem item)
        {
            if (item.IsPhysical)
            {
                await TogglePhysicalAsync(item);
            }
            else if (item.DbId is int dbId)
            {
                DeviceService.TogglePersist(dbId);
                LoadDevices();
            }
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is DeviceListItem item)
        {
            if (MessageBox.Show($"Delete '{item.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                if (item.IsPhysical)
                {
                    _all.Remove(item);
                }
                else if (item.DbId is int dbId)
                {
                    using var db = new SmartHomeDbContext();
                    var tracked = db.Devices.FirstOrDefault(d => d.Id == dbId);
                    if (tracked is not null)
                    {
                        db.Devices.Remove(tracked);
                        db.SaveChanges();
                    }
                }
                LoadDevices();
                DeviceService.ReloadForCurrentUser();
                if (Window.GetWindow(this) is MainWindow mw) mw.RefreshMenuState();
            }
        }
    }

    private async void AddPhysical_Click(object sender, RoutedEventArgs e)
    {
        var user = AuthService.CurrentUser;
        if (user == null)
        {
            MessageBox.Show("Please sign in first.");
            return;
        }

        var pat = NormalizePat(AuthService.CurrentSmartThingsPat ?? System.Environment.GetEnvironmentVariable("SMARTTHINGS_PAT"));
        if (string.IsNullOrWhiteSpace(pat))
        {
            MessageBox.Show("Nincs SmartThings PAT a felhasználóhoz vagy környezetben. Adj meg egy érvényes PAT-et regisztrációkor.", "Hiányzó token", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            var stDevices = await _stDeviceService.ListDevicesAsync(pat);
            var vm = new AddPhysicalDeviceDialogViewModel();
            vm.LoadDevices(stDevices, _typeResolver, _iconResolver);
            var dlg = new AddPhysicalDeviceDialog(vm) { Owner = Window.GetWindow(this) as Window };
            var res = dlg.ShowDialog();
            if (res == true)
            {
                using var db = new SmartHomeDbContext();
                foreach (var pd in dlg.SelectedDevices)
                {
                    // skip if already exists for this user
                    var exists = db.Devices.Any(d => d.UserId == user.Id && d.PhysicalDeviceId == pd.Id && d.IsPhysical);
                    if (exists) continue;

                    var dev = new Device
                    {
                        Name = pd.Name,
                        IconKey = pd.IconKey,
                        Type = pd.DeviceType.ToString(),
                        Room = string.Empty,
                        Favorite = false,
                        IsOn = false,
                        IsOnline = true,
                        IsPhysical = true,
                        PhysicalDeviceId = pd.Id,
                        UserId = user.Id
                    };
                    db.Devices.Add(dev);
                }
                db.SaveChanges();
                LoadDevices();
                DeviceService.ReloadForCurrentUser();
            }
        }
        catch (System.Exception ex)
        {
            try { System.IO.File.AppendAllText("error.log", $"[AddPhysical] {System.DateTime.Now}: {ex}\n\n"); } catch { }
            MessageBox.Show($"SmartThings eszközlista hiba: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task TogglePhysicalAsync(DeviceListItem item)
    {
        var pat = NormalizePat(AuthService.CurrentSmartThingsPat ?? System.Environment.GetEnvironmentVariable("SMARTTHINGS_PAT"));
        if (string.IsNullOrWhiteSpace(pat) || string.IsNullOrWhiteSpace(item.PhysicalId))
        {
            MessageBox.Show("Hiányzó PAT vagy deviceId.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            using var http = new HttpClient { BaseAddress = new System.Uri("https://api.smartthings.com/v1/") };
            var client = new SmartThingsClient(http, pat);
            if (item.IsOn)
                await client.TurnOffAsync(item.PhysicalId);
            else
                await client.TurnOnAsync(item.PhysicalId);
            item.IsOn = !item.IsOn;
            ApplyFilter();
        }
        catch (System.Exception ex)
        {
            try { System.IO.File.AppendAllText("error.log", $"[TogglePhysical] {System.DateTime.Now}: {ex}\n\n"); } catch { }
            MessageBox.Show($"SmartThings kapcsolás hiba: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static string NormalizePat(string? pat)
    {
        if (string.IsNullOrWhiteSpace(pat)) return string.Empty;
        return new string(pat.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }
}
