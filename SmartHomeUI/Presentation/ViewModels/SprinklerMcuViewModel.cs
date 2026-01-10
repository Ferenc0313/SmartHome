using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SmartHomeUI.Domain.Mcu;
using SmartHomeUI.Services;

namespace SmartHomeUI.Presentation.ViewModels;

public sealed class SprinklerMcuViewModel : INotifyPropertyChanged
{
    private IrrigationMcuController? _controller;
    private readonly ObservableCollection<LogEntry> _logs = new();

    private double _soil;
    private double _temp;
    private double _rain;
    private bool _valveOpen;
    private bool _rainLocked;
    private bool _isWatering;
    private double _moistureThreshold = 35;
    private double _rainLockThreshold = 60;
    private double _maxWaterMinutes = 8;
    private double _cooldownMinutes = 15;
    private bool _isReady;
    private string _status = "Waiting for devices.";

    public SprinklerMcuViewModel()
    {
        AttachController(IrrigationMcuRuntime.Controller);
        IrrigationMcuRuntime.SnapshotUpdated += (_, snap) => ApplySnapshot(snap);
        IrrigationMcuRuntime.ReadinessChanged += (_, ready) => IsReady = ready;
        IrrigationMcuRuntime.StatusChanged += (_, status) => Status = status;
        IsReady = IrrigationMcuRuntime.IsReady;
        Status = IrrigationMcuRuntime.LastStatus;
        if (IrrigationMcuRuntime.LastSnapshot is IrrigationSnapshot snap)
        {
            ApplySnapshot(snap);
        }
    }

    public ObservableCollection<LogEntry> LogEntries => _controller?.LogEntries ?? _logs;

    public double SoilMoisture
    {
        get => _soil;
        private set { if (Math.Abs(_soil - value) < 0.001) return; _soil = value; OnPropertyChanged(); }
    }

    public double TemperatureC
    {
        get => _temp;
        private set { if (Math.Abs(_temp - value) < 0.001) return; _temp = value; OnPropertyChanged(); }
    }

    public double RainLevel
    {
        get => _rain;
        private set { if (Math.Abs(_rain - value) < 0.001) return; _rain = value; OnPropertyChanged(); }
    }

    public bool ValveOpen
    {
        get => _valveOpen;
        private set { if (_valveOpen == value) return; _valveOpen = value; OnPropertyChanged(); }
    }

    public bool RainLocked
    {
        get => _rainLocked;
        private set { if (_rainLocked == value) return; _rainLocked = value; OnPropertyChanged(); }
    }

    public bool IsWatering
    {
        get => _isWatering;
        private set { if (_isWatering == value) return; _isWatering = value; OnPropertyChanged(); }
    }

    public double MoistureThreshold
    {
        get => _moistureThreshold;
        set { _moistureThreshold = value; OnPropertyChanged(); }
    }

    public double RainLockThreshold
    {
        get => _rainLockThreshold;
        set { _rainLockThreshold = value; OnPropertyChanged(); }
    }

    public double MaxWaterMinutes
    {
        get => _maxWaterMinutes;
        set { _maxWaterMinutes = value; OnPropertyChanged(); }
    }

    public double CooldownMinutes
    {
        get => _cooldownMinutes;
        set { _cooldownMinutes = value; OnPropertyChanged(); }
    }

    public bool IsReady
    {
        get => _isReady;
        private set { if (_isReady == value) return; _isReady = value; OnPropertyChanged(); }
    }

    public string Status
    {
        get => _status;
        private set { if (_status == value) return; _status = value; OnPropertyChanged(); }
    }

    public void ApplySettings()
    {
        _controller?.UpdateSettings(MoistureThreshold, RainLockThreshold, MaxWaterMinutes, CooldownMinutes);
    }

    private void AttachController(IrrigationMcuController? controller)
    {
        _controller = controller;
        if (_controller != null)
        {
            MoistureThreshold = _controller.MoistureThreshold;
            RainLockThreshold = _controller.RainLockThreshold;
            MaxWaterMinutes = _controller.MaxWaterMinutes;
            CooldownMinutes = _controller.CooldownMinutes;
        }
    }

    private void ApplySnapshot(IrrigationSnapshot snap)
    {
        SoilMoisture = snap.SoilMoisture;
        TemperatureC = snap.TemperatureC;
        RainLevel = snap.RainLevel;
        ValveOpen = snap.ValveOpen;
        RainLocked = snap.RainLocked;
        IsWatering = snap.IsWatering;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
