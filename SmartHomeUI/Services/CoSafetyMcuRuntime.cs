using System;
using System.Linq;
using SmartHomeUI.Domain.Mcu;

namespace SmartHomeUI.Services;

/// <summary>
/// Hosts the virtual CO safety MCU; only runs when required devices exist AND are turned on.
/// </summary>
public static class CoSafetyMcuRuntime
{
    private static CoSafetyMcuController? _controller;
    private static CoSafetySnapshot? _last;

    public static event EventHandler<CoSafetySnapshot>? SnapshotUpdated;
    public static event EventHandler<bool>? ReadinessChanged;
    public static event EventHandler<string>? StatusChanged;

    public static bool IsReady { get; private set; }
    public static string LastStatus { get; private set; } = "Waiting for devices.";
    public static CoSafetySnapshot? LastSnapshot => _last;
    public static CoSafetyMcuController? Controller => _controller;

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
        _controller?.SetEnabled(false);
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
            _controller = new CoSafetyMcuController();
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

    private static void OnSnapshotChanged(object? sender, CoSafetySnapshot snap)
    {
        _last = snap;
        SnapshotUpdated?.Invoke(sender, snap);

        // Sync SmartDoor on/off state so UI tiles reflect the automation
        DeviceService.SetSmartDoorStateForCoSafety(snap.DoorOpen);
    }

    private static (bool ok, string reason) HasRequiredDevicesAndOnState()
    {
        if (!DeviceService.Devices.Any()) return (false, "No devices for current user.");

        bool hasMcu = DeviceService.Devices.Any(d => d.Type.Equals("CoSafetyMcu", StringComparison.OrdinalIgnoreCase));
        bool hasSensor = DeviceService.Devices.Any(d => d.Type.Equals("CoSensor", StringComparison.OrdinalIgnoreCase));
        bool hasDetector = DeviceService.Devices.Any(d => d.Type.Equals("CoDetector", StringComparison.OrdinalIgnoreCase));
        if (!(hasMcu && hasSensor && hasDetector))
        {
            return (false, "Missing CO MCU or required virtual devices (sensor, detector).");
        }

        if (!DeviceService.IsDeviceTypeOn("CoSafetyMcu"))
            return (false, "Turn on the CO Safety MCU to start automation.");
        if (!DeviceService.IsDeviceTypeOn("CoSensor"))
            return (false, "Turn on the CO Sensor to start automation.");
        if (!DeviceService.IsDeviceTypeOn("CoDetector"))
            return (false, "Turn on the CO Detector to start automation.");

        return (true, "CO safety automation is running.");
    }
}
