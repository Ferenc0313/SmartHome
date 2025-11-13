using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using SmartHomeUI.ViewModels;
using SmartHomeUI.Services;

namespace SmartHomeUI.Views;

public partial class AuthPage : UserControl
{
    private readonly AuthViewModel _vm = new();
    private readonly DispatcherTimer _snackTimer = new() { Interval = TimeSpan.FromSeconds(3) };

    public AuthPage()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.Registered += msg => { ShowSnack(msg); TogglePanels(showRegister: false); };
        _vm.LoggedIn += user =>
        {
            // Load device context for user and navigate to Dashboard
            Services.DeviceService.LoadForUser(user.Id);
            var win = Window.GetWindow(this) as MainWindow;
            win?.ShowDashboard();
            win?.RefreshMenuState();
        };
        _snackTimer.Tick += (_, __) => { Snack.Visibility = Visibility.Collapsed; Snack.Opacity = 0; _snackTimer.Stop(); };
    }

    private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
            _vm.LoginPassword = pb.Password;
    }

    private void RegPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
            _vm.RegPassword = pb.Password;
    }

    private void RegConfirmBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox pb)
            _vm.RegConfirm = pb.Password;
    }

    private void OpenRegister_Click(object sender, RoutedEventArgs e) => TogglePanels(showRegister: true);
    private void CancelRegister_Click(object sender, RoutedEventArgs e) => TogglePanels(showRegister: false);

    private void TogglePanels(bool showRegister)
    {
        SignInPanel.Visibility = showRegister ? Visibility.Collapsed : Visibility.Visible;
        RegisterPanel.Visibility = showRegister ? Visibility.Visible : Visibility.Collapsed;
    }

    private void ShowSnack(string message)
    {
        SnackText.Text = message;
        Snack.Visibility = Visibility.Visible;
        Snack.Opacity = 1;
        _snackTimer.Stop();
        _snackTimer.Start();
    }
}
