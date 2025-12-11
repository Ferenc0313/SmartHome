using System;
using System.Collections.Generic; // retained for List<WidgetKind>, might be global but kept minimal
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq; // needed for LINQ operations inside ApplyEcoMode and elsewhere
using System.Runtime.CompilerServices;
using System.Windows.Input;
using SmartHomeUI.Models;
using SmartHomeUI.Services;

namespace SmartHomeUI.ViewModels;

public class DashboardViewModel : INotifyPropertyChanged
{
    private bool _isLoaded;
    private bool _isWidgetPickerOpen;
    private bool _isRestoring;
    private int? _pendingPlaceholderIndex;
    private static readonly List<WidgetKind> _persistedWidgetOrder = new();

    public DashboardViewModel()
    {
        InitializeWidgetOptions();
        Widgets.CollectionChanged += WidgetsOnCollectionChanged;
        RestorePersistedWidgets();
        ApplyEcoModeCommand = new DelegateCommand(ApplyEcoMode);
    }

    public ICommand ApplyEcoModeCommand { get; }

    public ReadOnlyObservableCollection<Device> Devices => DeviceService.Devices;

    public ObservableCollection<DashboardWidget> Widgets { get; } = new();
    public ObservableCollection<object> WidgetTiles { get; } = new();
    public ObservableCollection<WidgetOptionViewModel> WidgetOptions { get; } = new();

    public bool HasWidgets => Widgets.Count > 0;
    public bool CanAddWidgets => WidgetOptions.Any(o => o.IsEnabled);

    private int _deviceCount;
    public int DeviceCount { get => _deviceCount; private set { _deviceCount = value; OnPropertyChanged(); } }

    private int _onlineCount;
    public int OnlineCount { get => _onlineCount; private set { _onlineCount = value; OnPropertyChanged(); } }

    private int _offlineCount;
    public int OfflineCount { get => _offlineCount; private set { _offlineCount = value; OnPropertyChanged(); } }

    private int _lowBatteryCount;
    public int LowBatteryCount { get => _lowBatteryCount; private set { _lowBatteryCount = value; OnPropertyChanged(); } }

    private int _onCount;
    public int OnCount { get => _onCount; private set { _onCount = value; OnPropertyChanged(); } }

    private bool _coSafetyAvailable;
    public bool CoSafetyAvailable
    {
        get => _coSafetyAvailable;
        private set
        {
            if (_coSafetyAvailable == value) return;
            _coSafetyAvailable = value;
            OnPropertyChanged();
        }
    }


    public ReadOnlyObservableCollection<DeviceService.ActivityEntry> Activity => DeviceService.Activity;

    public bool IsWidgetPickerOpen
    {
        get => _isWidgetPickerOpen;
        set
        {
            if (_isWidgetPickerOpen == value) return;
            _isWidgetPickerOpen = value;
            OnPropertyChanged();
        }
    }

    public void Load()
    {
        if (_isLoaded) return;
        _isLoaded = true;

        UpdateAggregates();
        UpdateCoSafetyAvailability();
        RefreshWidgetOptions();
        UpdateOptionSelections();

        DeviceService.DevicesChanged += OnDevicesChanged;
        DeviceService.StateChanged += OnStateChanged;
        EnsureTrailingPlaceholder();
    }

        public void OpenWidgetPicker(AddWidgetPlaceholder placeholder)
    {
        if (!CanAddWidgets) return;
        var index = WidgetTiles.IndexOf(placeholder);
        if (index < 0) return;
        _pendingPlaceholderIndex = index;
        IsWidgetPickerOpen = true;
    }

    public void CloseWidgetPicker()
    {
        _pendingPlaceholderIndex = null;
        IsWidgetPickerOpen = false;
    }

    public void AddWidget(WidgetKind kind)
    {
        var option = WidgetOptions.FirstOrDefault(o => o.Kind == kind);
        if (option is null || !option.IsEnabled) return;
        if (Widgets.Any(w => w.Kind == kind))
        {
            CloseWidgetPicker();
            return;
        }

        var widget = CreateWidget(kind);
        int insertPosition = Widgets.Count;

        if (_pendingPlaceholderIndex is int idx &&
            idx >= 0 &&
            idx < WidgetTiles.Count &&
            WidgetTiles[idx] is AddWidgetPlaceholder)
        {
            insertPosition = CountWidgetsBefore(idx);
            WidgetTiles[idx] = widget;
        }
        else
        {
            var fallbackIdx = WidgetTiles
                .Select((item, index) => (item, index))
                .FirstOrDefault(t => t.item is AddWidgetPlaceholder);
            if (fallbackIdx.item is AddWidgetPlaceholder)
            {
                insertPosition = CountWidgetsBefore(fallbackIdx.index);
                WidgetTiles[fallbackIdx.index] = widget;
            }
            else
            {
                WidgetTiles.Add(widget);
            }
        }

        if (insertPosition >= 0 && insertPosition <= Widgets.Count)
            Widgets.Insert(insertPosition, widget);
        else
            Widgets.Add(widget);

        EnsureTrailingPlaceholder();
        CloseWidgetPicker();
    }

    private static DashboardWidget CreateWidget(WidgetKind kind) => kind switch
    {
        WidgetKind.Security => new SecurityWidget(),
        WidgetKind.Energy => new EnergyWidget(),
        WidgetKind.Climate => new ClimateWidget(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };

    private void OnDevicesChanged()
    {
        UpdateAggregates();
        UpdateCoSafetyAvailability();
        RefreshWidgetOptions();
    }

    private void OnStateChanged()
    {
        UpdateAggregates();
        UpdateCoSafetyAvailability();
    }

    private void UpdateAggregates()
    {
        var (total, online, on, low) = DeviceService.GetAggregates();
        DeviceCount = total;
        OnlineCount = online;
        OfflineCount = total - online;
        LowBatteryCount = low;
        OnCount = on;
    }

    private void UpdateCoSafetyAvailability()
    {
        var snapshot = Devices.ToList();
        bool hasSensor = snapshot.Any(d => d.Type.Equals("CoSensor", StringComparison.OrdinalIgnoreCase));
        bool hasDetector = snapshot.Any(d => d.Type.Equals("CoDetector", StringComparison.OrdinalIgnoreCase));
        bool hasDoor = snapshot.Any(d => d.Type.Equals("SmartDoor", StringComparison.OrdinalIgnoreCase));
        CoSafetyAvailable = hasSensor && hasDetector && hasDoor;
    }

    private void InitializeWidgetOptions()
    {
        WidgetOptions.Clear();
        WidgetOptions.Add(new WidgetOptionViewModel(
            WidgetKind.Security,
            "Security",
            "Arm the system, check locks and critical sensors.",
            "Requires a lock, camera, or motion/smoke sensor"));
        WidgetOptions.Add(new WidgetOptionViewModel(
            WidgetKind.Energy,
            "Energy",
            "Track consumption and trigger eco actions.",
            "Requires a smart plug or light"));
        WidgetOptions.Add(new WidgetOptionViewModel(
            WidgetKind.Climate,
            "Climate",
            "Adjust comfort settings and monitor the environment.",
            "Requires a thermostat or weather station"));
    }

    private void RefreshWidgetOptions()
    {
        var caps = DeviceService.GetWidgetCapabilities();
        foreach (var option in WidgetOptions)
        {
            var available = option.Kind switch
            {
                WidgetKind.Security => caps.HasSecurityDevices,
                WidgetKind.Energy => caps.HasEnergyDevices,
                WidgetKind.Climate => caps.HasClimateDevices,
                _ => false
            };
            option.SetAvailability(available);
        }
        OnPropertyChanged(nameof(CanAddWidgets));
        EnsureTrailingPlaceholder();
    }

    private void UpdateOptionSelections()
    {
        foreach (var option in WidgetOptions)
        {
            var selected = Widgets.Any(w => w.Kind == option.Kind);
            option.SetSelected(selected);
        }
        OnPropertyChanged(nameof(HasWidgets));
        OnPropertyChanged(nameof(CanAddWidgets));
        EnsureTrailingPlaceholder();
    }

    private void WidgetsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_isRestoring) return;
        UpdateOptionSelections();
        PersistCurrentWidgets();
    }

    private void EnsureTrailingPlaceholder()
    {
        if (!CanAddWidgets)
        {
            for (int i = WidgetTiles.Count - 1; i >= 0; i--)
            {
                if (WidgetTiles[i] is AddWidgetPlaceholder)
                    WidgetTiles.RemoveAt(i);
            }
            return;
        }

        bool placeholderSeen = false;
        for (int i = WidgetTiles.Count - 1; i >= 0; i--)
        {
            if (WidgetTiles[i] is AddWidgetPlaceholder)
            {
                if (!placeholderSeen)
                {
                    placeholderSeen = true;
                    if (i != WidgetTiles.Count - 1)
                    {
                        WidgetTiles.RemoveAt(i);
                        WidgetTiles.Add(new AddWidgetPlaceholder());
                    }
                }
                else
                {
                    WidgetTiles.RemoveAt(i);
                }
            }
        }

        if (!placeholderSeen)
        {
            WidgetTiles.Add(new AddWidgetPlaceholder());
        }
    }

    private int CountWidgetsBefore(int index) =>
        WidgetTiles.Take(index).Count(item => item is DashboardWidget);

    private void RestorePersistedWidgets()
    {
        _isRestoring = true;
        Widgets.Clear();
        WidgetTiles.Clear();

        foreach (var kind in _persistedWidgetOrder)
        {
            var widget = CreateWidget(kind);
            Widgets.Add(widget);
            WidgetTiles.Add(widget);
        }

        _isRestoring = false;
        UpdateOptionSelections();
        EnsureTrailingPlaceholder();
        PersistCurrentWidgets();
    }

    private void PersistCurrentWidgets()
    {
        _persistedWidgetOrder.Clear();
        foreach (var widget in Widgets)
        {
            _persistedWidgetOrder.Add(widget.Kind);
        }
    }

    public void ApplyEcoMode()
    {
        // Take a snapshot to avoid 'Collection was modified' while DeviceService replaces items
        var ids = Devices.Select(d => d.Id).ToList();
        foreach (var id in ids)
        {
            DeviceService.SetValue(id, 0);
        }
        // Recompute aggregates after updates
        UpdateAggregates();
    }

    

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public enum WidgetKind
{
    Security,
    Energy,
    Climate
}

public abstract class DashboardWidget
{
    protected DashboardWidget(string title) => Title = title;

    public abstract WidgetKind Kind { get; }
    public string Title { get; }
}

public sealed class SecurityWidget : DashboardWidget
{
    public SecurityWidget() : base("Security") { }
    public override WidgetKind Kind => WidgetKind.Security;
}

public sealed class EnergyWidget : DashboardWidget
{
    public EnergyWidget() : base("Energy") { }
    public override WidgetKind Kind => WidgetKind.Energy;
}

public sealed class ClimateWidget : DashboardWidget
{
    public ClimateWidget() : base("Climate") { }
    public override WidgetKind Kind => WidgetKind.Climate;
}

public sealed class AddWidgetPlaceholder
{
}

public sealed class WidgetOptionViewModel : INotifyPropertyChanged
{
    private bool _isAvailable;
    private bool _isSelected;

    public WidgetOptionViewModel(WidgetKind kind, string title, string description, string requirement)
    {
        Kind = kind;
        Title = title;
        Description = description;
        Requirement = requirement;
    }

    public WidgetKind Kind { get; }
    public string Title { get; }
    public string Description { get; }
    public string Requirement { get; }

    public bool IsAvailable
    {
        get => _isAvailable;
        private set
        {
            if (_isAvailable == value) return;
            _isAvailable = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    public bool IsSelected
    {
        get => _isSelected;
        private set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEnabled));
        }
    }

    public bool IsEnabled => IsAvailable && !IsSelected;

    public void SetAvailability(bool value) => IsAvailable = value;
    public void SetSelected(bool value) => IsSelected = value;

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public class DelegateCommand : ICommand
{
    private readonly Action _execute;

    public DelegateCommand(Action execute)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute();
}
