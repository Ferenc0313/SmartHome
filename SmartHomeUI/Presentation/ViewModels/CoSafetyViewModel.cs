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
    private readonly CoSafetyMcuController _controller;

    private double _coLevel;
    private CoState _sensorState;
    private bool _detectorActive;
    private bool _doorOpen;
    private bool _doorLocked;
    private DoorMode _doorMode;

    public CoSafetyViewModel()
    {
        _controller = new CoSafetyMcuController();

        _coLevel = _controller.Environment.CoLevel;
        _sensorState = _controller.Sensor.State;
        _detectorActive = _controller.Detector.IsActive;
        _doorOpen = _controller.Door.IsOpen;
        _doorLocked = _controller.Door.IsLocked;
        _doorMode = _controller.Door.Mode;
        CoSafetyUiState.Instance.SensorState = _sensorState;
        CoSafetyUiState.Instance.DetectorActive = _detectorActive;
        CoSafetyUiState.Instance.DoorEmergency = _doorMode == DoorMode.EmergencyOverride;

        _controller.CoLevelChanged += (_, level) => UpdateCoLevel(level);
        _controller.CoStateChanged += (_, state) => UpdateSensorState(state);
        _controller.DetectorStateChanged += (_, isActive) => UpdateDetector(isActive);
        _controller.DoorStateChanged += (_, _) => RefreshDoorState();
        _controller.ImportantEvent += (_, message) => DeviceService.AddActivityMessage(message);
    }

    public ObservableCollection<LogEntry> LogEntries => _controller.LogEntries;

    public double CoLevel
    {
        get => _coLevel;
        set
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(clamped - _coLevel) < 0.0001) return;
            _coLevel = clamped;
            OnPropertyChanged();
            _controller.SetCoLevel(clamped);
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

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
