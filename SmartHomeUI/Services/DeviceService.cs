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
            _activity.Insert(0, new ActivityEntry { Time = time, Message = message });
            while (_activity.Count > 30) _activity.RemoveAt(_activity.Count - 1);
        });
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

    private static string ResolveIconFromType(string? type)
    {
        if (string.IsNullOrWhiteSpace(type)) return "E80F";
        return type.Equals("SmartPlug", StringComparison.OrdinalIgnoreCase) ? "Assets/Icons/Smart_Plug.png"
             : type.Equals("SmartBulb", StringComparison.OrdinalIgnoreCase) ? "E7F8"
             : "E80F";
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
    }
}
