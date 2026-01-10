using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SmartHomeUI.Domain.Devices;
using SmartHomeUI.Domain.Mcu;
using SmartHomeUI.Services;

namespace SmartHomeUI.Presentation.ViewModels;

public sealed class CoSafetyMcuViewModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<Domain.Mcu.LogEntry> _fallbackLogs = new();
    private CoSafetyMcuController? _controller;

    private double _coLevel;
    private CoState _sensorState;
    private bool _detectorActive;
    private bool _doorOpen;
    private bool _doorLocked;
    private DoorMode _doorMode;
    private double _warn = 30;
    private double _crit = 60;
    private double _vent = 8;
    private bool _isReady;
    private string _status = "Waiting for devices.";

    public CoSafetyMcuViewModel()
    {
        Attach(CoSafetyMcuRuntime.Controller);

        CoSafetyMcuRuntime.SnapshotUpdated += (_, snap) => ApplySnapshot(snap);
        CoSafetyMcuRuntime.ReadinessChanged += (_, ready) => IsReady = ready;
        CoSafetyMcuRuntime.StatusChanged += (_, status) => Status = status;

        IsReady = CoSafetyMcuRuntime.IsReady;
        Status = CoSafetyMcuRuntime.LastStatus;
        if (CoSafetyMcuRuntime.Controller != null)
        {
            _warn = CoSafetyMcuRuntime.Controller.WarningThreshold;
            _crit = CoSafetyMcuRuntime.Controller.CriticalThreshold;
            _vent = CoSafetyMcuRuntime.Controller.VentilationStrength;
        }
        if (CoSafetyMcuRuntime.LastSnapshot is CoSafetySnapshot snap)
        {
            ApplySnapshot(snap);
        }
    }

    public ObservableCollection<Domain.Mcu.LogEntry> LogEntries => _controller?.LogEntries ?? _fallbackLogs;

    public double CoLevel
    {
        get => _coLevel;
        set
        {
            var clamped = System.Math.Clamp(value, 0, 100);
            if (System.Math.Abs(clamped - _coLevel) < 0.0001) return;
            _coLevel = clamped;
            OnPropertyChanged();
            _controller?.SetCoLevel(clamped);
        }
    }

    public CoState SensorState
    {
        get => _sensorState;
        private set
        {
            if (_sensorState == value) return;
            _sensorState = value;
            OnPropertyChanged();
        }
    }

    public bool DetectorActive
    {
        get => _detectorActive;
        private set { if (_detectorActive == value) return; _detectorActive = value; OnPropertyChanged(); }
    }

    public bool DoorOpen
    {
        get => _doorOpen;
        private set { if (_doorOpen == value) return; _doorOpen = value; OnPropertyChanged(); }
    }

    public bool DoorLocked
    {
        get => _doorLocked;
        private set { if (_doorLocked == value) return; _doorLocked = value; OnPropertyChanged(); }
    }

    public DoorMode DoorMode
    {
        get => _doorMode;
        private set { if (_doorMode == value) return; _doorMode = value; OnPropertyChanged(); }
    }

    public double WarningThreshold
    {
        get => _warn;
        set { if (System.Math.Abs(_warn - value) < 0.001) return; _warn = value; OnPropertyChanged(); }
    }

    public double CriticalThreshold
    {
        get => _crit;
        set { if (System.Math.Abs(_crit - value) < 0.001) return; _crit = value; OnPropertyChanged(); }
    }

    public double VentilationStrength
    {
        get => _vent;
        set { if (System.Math.Abs(_vent - value) < 0.001) return; _vent = value; OnPropertyChanged(); }
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
        _controller?.UpdateSettings(WarningThreshold, CriticalThreshold, VentilationStrength);
    }

    private void ApplySnapshot(CoSafetySnapshot snap)
    {
        _coLevel = snap.CoLevel;
        OnPropertyChanged(nameof(CoLevel));
        SensorState = snap.SensorState;
        DetectorActive = snap.DetectorActive;
        DoorOpen = snap.DoorOpen;
        DoorLocked = snap.DoorLocked;
        DoorMode = snap.DoorMode;
    }

    private void Attach(CoSafetyMcuController? controller)
    {
        _controller = controller;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
