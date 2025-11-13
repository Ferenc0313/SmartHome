using System;
using System.Linq;
using System.Windows;
using SmartHomeUI.Data;
using SmartHomeUI.Services;

namespace SmartHomeUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const double DesignWidth = 1600;
        private const double DesignHeight = 900;

        public MainWindow()
        {
            InitializeComponent();
            // Load dashboard by default on startup
            ShowDashboard();
            RefreshMenuState();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => ApplyScaledWindowSize();

        private void NavigateToDashboard(object sender, RoutedEventArgs e) => ShowDashboard();
        private void NavigateToUsers(object sender, RoutedEventArgs e) => ShowUsers();
        private void NavigateToDevices(object sender, RoutedEventArgs e) => ShowDevices();
        private void NavigateToSettings(object sender, RoutedEventArgs e) => DashboardContent.Children.Clear();
        private void NavigateToAutomations(object sender, RoutedEventArgs e) => ShowAutomations();

        // Window control buttons (top-right)
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void ToggleMaximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // Public helpers for programmatic navigation
        public void ShowDashboard()
        {
            DashboardContent.Children.Clear();
            try
            {
                DashboardContent.Children.Add(new Views.DashboardPage());
            }
            catch (Exception ex)
            {
                var fallback = new System.Windows.Controls.Border { Style = (System.Windows.Style)FindResource("GlassPanel"), Margin = new Thickness(10), Padding = new Thickness(16) };
                var txt = new System.Windows.Controls.TextBlock { Text = $"Dashboard failed to load: {ex.Message}", Foreground = (System.Windows.Media.Brush)FindResource("NeonCyan"), FontSize = 16 };
                fallback.Child = txt;
                DashboardContent.Children.Add(fallback);
            }
        }

        public void ShowUsers()
        {
            DashboardContent.Children.Clear();
            if (AuthService.CurrentUser is not null)
                DashboardContent.Children.Add(new Views.AccountPage());
            else
                DashboardContent.Children.Add(new Views.AuthPage());
            RefreshMenuState();
        }

        public void ShowDevices()
        {
            DashboardContent.Children.Clear();
            try
            {
                DashboardContent.Children.Add(new Views.DevicesPage());
            }
            catch (Exception ex)
            {
                var fallback = new System.Windows.Controls.Border { Style = (System.Windows.Style)FindResource("GlassPanel"), Margin = new Thickness(10), Padding = new Thickness(16) };
                var txt = new System.Windows.Controls.TextBlock { Text = $"Devices failed to load: {ex.Message}", Foreground = (System.Windows.Media.Brush)FindResource("NeonCyan"), FontSize = 16 };
                fallback.Child = txt;
                DashboardContent.Children.Add(fallback);
            }
        }

        public void ShowAutomations()
        {
            DashboardContent.Children.Clear();
            DashboardContent.Children.Add(new Views.AutomationsPage());
        }

        public void RefreshMenuState()
        {
            try
            {
                var user = AuthService.CurrentUser;
                bool hasDevices = false;
                if (user != null)
                {
                    using var db = new SmartHomeDbContext();
                    hasDevices = db.Devices.Any(d => d.UserId == user.Id);
                }
                btnAutomations.Visibility = hasDevices ? Visibility.Visible : Visibility.Collapsed;
            }
            catch
            {
                btnAutomations.Visibility = Visibility.Collapsed;
            }
        }

        private void ApplyScaledWindowSize()
        {
            var workArea = SystemParameters.WorkArea;
            var scale = Math.Min(workArea.Width / DesignWidth, workArea.Height / DesignHeight);
            scale = Math.Min(scale, 1.0);

            Width = DesignWidth * scale;
            Height = DesignHeight * scale;

            Left = workArea.Left + (workArea.Width - Width) / 2;
            Top = workArea.Top + (workArea.Height - Height) / 2;
        }
    }
}
