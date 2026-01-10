using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using SmartHomeUI.Domain.Devices;

namespace SmartHomeUI.Domain.Mcu;

public sealed record CoSafetySnapshot(double CoLevel, CoState SensorState, bool DetectorActive, bool DoorOpen, bool DoorLocked, DoorMode DoorMode);

public class CoSafetyMcuController
{
    private readonly DispatcherTimer _timer;
    private readonly EventHandler _tickHandler;

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

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _tickHandler = (_, _) => Tick();
        _timer.Tick += _tickHandler;
    }

    public VirtualCoEnvironment Environment { get; }
    public CoSensor Sensor { get; }
    public CoDetector Detector { get; }
    public SmartDoor Door { get; }
    public double WarningThreshold { get; private set; } = 30;
    public double CriticalThreshold { get; private set; } = 60;
    public double VentilationStrength { get; private set; } = 8;
    public bool IsEnabled { get; private set; }
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    public event EventHandler<double>? CoLevelChanged;
    public event EventHandler<CoState>? CoStateChanged;
    public event EventHandler<bool>? DetectorStateChanged;
    public event EventHandler? DoorStateChanged;
    public event EventHandler<string>? ImportantEvent;
    public event EventHandler<CoSafetySnapshot>? SnapshotChanged;

    public void SetCoLevel(double level) => Environment.SetCoLevel(level);

    public void SetEnabled(bool enabled)
    {
        if (IsEnabled == enabled) return;
        IsEnabled = enabled;
        if (enabled)
        {
            AddLog("CO safety MCU enabled.");
            _timer.Start();
            EmitSnapshot();
        }
        else
        {
            _timer.Stop();
            AddLog("CO safety MCU disabled.");
        }
    }

    public void UpdateSettings(double warningThreshold, double criticalThreshold, double ventilationStrength)
    {
        WarningThreshold = Math.Clamp(warningThreshold, 5, 95);
        CriticalThreshold = Math.Clamp(criticalThreshold, WarningThreshold + 1, 100);
        VentilationStrength = Math.Clamp(ventilationStrength, 1, 40);
        Sensor.UpdateThresholds(WarningThreshold, CriticalThreshold);
        AddLog($"Settings updated: Warn>={WarningThreshold:F0} | Critical>={CriticalThreshold:F0} | Ventilation drop {VentilationStrength:F0}");
    }

    private void Tick()
    {
        if (!IsEnabled) return;

        Environment.Drift();

        if (Door.IsOpen)
        {
            Environment.Ventilate(VentilationStrength);
        }

        EmitSnapshot();
    }

    private void OnCoLevelChanged(object? sender, double level)
    {
        CoLevelChanged?.Invoke(this, level);
        EmitSnapshot();
    }

    private void OnCoStateChanged(object? sender, CoStateChangedEventArgs e)
    {
        AddLog($"CO sensor state changed: {e.PreviousState} -> {e.NewState} at {e.Level:F0}.");
        ApplyDoorPolicy(e.NewState);
        CoStateChanged?.Invoke(this, e.NewState);
        EmitSnapshot();
    }

    private void OnCoAlarmRaised(object? sender, EventArgs e)
    {
        AddLog("CO alarm raised (Critical).");
        ApplyDoorPolicy(CoState.Critical);
        DetectorStateChanged?.Invoke(this, true);
        // Immediate ventilation effect when alarm triggers
        Environment.Ventilate(VentilationStrength * 2);
        EmitSnapshot();
    }

    private void OnCoAlarmCleared(object? sender, EventArgs e)
    {
        AddLog("CO alarm cleared (back to Normal).");
        ApplyDoorPolicy(CoState.Normal);
        DetectorStateChanged?.Invoke(this, false);
        EmitSnapshot();
    }

    private void ApplyDoorPolicy(CoState state)
    {
        switch (state)
        {
            case CoState.Warning:
                if (EnsureDoorOpen(DoorMode.Normal, unlock: true))
                {
                    Environment.Ventilate(VentilationStrength);
                }
                break;
            case CoState.Critical:
                EnsureDoorOpen(DoorMode.EmergencyOverride, unlock: true);
                break;
            case CoState.Normal:
                EnsureDoorClosedAndLocked();
                break;
        }
    }

    private bool EnsureDoorOpen(DoorMode targetMode, bool unlock)
    {
        bool changed = false;

        if (unlock && Door.IsLocked)
        {
            Door.Unlock();
            changed = true;
        }

        if (!Door.IsOpen)
        {
            Door.OpenDoor();
            changed = true;
        }

        if (Door.Mode != targetMode)
        {
            Door.SetMode(targetMode);
            changed = true;
        }

        if (changed)
        {
            AddLog(targetMode == DoorMode.EmergencyOverride
                ? "SmartDoor opened and unlocked (EmergencyOverride)."
                : "SmartDoor opened for ventilation (Warning).");
            DoorStateChanged?.Invoke(this, EventArgs.Empty);
        }

        return changed;
    }

    private void EnsureDoorClosedAndLocked()
    {
        if (!Door.IsOpen && Door.IsLocked && Door.Mode == DoorMode.Normal) return;

        Door.CloseDoor();
        Door.Lock();
        Door.SetMode(DoorMode.Normal);
        AddLog("SmartDoor closed, locked, and returned to Normal mode.");
        DoorStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EmitSnapshot()
    {
        SnapshotChanged?.Invoke(this, new CoSafetySnapshot(Environment.CoLevel, Sensor.State, Detector.IsActive, Door.IsOpen, Door.IsLocked, Door.Mode));
    }

    private void AddLog(string message)
    {
        LogEntries.Insert(0, new LogEntry(DateTime.Now, message));
        ImportantEvent?.Invoke(this, message);
    }

    public void Stop()
    {
        SetEnabled(false);
    }
}
