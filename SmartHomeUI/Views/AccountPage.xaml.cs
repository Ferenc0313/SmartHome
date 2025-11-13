using System.Windows;
using System.Windows.Controls;
using SmartHomeUI.Services;

namespace SmartHomeUI.Views;

public partial class AccountPage : UserControl
{
    public AccountPage()
    {
        InitializeComponent();
        var u = AuthService.CurrentUser;
        if (u != null)
        {
            UserNameText.Text = u.Name;
            EmailText.Text = string.IsNullOrWhiteSpace(u.Email) ? "N/A" : u.Email;
        }
    }

    private void Logout_Click(object sender, RoutedEventArgs e)
    {
        AuthService.LogOut();
        Services.DeviceService.Clear();
        var win = Window.GetWindow(this) as MainWindow;
        win?.ShowUsers();
        win?.RefreshMenuState();
    }
}
