using System.Windows;
using System.Windows.Controls;
using SmartHomeUI.ViewModels;
using SmartHomeUI.Views;
using SmartHomeUI.Data;
using SmartHomeUI.Models;

namespace SmartHomeUI.Views;

public partial class UsersPage : UserControl
{
    public UsersPage()
    {
        InitializeComponent();
        var vm = new UsersViewModel();
        DataContext = vm;
        // Load on first show
        vm.LoadUsersCommand.Execute(null);
    }

    private void AddDevice_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        try
        {
            if (sender is Button btn && btn.DataContext is User user)
            {
                var dlg = new AddDeviceDialog { Owner = Window.GetWindow(this) };
                var result = dlg.ShowDialog();
                if (result == true && dlg.SelectedOption is not null)
                {
                    using var db = new SmartHomeDbContext();
                    var device = new Device{ Name = dlg.SelectedOption.Name, IconKey = dlg.SelectedOption.IconKey, Type = dlg.SelectedOption.Type, UserId = user.Id };
                    db.Devices.Add(device);
                    db.SaveChanges();
                    if (Window.GetWindow(this) is MainWindow mw) mw.RefreshMenuState();
                }
            }
        }
        catch (System.Exception ex)
        {
            try { System.IO.File.AppendAllText("error.log", $"[AddDevice.UsersPage] {System.DateTime.Now}: {ex}\n\n"); } catch { }
            MessageBox.Show($"Failed to add device:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

