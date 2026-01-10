using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using SmartHomeUI.Domain.Devices;
using SmartHomeUI.Domain.Mcu;
using SmartHomeUI.Services;

namespace SmartHomeUI.Presentation.ViewModels;

public class CoSafetyViewModel : INotifyPropertyChanged
{
    private readonly ObservableCollection<LogEntry> _fallbackLogs = new();
    private CoSafetyMcuController? _controller;

    private double _coLevel;
    private CoState _sensorState;
    private bool _detectorActive;
    private bool _doorOpen;
    private bool _doorLocked;
    private DoorMode _doorMode;
    private bool _isReady;
    private string _status = "Waiting for devices.";

    public CoSafetyViewModel()
    {
        AttachController(CoSafetyMcuRuntime.Controller);

        CoSafetyMcuRuntime.SnapshotUpdated += (_, snap) => ApplySnapshot(snap);
        CoSafetyMcuRuntime.ReadinessChanged += (_, ready) => IsReady = ready;
        CoSafetyMcuRuntime.StatusChanged += (_, status) => Status = status;

        IsReady = CoSafetyMcuRuntime.IsReady;
        Status = CoSafetyMcuRuntime.LastStatus;

        if (CoSafetyMcuRuntime.LastSnapshot is CoSafetySnapshot snap)
        {
            ApplySnapshot(snap);
        }
    }

    public ObservableCollection<LogEntry> LogEntries => _controller?.LogEntries ?? _fallbackLogs;

    public double CoLevel
    {
        get => _coLevel;
        set
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(clamped - _coLevel) < 0.0001) return;
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
            OnPropertyChanged(nameof(SensorStateText));
            CoSafetyUiState.Instance.SensorState = _sensorState;
        }
    }

    public bool IsDetectorActive
    {
        get => _detectorActive;
        private set
        {
            if (_detectorActive == value) return;
            _detectorActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DetectorStatus));
        }
    }

    public bool IsDoorOpen
    {
        get => _doorOpen;
        private set
        {
            if (_doorOpen == value) return;
            _doorOpen = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DoorStateText));
        }
    }

    public bool IsDoorLocked
    {
        get => _doorLocked;
        private set
        {
            if (_doorLocked == value) return;
            _doorLocked = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DoorLockStateText));
        }
    }

    public DoorMode DoorMode
    {
        get => _doorMode;
        private set
        {
            if (_doorMode == value) return;
            _doorMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DoorModeText));
        }
    }

    public string SensorStateText => SensorState.ToString();
    public string DetectorStatus => IsDetectorActive ? "Active" : "Inactive";
    public string DoorStateText => IsDoorOpen ? "Open" : "Closed";
    public string DoorLockStateText => IsDoorLocked ? "Locked" : "Unlocked";
    public string DoorModeText => DoorMode.ToString();
    public bool IsReady
    {
        get => _isReady;
        private set
        {
            if (_isReady == value) return;
            _isReady = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value) return;
            _status = value;
            OnPropertyChanged();
        }
    }

    private void AttachController(CoSafetyMcuController? controller)
    {
        _controller = controller;
    }

    private void UpdateCoLevel(double level)
    {
        var clamped = Math.Clamp(level, 0, 100);
        if (Math.Abs(clamped - _coLevel) < 0.0001) return;
        _coLevel = clamped;
        OnPropertyChanged(nameof(CoLevel));
    }

    private void UpdateSensorState(CoState state) => SensorState = state;

    private void UpdateDetector(bool isActive)
    {
        IsDetectorActive = isActive;
        CoSafetyUiState.Instance.DetectorActive = isActive;
    }

    private void RefreshDoorState()
    {
        IsDoorOpen = _controller.Door.IsOpen;
        IsDoorLocked = _controller.Door.IsLocked;
        DoorMode = _controller.Door.Mode;
        CoSafetyUiState.Instance.DoorEmergency = DoorMode == DoorMode.EmergencyOverride;
        DeviceService.SetSmartDoorStateForCoSafety(IsDoorOpen);
    }

    private void ApplySnapshot(CoSafetySnapshot snap)
    {
        _coLevel = snap.CoLevel;
        OnPropertyChanged(nameof(CoLevel));
        SensorState = snap.SensorState;
        IsDetectorActive = snap.DetectorActive;
        IsDoorOpen = snap.DoorOpen;
        IsDoorLocked = snap.DoorLocked;
        DoorMode = snap.DoorMode;
        CoSafetyUiState.Instance.SensorState = _sensorState;
        CoSafetyUiState.Instance.DetectorActive = _detectorActive;
        CoSafetyUiState.Instance.DoorEmergency = _doorMode == DoorMode.EmergencyOverride;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
