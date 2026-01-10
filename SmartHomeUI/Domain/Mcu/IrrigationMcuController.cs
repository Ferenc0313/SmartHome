using System;
using System.Collections.ObjectModel;
using System.Windows.Threading;
using SmartHomeUI.Domain.Devices;

namespace SmartHomeUI.Domain.Mcu;

public sealed record IrrigationSnapshot(double SoilMoisture, double TemperatureC, double RainLevel, bool ValveOpen, bool IsWatering, bool RainLocked);

public sealed class IrrigationMcuController : IDisposable
{
    private readonly VirtualIrrigationEnvironment _environment;
    private readonly SoilMoistureSensor _soilSensor;
    private readonly RainSensor _rainSensor;
    private readonly TemperatureSensor _tempSensor;
    private readonly SprinklerValve _valve;
    private readonly DispatcherTimer _timer;
    private readonly EventHandler _tickHandler;
    private DateTime? _wateringStartedUtc;
    private DateTime _lastCoolDownEndUtc = DateTime.MinValue;
    private bool _disposed;

    public IrrigationMcuController()
    {
        _environment = new VirtualIrrigationEnvironment();
        _soilSensor = new SoilMoistureSensor();
        _rainSensor = new RainSensor();
        _tempSensor = new TemperatureSensor();
        _valve = new SprinklerValve();
        LogEntries = new ObservableCollection<LogEntry>();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _tickHandler = (_, _) => Tick();
        _timer.Tick += _tickHandler;

        _soilSensor.MoistureChanged += (_, v) => EmitSnapshot();
        _rainSensor.RainChanged += (_, _) => EmitSnapshot();
        _tempSensor.TemperatureChanged += (_, _) => EmitSnapshot();
    }

    public ObservableCollection<LogEntry> LogEntries { get; }

    public double MoistureThreshold { get; set; } = 35;
    public double RainLockThreshold { get; set; } = 60;
    public double MaxWaterMinutes { get; set; } = 8;
    public double CooldownMinutes { get; set; } = 15;

    public bool IsEnabled { get; private set; }

    public event EventHandler<IrrigationSnapshot>? SnapshotChanged;
    public event EventHandler<string>? ImportantEvent;

    public void SetEnabled(bool enabled)
    {
        if (IsEnabled == enabled) return;
        IsEnabled = enabled;
        if (!enabled)
        {
            _timer.Stop();
            StopWatering("MCU disabled");
        }
        else
        {
            _timer.Start();
            AddLog("Irrigation MCU enabled.");
            EmitSnapshot();
        }
    }

    public void UpdateSettings(double moistureThreshold, double rainLockThreshold, double maxWaterMinutes, double cooldownMinutes)
    {
        MoistureThreshold = Clamp(moistureThreshold, 5, 90);
        RainLockThreshold = Clamp(rainLockThreshold, 0, 100);
        MaxWaterMinutes = Clamp(maxWaterMinutes, 1, 30);
        CooldownMinutes = Clamp(cooldownMinutes, 1, 120);
        AddLog($"Settings updated: Moisture<{MoistureThreshold}% | Rain lock>{RainLockThreshold}% | Max {MaxWaterMinutes}m | Cooldown {CooldownMinutes}m");
    }

    private void Tick()
    {
        if (!IsEnabled) return;

        _environment.Drift();
        _environment.ApplyIrrigationEffect(_valve.IsOpen);

        _soilSensor.Update(_environment.SoilMoisture);
        _rainSensor.Update(_environment.RainLevel);
        _tempSensor.Update(_environment.TemperatureC);

        Evaluate();
        EmitSnapshot();
    }

    private void Evaluate()
    {
        var now = DateTime.UtcNow;
        var rainLocked = _rainSensor.RainLevel >= RainLockThreshold;

        if (rainLocked && _valve.IsOpen)
        {
            StopWatering("Rain lock active.");
        }

        var needsWater = _soilSensor.MoisturePercent < MoistureThreshold;
        var inCooldown = now < _lastCoolDownEndUtc;

        if (!rainLocked && needsWater && !inCooldown && !_valve.IsOpen)
        {
            StartWatering();
        }

        if (_valve.IsOpen)
        {
            if (!needsWater || rainLocked)
            {
                StopWatering(needsWater ? "Rain lock triggered." : "Moisture recovered.");
                return;
            }

            if (_wateringStartedUtc.HasValue && now - _wateringStartedUtc.Value > TimeSpan.FromMinutes(MaxWaterMinutes))
            {
                StopWatering("Max watering duration reached.");
            }
        }
    }

    private void StartWatering()
    {
        _valve.Open();
        _wateringStartedUtc = DateTime.UtcNow;
        AddLog("Sprinkler valve opened (watering started).");
        EmitSnapshot();
    }

    private void StopWatering(string reason)
    {
        if (_valve.IsOpen)
        {
            _valve.Close();
            _lastCoolDownEndUtc = DateTime.UtcNow.AddMinutes(CooldownMinutes);
            AddLog($"Sprinkler valve closed. Reason: {reason} Cooldown until {_lastCoolDownEndUtc:HH:mm}.");
        }
        _wateringStartedUtc = null;
        EmitSnapshot();
    }

    private void AddLog(string message)
    {
        LogEntries.Insert(0, new LogEntry(DateTime.Now, message));
        ImportantEvent?.Invoke(this, message);
    }

    private void EmitSnapshot()
    {
        var snap = new IrrigationSnapshot(
            _soilSensor.MoisturePercent,
            _tempSensor.TemperatureC,
            _rainSensor.RainLevel,
            _valve.IsOpen,
            _wateringStartedUtc != null,
            _rainSensor.RainLevel >= RainLockThreshold);
        SnapshotChanged?.Invoke(this, snap);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer.Stop();
        _timer.Tick -= _tickHandler;
    }

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : (value > max ? max : value);
}
