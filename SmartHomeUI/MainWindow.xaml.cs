using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
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
        private readonly GridLength _sidebarExpandedWidth = new(280);
        private readonly GridLength _sidebarCollapsedWidth = new(0);
        private bool _isSidebarCollapsed;

        public MainWindow()
        {
            InitializeComponent();
            // Load dashboard by default on startup
            ShowDashboard();
            RefreshMenuState();
            UpdateSidebarState();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) => ApplyScaledWindowSize();

        private void NavigateToDashboard(object sender, RoutedEventArgs e) => ShowDashboard();
        private void NavigateToUsers(object sender, RoutedEventArgs e) => ShowUsers();
        private void NavigateToDevices(object sender, RoutedEventArgs e) => ShowDevices();
        private void NavigateToSettings(object sender, RoutedEventArgs e) => ShowSettings();
        private void NavigateToAutomations(object sender, RoutedEventArgs e) => ShowAutomations();

        // Window control buttons (top-right)
        private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
        private void ToggleMaximize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
        private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
        {
            _isSidebarCollapsed = !_isSidebarCollapsed;
            UpdateSidebarState();
        }

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
            SetActiveMenu(btnDashboard);
        }

        public void ShowUsers()
        {
            DashboardContent.Children.Clear();
            if (AuthService.CurrentUser is not null)
                DashboardContent.Children.Add(new Views.AccountPage());
            else
                DashboardContent.Children.Add(new Views.AuthPage());
            RefreshMenuState();
            SetActiveMenu(btnUsers);
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
            RefreshMenuState();
            SetActiveMenu(btnDevices);
        }

        public void ShowAutomations()
        {
            DashboardContent.Children.Clear();
            DashboardContent.Children.Add(new Views.AutomationsPage());
            RefreshMenuState();
            SetActiveMenu(btnAutomations);
        }

        public void ShowSettings()
        {
            DashboardContent.Children.Clear();
            RefreshMenuState();
            SetActiveMenu(btnSettings);
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

            if (btnAutomations.Visibility == Visibility.Collapsed && ReferenceEquals(_activeMenuButton, btnAutomations))
            {
                SetActiveMenu(btnDevices);
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

        private void UpdateSidebarState()
        {
            SidebarColumn.Width = _isSidebarCollapsed ? _sidebarCollapsedWidth : _sidebarExpandedWidth;
            SidebarToggleButton.Visibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
            SidebarToggleButtonCollapsed.Visibility = _isSidebarCollapsed ? Visibility.Visible : Visibility.Collapsed;

            SidebarToggleLabel.Text = "<";
            SidebarToggleLabelCollapsed.Text = ">";

            SidebarPanel.ClipToBounds = !_isSidebarCollapsed;
            SidebarPanel.BorderThickness = _isSidebarCollapsed ? new Thickness(0) : new Thickness(0, 0, 1, 0);
            SidebarContent.Visibility = _isSidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;

            var toolTipText = _isSidebarCollapsed ? "Oldals\u00E1v megnyit\u00E1sa" : "Oldals\u00E1v bez\u00E1r\u00E1sa";
            SidebarToggleButton.ToolTip = toolTipText;
            SidebarToggleButtonCollapsed.ToolTip = toolTipText;
        }

        private Button? _activeMenuButton;

        private void SetActiveMenu(Button? active)
        {
            _activeMenuButton = active;
            var activeStyle = (Style)FindResource("ActiveMenuButtonStyle");
            var normalStyle = (Style)FindResource("MenuButtonStyle");

            foreach (var button in new[] { btnDashboard, btnUsers, btnDevices, btnAutomations, btnSettings })
            {
                button.Style = ReferenceEquals(button, active) ? activeStyle : normalStyle;
            }
        }
    }
}
