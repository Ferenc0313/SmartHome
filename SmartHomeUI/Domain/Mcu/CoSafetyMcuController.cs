using System;
using System.Collections.ObjectModel;
using SmartHomeUI.Domain.Devices;

namespace SmartHomeUI.Domain.Mcu;

public class CoSafetyMcuController
{
    public CoSafetyMcuController()
    {
        Environment = new VirtualCoEnvironment();
        Sensor = new CoSensor(Environment);
        Detector = new CoDetector(Sensor);
        Door = new SmartDoor();

        Environment.CoLevelChanged += OnCoLevelChanged;
        Sensor.CoStateChanged += OnCoStateChanged;
        Detector.CoAlarmRaised += OnCoAlarmRaised;
        Detector.CoAlarmCleared += OnCoAlarmCleared;

        AddLog("CO safety MCU initialized.");
    }

    public VirtualCoEnvironment Environment { get; }
    public CoSensor Sensor { get; }
    public CoDetector Detector { get; }
    public SmartDoor Door { get; }
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    public event EventHandler<double>? CoLevelChanged;
    public event EventHandler<CoState>? CoStateChanged;
    public event EventHandler<bool>? DetectorStateChanged;
    public event EventHandler? DoorStateChanged;
    public event EventHandler<string>? ImportantEvent;

    public void SetCoLevel(double level) => Environment.SetCoLevel(level);

    private void OnCoLevelChanged(object? sender, double level)
    {
        CoLevelChanged?.Invoke(this, level);
    }

    private void OnCoStateChanged(object? sender, CoStateChangedEventArgs e)
    {
        AddLog($"CO sensor state changed: {e.PreviousState} -> {e.NewState} at {e.Level:F0}.");
        CoStateChanged?.Invoke(this, e.NewState);
    }

    private void OnCoAlarmRaised(object? sender, EventArgs e)
    {
        AddLog("CO alarm raised (Critical).");
        ActivateEmergencyDoor();
        DetectorStateChanged?.Invoke(this, true);
    }

    private void OnCoAlarmCleared(object? sender, EventArgs e)
    {
        AddLog("CO alarm cleared (back to Normal).");
        RestoreDoorAfterClear();
        DetectorStateChanged?.Invoke(this, false);
    }

    private void ActivateEmergencyDoor()
    {
        Door.Unlock();
        Door.OpenDoor();
        Door.SetMode(DoorMode.EmergencyOverride);
        AddLog("SmartDoor opened and unlocked (EmergencyOverride).");
        DoorStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RestoreDoorAfterClear()
    {
        Door.CloseDoor();
        Door.Lock();
        Door.SetMode(DoorMode.Normal);
        AddLog("SmartDoor closed, locked, and returned to Normal mode.");
        DoorStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void AddLog(string message)
    {
        LogEntries.Insert(0, new LogEntry(DateTime.Now, message));
        ImportantEvent?.Invoke(this, message);
    }
}
