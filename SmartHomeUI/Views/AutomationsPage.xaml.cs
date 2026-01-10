using System;
using System.Linq;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using SmartHomeUI.Data;
using SmartHomeUI.Models;
using SmartHomeUI.Services;

namespace SmartHomeUI.Views;

public partial class AutomationsPage : UserControl
{
    private class AutomationItem
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public int DeviceId { get; init; }
        public string DeviceName { get; init; } = string.Empty;
        public string TimeHHmm { get; init; } = "00:00";
        public string Action { get; init; } = "Toggle";
        public double Value { get; init; }
        public bool Enabled { get; init; }
        public string DisplayAction { get; init; } = string.Empty;
        public string DisplayValue { get; init; } = string.Empty;
    }

    private System.Collections.Generic.List<AutomationItem> _all = new();
    private int? _editingAutomationId = null;

    public AutomationsPage()
    {
        InitializeComponent();
        Loaded += (_, __) => UpdateActionValueUi();
        LoadData();
    }

    private string GetSelectedActionKey()
    {
        if (ActionBox.SelectedItem is ComboBoxItem item)
        {
            var tag = item.Tag?.ToString();
            if (!string.IsNullOrWhiteSpace(tag)) return tag!;
            return item.Content?.ToString() ?? "SetOnOff";
        }
        return "SetOnOff";
    }

    private void ActionBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateActionValueUi();
    }

    private void UpdateActionValueUi()
    {
        if (ActionBox is null || ValueLabel is null || ValueBox is null || OnOffBox is null) return;
        var actionKey = GetSelectedActionKey();
        if (actionKey.Equals("SetValue", StringComparison.OrdinalIgnoreCase))
        {
            ValueLabel.Text = "Value";
            ValueBox.Visibility = Visibility.Visible;
            OnOffBox.Visibility = Visibility.Collapsed;
        }
        else
        {
            ValueLabel.Text = "State";
            ValueBox.Visibility = Visibility.Collapsed;
            OnOffBox.Visibility = Visibility.Visible;
        }
    }

    private void LoadData()
    {
        ResetEditingState();
        using var db = new SmartHomeDbContext();
        var user = Services.AuthService.CurrentUser;
        var uid = user?.Id ?? 0;
        var devices = db.Devices.Where(d => d.UserId == uid).OrderBy(d => d.Name).ToList();
        DevicesBox.ItemsSource = devices;
        var devMap = devices.ToDictionary(d => d.Id, d => d.Name);
        _all = db.Automations.Where(a => a.UserId == uid)
            .OrderBy(a => a.TimeHHmm)
            .AsEnumerable()
            .Select(a => new AutomationItem
            {
                Id = a.Id,
                Name = a.Name,
                DeviceId = a.DeviceId,
                DeviceName = devMap.TryGetValue(a.DeviceId, out var n) ? n : $"Device #{a.DeviceId}",
                TimeHHmm = a.TimeHHmm,
                Action = a.Action,
                Value = a.Value,
                Enabled = a.Enabled,
                DisplayAction = BuildDisplayAction(a.Action),
                DisplayValue = BuildDisplayValue(a.Action, a.Value)
            }).ToList();
        ApplyFilter();
    }

    private static string BuildDisplayAction(string? action)
    {
        if (string.IsNullOrWhiteSpace(action)) return "Toggle";
        if (action.Equals("SetOnOff", StringComparison.OrdinalIgnoreCase)) return "Toggle";
        return action;
    }

    private static string BuildDisplayValue(string? action, double value)
    {
        if (string.IsNullOrWhiteSpace(action)) return string.Empty;
        if (action.Equals("SetOnOff", StringComparison.OrdinalIgnoreCase))
            return value >= 0.5 ? " ON" : " OFF";
        if (action.Equals("SetValue", StringComparison.OrdinalIgnoreCase))
            return $" {value.ToString(CultureInfo.InvariantCulture)}";
        // legacy toggle doesn't need a numeric value in the list
        if (action.Equals("Toggle", StringComparison.OrdinalIgnoreCase))
            return string.Empty;
        return $" {value.ToString(CultureInfo.InvariantCulture)}";
    }

    private void AddOrUpdate_Click(object sender, RoutedEventArgs e)
    {
        var user = Services.AuthService.CurrentUser;
        if (user == null) { MessageBox.Show("Please sign in first."); return; }
        int deviceId;
        if (DevicesBox.SelectedValue is int selectedId)
        {
            deviceId = selectedId;
        }
        else if (DevicesBox.SelectedItem is Device dev)
        {
            deviceId = dev.Id;
        }
        else
        {
            MessageBox.Show("Select device");
            return;
        }
        var actionKey = GetSelectedActionKey();
        var timeRaw = TimeBox.Text.Trim();
        if (!TimeSpan.TryParseExact(timeRaw, @"hh\:mm", CultureInfo.InvariantCulture, out var ts))
        {
            MessageBox.Show("Time must be HH:mm (24h)");
            return;
        }
        var time = new DateTime(2000, 1, 1, ts.Hours, ts.Minutes, 0).ToString("HH:mm");
        var action = actionKey;
        double val = 0;
        if (actionKey.Equals("SetValue", StringComparison.OrdinalIgnoreCase))
        {
            if (!double.TryParse(ValueBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out val))
                val = 0;
        }
        else if (actionKey.Equals("SetOnOff", StringComparison.OrdinalIgnoreCase))
        {
            val = (OnOffBox.SelectedItem as ComboBoxItem)?.Tag?.ToString() == "1" ? 1 : 0;
        }
        using var db = new SmartHomeDbContext();
        if (_editingAutomationId.HasValue)
        {
            var tracked = db.Automations.FirstOrDefault(a => a.Id == _editingAutomationId.Value && a.UserId == user.Id);
            if (tracked is null)
            {
                MessageBox.Show("Automation was not found. It may have been removed.");
                ResetEditingState();
                LoadData();
                return;
            }
            tracked.Name = NameBox.Text.Trim();
            tracked.DeviceId = deviceId;
            tracked.TimeHHmm = time;
            tracked.Action = action;
            tracked.Value = val;
            db.SaveChanges();
        }
        else
        {
            db.Automations.Add(new Automation
            {
                Name = NameBox.Text.Trim(),
                UserId = user.Id,
                DeviceId = deviceId,
                TimeHHmm = time,
                Action = action,
                Value = val,
                Enabled = true
            });
            db.SaveChanges();
        }
        ResetEditingState();
        LoadData();
    }

    private void RunNow_Click(object sender, RoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)?.Tag;
        if (tag is Automation a)
            AutomationService.RunNow(a.Id);
        else if (tag is AutomationItem item)
            AutomationService.RunNow(item.Id);
    }

    private void ToggleEnabled_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is Automation a)
        {
            using var db = new SmartHomeDbContext();
            var tracked = db.Automations.First(x => x.Id == a.Id);
            tracked.Enabled = !tracked.Enabled;
            db.SaveChanges();
            LoadData();
        }
    }

    private void Enabled_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is AutomationItem item)
        {
            using var db = new SmartHomeDbContext();
            var tracked = db.Automations.First(x => x.Id == item.Id);
            tracked.Enabled = !tracked.Enabled;
            db.SaveChanges();
            LoadData();
        }
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is AutomationItem item)
        {
            if (MessageBox.Show($"Delete automation '{item.Name}'?", "Confirm", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                using var db = new SmartHomeDbContext();
                var tracked = db.Automations.First(x => x.Id == item.Id);
                db.Automations.Remove(tracked);
                db.SaveChanges();
                LoadData();
            }
        }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => ApplyFilter();

    private void ApplyFilter()
    {
        var text = (SearchBox?.Text ?? string.Empty).Trim();
        var q = _all.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(text))
        {
            q = q.Where(a => (a.Name ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0
                           || (a.DeviceName ?? string.Empty).IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        List.ItemsSource = q.ToList();
        Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(ScrollListToTop));
    }

    private void Edit_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not AutomationItem item) return;
        _editingAutomationId = item.Id;
        NameBox.Text = item.Name;
        DevicesBox.SelectedValue = item.DeviceId;
        DevicesBox.Text = item.DeviceName;
        TimeBox.Text = item.TimeHHmm;

        var match = ActionBox.Items.OfType<ComboBoxItem>()
            .FirstOrDefault(i =>
                string.Equals(i.Tag?.ToString(), item.Action, StringComparison.OrdinalIgnoreCase)
                || string.Equals(i.Content?.ToString(), item.Action, StringComparison.OrdinalIgnoreCase));
        if (match != null)
            ActionBox.SelectedItem = match;

        if (item.Action.Equals("SetOnOff", StringComparison.OrdinalIgnoreCase))
        {
            OnOffBox.SelectedIndex = item.Value >= 0.5 ? 0 : 1;
            ValueBox.Text = string.Empty;
        }
        else
        {
            ValueBox.Text = item.Value.ToString(CultureInfo.InvariantCulture);
        }
        AddOrUpdateButton.Content = "Update";
        CancelEditButton.Visibility = Visibility.Visible;
    }

    private void CancelEdit_Click(object sender, RoutedEventArgs e) => ResetEditingState();

    private void ResetEditingState()
    {
        _editingAutomationId = null;
        NameBox.Text = string.Empty;
        DevicesBox.SelectedIndex = -1;
        DevicesBox.SelectedValue = null;
        DevicesBox.Text = string.Empty;
        TimeBox.Text = string.Empty;
        if (ActionBox.Items.OfType<ComboBoxItem>().FirstOrDefault() is ComboBoxItem firstAction)
            ActionBox.SelectedItem = firstAction;
        ValueBox.Text = string.Empty;
        OnOffBox.SelectedIndex = 0;
        UpdateActionValueUi();
        AddOrUpdateButton.Content = "Add";
        CancelEditButton.Visibility = Visibility.Collapsed;
    }

    private void ScrollListToTop()
    {
        if (List is null) return;
        var viewer = FindDescendant<ScrollViewer>(List);
        viewer?.ScrollToTop();
    }

    private static T? FindDescendant<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent is T match) return match;
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            var result = FindDescendant<T>(child);
            if (result is not null) return result;
        }
        return null;
    }
}
