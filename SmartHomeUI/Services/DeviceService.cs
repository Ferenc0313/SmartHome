using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Threading;
using SmartHomeUI.Data;
using SmartHomeUI.Models;

namespace SmartHomeUI.Services;

public static class DeviceService
{
public sealed class ActivityEntry
{
    public string Time { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

public sealed class WidgetCapabilities
{
    public bool HasSecurityDevices { get; init; }
    public bool HasEnergyDevices { get; init; }
    public bool HasClimateDevices { get; init; }
}

    public sealed class DeviceState
    {
        public bool IsOnline { get; set; } = true;
        public bool IsOn { get; set; }
        public int Battery { get; set; } = 100;
        public DateTime LastSeen { get; set; } = DateTime.UtcNow;
    }

    private static readonly ObservableCollection<Device> _devices = new();
    public static ReadOnlyObservableCollection<Device> Devices { get; } = new(_devices);

    private static readonly Dictionary<int, DeviceState> _state = new();
    public static event Action? StateChanged;
    public static event Action? DevicesChanged;

    private static IDeviceProvider? _provider;
    private static int? _userId;

    private static readonly ObservableCollection<ActivityEntry> _activity = new();
    public static ReadOnlyObservableCollection<ActivityEntry> Activity { get; } = new(_activity);
    private static string? _lastActivityKey;

    private static readonly HashSet<string> SecurityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Lock",
        "Camera",
        "SensorMotion",
        "Alarm"
    };

    private static readonly HashSet<string> EnergyTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Plug",
        "Light"
    };

    private static readonly HashSet<string> ClimateTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "Thermostat",
        "Weather"
    };

    private static Dispatcher? _dispatcher;
    public static void InitializeDispatcher(Dispatcher dispatcher) => _dispatcher = dispatcher;

    public static void LoadForUser(int userId)
    {
        _userId = userId;
        ReloadForCurrentUser();
    }

    public static void ReloadForCurrentUser()
    {
        if (_userId == null) return;
        using var db = new SmartHomeDbContext();
        var list = db.Devices.Where(d => d.UserId == _userId.Value).OrderBy(d => d.Name).ToList();

        // Backfill missing icon/type for physical devices
        bool changed = false;
        foreach (var d in list)
        {
            if (d.IsPhysical)
            {
                if (string.IsNullOrWhiteSpace(d.IconKey))
                {
                    d.IconKey = ResolveIconFromType(d.Type);
                    changed = true;
                }
                if (string.IsNullOrWhiteSpace(d.Type))
                {
                    d.Type = "SmartPlug";
                    changed = true;
                }
            }

            if (!string.IsNullOrWhiteSpace(d.Type))
            {
                var resolved = ResolveIconFromType(d.Type);
                if (ShouldNormalizeIconKey(d.IconKey, resolved))
                {
                    d.IconKey = resolved;
                    changed = true;
                }
            }
        }
        if (changed) db.SaveChanges();

        _devices.Clear();
        foreach (var d in list) _devices.Add(d);

        var now = DateTime.UtcNow;
        _state.Clear();
        foreach (var d in list)
            _state[d.Id] = new DeviceState { IsOnline = d.IsOnline, IsOn = d.IsOn, Battery = d.Battery > 0 ? d.Battery : 80, LastSeen = d.LastSeen ?? now };

        _provider?.Stop();
        _provider?.Dispose();
        _provider = new DummyDeviceProvider();
        _provider.Ticked += id =>
        {
            if (_state.TryGetValue(id, out var s))
            {
                // Only simulate a heartbeat: update last seen and a gentle battery drain.
                // Do NOT toggle state and do NOT log activity here.
                s.Battery = Math.Max(5, s.Battery - 1);
                s.LastSeen = DateTime.UtcNow;
                RaiseOnUI(StateChanged);
            }
        };
        _provider.Start(list.Select(d => d.Id));

        RaiseOnUI(DevicesChanged);
        RaiseOnUI(StateChanged);
        IrrigationMcuRuntime.SyncWithDevices();
        CoSafetyMcuRuntime.SyncWithDevices();
    }

    public static void Clear()
    {
        _userId = null;
        _provider?.Stop();
        _provider?.Dispose();
        _provider = null;
        _devices.Clear();
        _state.Clear();
        _activity.Clear();
        RaiseOnUI(DevicesChanged);
        RaiseOnUI(StateChanged);
        IrrigationMcuRuntime.Stop();
        CoSafetyMcuRuntime.Stop();
    }

    public static (int total, int online, int onCount, int lowBattery) GetAggregates()
    {
        var total = _devices.Count;
        var online = _state.Count(kv => kv.Value.IsOnline);
        var onCount = _state.Count(kv => kv.Value.IsOn);
        var low = _state.Count(kv => kv.Value.Battery <= 15);
        return (total, online, onCount, low);
    }

    public static WidgetCapabilities GetWidgetCapabilities()
    {
        if (_devices.Count == 0)
        {
            return new WidgetCapabilities();
        }

        static bool ContainsType(IEnumerable<Device> devices, HashSet<string> typeSet) =>
            devices.Any(d => !string.IsNullOrWhiteSpace(d.Type) && typeSet.Contains(d.Type));

        var snapshot = _devices.ToList();
        return new WidgetCapabilities
        {
            HasSecurityDevices = ContainsType(snapshot, SecurityTypes),
            HasEnergyDevices = ContainsType(snapshot, EnergyTypes),
            HasClimateDevices = ContainsType(snapshot, ClimateTypes)
        };
    }

    public static void Toggle(int deviceId)
    {
        if (_state.TryGetValue(deviceId, out var s))
        {
            s.IsOn = !s.IsOn;
            s.LastSeen = DateTime.UtcNow;
            var dev = _devices.FirstOrDefault(d => d.Id == deviceId);
            AddActivity($"{dev?.Name ?? ("Device " + deviceId)} turned {(s.IsOn ? "on" : "off")}");
            RaiseOnUI(StateChanged);
            CoSafetyMcuRuntime.SyncWithDevices();
            IrrigationMcuRuntime.SyncWithDevices();
        }
    }

    public static void TogglePersist(int deviceId)
    {
        var dev = _devices.FirstOrDefault(d => d.Id == deviceId);
        if (dev is null) return;

        var targetState = !dev.IsOn;

        // Physical device -> SmartThings call
        if (dev.IsPhysical && !string.IsNullOrWhiteSpace(dev.PhysicalDeviceId))
        {
            var pat = NormalizePat(AuthService.CurrentSmartThingsPat ?? Environment.GetEnvironmentVariable("SMARTTHINGS_PAT"));
            if (string.IsNullOrWhiteSpace(pat))
            {
                AddActivity("SmartThings token missing. Cannot toggle physical device.");
                return;
            }

            var ok = SmartThingsSwitchService.SetSwitchState(pat, dev.PhysicalDeviceId, targetState);
            if (!ok)
            {
                AddActivity($"SmartThings toggle failed for {dev.Name}.");
                return;
            }
        }

        if (_state.TryGetValue(deviceId, out var s))
        {
            s.IsOn = targetState;
            s.LastSeen = DateTime.UtcNow;
        }

        using (var db = new SmartHomeDbContext())
        {
            var tracked = db.Devices.FirstOrDefault(d => d.Id == deviceId);
            if (tracked is not null)
            {
                tracked.IsOn = targetState;
                tracked.LastSeen = DateTime.UtcNow;
                db.SaveChanges();
            }
        }

        RefreshDeviceInCollection(deviceId, d => d.IsOn = targetState);
        AddActivity($"{dev.Name} turned {(targetState ? "on" : "off")}");
        RaiseOnUI(StateChanged);
        // Ensure MCU runtimes react immediately to on/off changes.
        CoSafetyMcuRuntime.SyncWithDevices();
        IrrigationMcuRuntime.SyncWithDevices();
    }

    public static void SetOnOffPersist(int deviceId, bool isOn)
    {
        var dev = _devices.FirstOrDefault(d => d.Id == deviceId);
        if (dev is null) return;

        // Physical device -> SmartThings call
        if (dev.IsPhysical && !string.IsNullOrWhiteSpace(dev.PhysicalDeviceId))
        {
            var pat = NormalizePat(AuthService.CurrentSmartThingsPat ?? Environment.GetEnvironmentVariable("SMARTTHINGS_PAT"));
            if (string.IsNullOrWhiteSpace(pat))
            {
                AddActivity("SmartThings token missing. Cannot toggle physical device.");
                return;
            }

            var ok = SmartThingsSwitchService.SetSwitchState(pat, dev.PhysicalDeviceId, isOn);
            if (!ok)
            {
                AddActivity($"SmartThings toggle failed for {dev.Name}.");
                return;
            }
        }

        if (_state.TryGetValue(deviceId, out var s))
        {
            s.IsOn = isOn;
            s.LastSeen = DateTime.UtcNow;
        }

        using (var db = new SmartHomeDbContext())
        {
            var tracked = db.Devices.FirstOrDefault(d => d.Id == deviceId);
            if (tracked is not null)
            {
                tracked.IsOn = isOn;
                tracked.LastSeen = DateTime.UtcNow;
                db.SaveChanges();
            }
        }

        RefreshDeviceInCollection(deviceId, d => d.IsOn = isOn);
        AddActivity($"{dev.Name} turned {(isOn ? "on" : "off")}");
        RaiseOnUI(StateChanged);
        CoSafetyMcuRuntime.SyncWithDevices();
        IrrigationMcuRuntime.SyncWithDevices();
    }

    public static string NormalizePat(string? pat)
    {
        if (string.IsNullOrWhiteSpace(pat)) return string.Empty;
        return new string(pat.Where(c => !char.IsWhiteSpace(c)).ToArray());
    }

    public static void SetValue(int deviceId, double value)
    {
        // Persist to DB regardless of runtime cache availability
        using (var db = new SmartHomeDbContext())
        {
            var tracked = db.Devices.FirstOrDefault(d => d.Id == deviceId);
            if (tracked is not null)
            {
                tracked.Value = value;
                tracked.LastSeen = DateTime.UtcNow;
                db.SaveChanges();
            }
        }

        if (_state.TryGetValue(deviceId, out var s))
        {
            s.LastSeen = DateTime.UtcNow;
        }

        var dev = _devices.FirstOrDefault(d => d.Id == deviceId);
        RefreshDeviceInCollection(deviceId, d => d.Value = value);
        AddActivity($"{dev?.Name ?? ("Device " + deviceId)} value set to {value:0}");
        RaiseOnUI(StateChanged);
        // Some automations/MCUs depend on device readings.
        CoSafetyMcuRuntime.SyncWithDevices();
        IrrigationMcuRuntime.SyncWithDevices();
    }

    private static void AddActivity(string message)
    {
        var now = DateTime.Now;
        var time = now.ToString("HH:mm");
        var key = now.ToString("yyyyMMddHHmm") + "|" + message;
        if (_lastActivityKey == key) return; // prevent duplicates within the same minute for identical message
        _lastActivityKey = key;
        RunOnUI(() =>
        {
            _activity.Insert(0, new ActivityEntry { Time = time, Message = message, CreatedAt = DateTime.UtcNow });
            while (_activity.Count > 30) _activity.RemoveAt(_activity.Count - 1);
            // Drop entries older than 24h to limit retention
            var cutoff = DateTime.UtcNow.AddHours(-24);
            for (int i = _activity.Count - 1; i >= 0; i--)
            {
                if (_activity[i].CreatedAt < cutoff) _activity.RemoveAt(i);
            }
        });
    }

    public static void AddActivityMessage(string message) => AddActivity(message);

    public static void ClearActivity()
    {
        _lastActivityKey = null;
        RunOnUI(() => _activity.Clear());
    }

    private static void RaiseOnUI(Action? action) => RunOnUI(() => action?.Invoke());

    private static void RunOnUI(Action action)
    {
        var d = _dispatcher ?? System.Windows.Application.Current?.Dispatcher;
        if (d is not null && !d.CheckAccess()) d.Invoke(action);
        else action();
    }

    private static void RefreshDeviceInCollection(int deviceId, Action<Device> mutator)
    {
        var dev = _devices.FirstOrDefault(d => d.Id == deviceId);
        if (dev is null) return;
        mutator(dev);
        var idx = _devices.IndexOf(dev);
        if (idx >= 0)
        {
            var copy = new Device
            {
                Id = dev.Id,
                Name = dev.Name,
                IconKey = dev.IconKey,
                Type = dev.Type,
                Room = dev.Room,
                IsOn = dev.IsOn,
                IsOnline = dev.IsOnline,
                Battery = dev.Battery,
                Value = dev.Value,
                Favorite = dev.Favorite,
                IsPhysical = dev.IsPhysical,
                PhysicalDeviceId = dev.PhysicalDeviceId,
                LastSeen = dev.LastSeen,
                CreatedAt = dev.CreatedAt,
                UserId = dev.UserId,
                User = dev.User
            };
            _devices[idx] = copy; // force Replace with a new instance so bindings refresh
        }
    }

    public static void SetSmartDoorStateForCoSafety(bool isOpen)
    {
        var doors = _devices.Where(d => d.Type.Equals("SmartDoor", StringComparison.OrdinalIgnoreCase)).ToList();
        if (!doors.Any()) return;

        foreach (var door in doors)
        {
            // update runtime state
            if (_state.TryGetValue(door.Id, out var st))
            {
                st.IsOn = isOpen;
                st.LastSeen = DateTime.UtcNow;
            }

            // persist to DB
            using (var db = new SmartHomeDbContext())
            {
                var tracked = db.Devices.FirstOrDefault(d => d.Id == door.Id);
                if (tracked is not null)
                {
                    tracked.IsOn = isOpen;
                    tracked.LastSeen = DateTime.UtcNow;
                    db.SaveChanges();
                }
            }

            // update observable collection copy
            RefreshDeviceInCollection(door.Id, d =>
            {
                d.IsOn = isOpen;
                d.LastSeen = DateTime.UtcNow;
            });
        }

        AddActivity($"Smart door {(isOpen ? "opened" : "closed")} due to CO safety automation.");
        RaiseOnUI(StateChanged);
    }

    private static string ResolveIconFromType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "E80F";
        return type.Equals("SmartPlug", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Smart_Plugg.png"
             : type.Equals("Plug", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Smart_Plugg.png"
             : type.Equals("SmartBulb", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Smart_Bulb.png"
             : type.Equals("Light", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Smart_Bulb.png"
             : type.Equals("Lock", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Smart_Lock.png"
             : type.Equals("Thermostat", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Thermostat.png"
             : type.Equals("Weather", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Weather_Station.png"
             : type.Equals("Alarm", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Smoke_Alarm.png"
             : type.Equals("SensorMotion", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Motion_Sensor.png"
             : type.Equals("CoSensor", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/CO_Sensor.png"
             : type.Equals("CoDetector", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/CO_Detector.png"
             : type.Equals("SmartDoor", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/closed-door-with-border-silhouette.png"
             : type.Equals("SprinklerMcu", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/MCU.png"
             : type.Equals("CoSafetyMcu", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/MCU.png"
             : type.Equals("SoilMoistureSensor", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Soil_Sensor.png"
             : type.Equals("RainSensor", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Rain_Sensor.png"
             : type.Equals("TempSensor", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Temperature_Sensor.png"
             : type.Equals("SprinklerValve", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Sprinkler.png"
             : type.Equals("Camera", StringComparison.OrdinalIgnoreCase) ? "E722"
             : type.Equals("Speaker", StringComparison.OrdinalIgnoreCase) ? "E767"
             : type.Equals("TV", StringComparison.OrdinalIgnoreCase) ? "E7F4"
             : "E80F";
    }

    private static bool ShouldNormalizeIconKey(string? current, string resolved)
    {
        if (string.IsNullOrWhiteSpace(resolved)) return false;
        if (string.IsNullOrWhiteSpace(current)) return true;

        // If we now have a custom image for this type, migrate legacy glyph keys to it.
        if (LooksLikeGlyphKey(current) && LooksLikeImagePath(resolved)) return true;

        // If this device used an older/alternate image key, normalize to the new one.
        if (LooksLikeImagePath(current) && LooksLikeImagePath(resolved) && !current.Equals(resolved, StringComparison.OrdinalIgnoreCase))
        {
            // Keep icons consistent for built-in assets.
            if (resolved.StartsWith("Assets/Icons/", StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    private static bool LooksLikeGlyphKey(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        if (s.Length != 4) return false;
        return int.TryParse(s, System.Globalization.NumberStyles.HexNumber, System.Globalization.CultureInfo.InvariantCulture, out _);
    }

    private static bool LooksLikeImagePath(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        return s.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
               || s.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
               || s.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase)
               || s.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)
               || s.Contains('/', StringComparison.Ordinal)
               || s.Contains("\\", StringComparison.Ordinal);
    }

    public static void UpdateStateFromExternal(int deviceId, bool isOn)
    {
        if (_state.TryGetValue(deviceId, out var s) && s.IsOn == isOn) return;

        if (_state.TryGetValue(deviceId, out var state))
        {
            state.IsOn = isOn;
            state.LastSeen = DateTime.UtcNow;
        }

        using (var db = new SmartHomeDbContext())
        {
            var tracked = db.Devices.FirstOrDefault(d => d.Id == deviceId);
            if (tracked is not null)
            {
                tracked.IsOn = isOn;
                tracked.LastSeen = DateTime.UtcNow;
                db.SaveChanges();
            }
        }

            RefreshDeviceInCollection(deviceId, d =>
            {
                d.IsOn = isOn;
                d.LastSeen = DateTime.UtcNow;
            });

            AddActivity($"{_devices.FirstOrDefault(d => d.Id == deviceId)?.Name ?? ("Device " + deviceId)} state synced to {(isOn ? "on" : "off")} (poll)");
            RaiseOnUI(StateChanged);
            CoSafetyMcuRuntime.SyncWithDevices();
            IrrigationMcuRuntime.SyncWithDevices();
        }

    public static bool IsDeviceTypeOn(string type)
    {
        var dev = _devices.FirstOrDefault(d => d.Type.Equals(type, StringComparison.OrdinalIgnoreCase));
        if (dev is null) return false;
        return _state.TryGetValue(dev.Id, out var st) && st.IsOn;
    }

}
