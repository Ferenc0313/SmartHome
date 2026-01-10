using System;
using System.Linq;
using SmartHomeUI.Domain.Mcu;

namespace SmartHomeUI.Services;

/// <summary>
/// Hosts the virtual irrigation MCU; only runs when all required virtual devices exist for the current user.
/// </summary>
public static class IrrigationMcuRuntime
{
    private static IrrigationMcuController? _controller;
    private static IrrigationSnapshot? _last;

    public static event EventHandler<IrrigationSnapshot>? SnapshotUpdated;
    public static event EventHandler<bool>? ReadinessChanged;
    public static event EventHandler<string>? StatusChanged;

    public static bool IsReady { get; private set; }
    public static string LastStatus { get; private set; } = "Waiting for devices.";
    public static IrrigationSnapshot? LastSnapshot => _last;
    public static IrrigationMcuController? Controller => _controller;

    public static void SyncWithDevices()
    {
        var (ready, reason) = HasRequiredDevicesAndOnState();
        LastStatus = reason;
        StatusChanged?.Invoke(null, reason);

        if (!ready)
        {
            Stop();
            return;
        }
        EnsureStarted();
    }

    public static void Stop()
    {
        if (_controller != null)
        {
            _controller.SetEnabled(false);
        }
        if (IsReady)
        {
            IsReady = false;
            ReadinessChanged?.Invoke(null, false);
        }
    }

    private static void EnsureStarted()
    {
        if (_controller == null)
        {
            _controller = new IrrigationMcuController();
            _controller.SnapshotChanged += OnSnapshotChanged;
            _controller.ImportantEvent += (_, msg) => DeviceService.AddActivityMessage(msg);
        }
        _controller.SetEnabled(true);
        if (!IsReady)
        {
            IsReady = true;
            ReadinessChanged?.Invoke(null, true);
        }
    }

    private static void OnSnapshotChanged(object? sender, IrrigationSnapshot snap)
    {
        _last = snap;
        SnapshotUpdated?.Invoke(sender, snap);

        var valve = DeviceService.Devices.FirstOrDefault(d => d.Type.Equals("SprinklerValve", StringComparison.OrdinalIgnoreCase));
        if (valve != null)
        {
            DeviceService.UpdateStateFromExternal(valve.Id, snap.ValveOpen);
        }
    }

    private static (bool ok, string reason) HasRequiredDevicesAndOnState()
    {
        if (!DeviceService.Devices.Any()) return (false, "No devices for current user.");

        bool hasMcu = DeviceService.Devices.Any(d => d.Type.Equals("SprinklerMcu", StringComparison.OrdinalIgnoreCase));
        bool hasMoisture = DeviceService.Devices.Any(d => d.Type.Equals("SoilMoistureSensor", StringComparison.OrdinalIgnoreCase));
        bool hasTemp = DeviceService.Devices.Any(d => d.Type.Equals("TempSensor", StringComparison.OrdinalIgnoreCase));
        bool hasRain = DeviceService.Devices.Any(d => d.Type.Equals("RainSensor", StringComparison.OrdinalIgnoreCase));
        bool hasValve = DeviceService.Devices.Any(d => d.Type.Equals("SprinklerValve", StringComparison.OrdinalIgnoreCase));

        if (!(hasMcu && hasMoisture && hasTemp && hasRain && hasValve))
        {
            var missing = string.Join(", ", new[]
            {
                hasMcu ? null : "Sprinkler MCU",
                hasMoisture ? null : "Soil moisture sensor",
                hasTemp ? null : "Temperature sensor",
                hasRain ? null : "Rain sensor",
                hasValve ? null : "Sprinkler valve",
            }.Where(s => !string.IsNullOrWhiteSpace(s)));

            return (false, $"Missing irrigation devices: {missing}.");
        }

        // Mimic CO safety behavior: require the core devices to be turned on to enable automation.
        // The valve itself can be off (closed) and is controlled by the MCU, so we don't require it to be on here.
        if (!DeviceService.IsDeviceTypeOn("SprinklerMcu"))
            return (false, "Turn on the Sprinkler MCU to start automation.");
        if (!DeviceService.IsDeviceTypeOn("SoilMoistureSensor"))
            return (false, "Turn on the Soil moisture sensor to start automation.");
        if (!DeviceService.IsDeviceTypeOn("TempSensor"))
            return (false, "Turn on the Temperature sensor to start automation.");
        if (!DeviceService.IsDeviceTypeOn("RainSensor"))
            return (false, "Turn on the Rain sensor to start automation.");

        return (true, "Virtual irrigation automation is running.");
    }
}
