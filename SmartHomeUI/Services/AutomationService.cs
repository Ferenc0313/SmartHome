using System;
using System.Linq;
using System.Windows.Threading;
using SmartHomeUI.Data;
using SmartHomeUI.Models;

namespace SmartHomeUI.Services;

public static class AutomationService
{
    private static readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) }; // 15s for better hit rate
    // Track last run as full minute timestamp (date+time), so daily schedules fire every day
    private static System.Collections.Generic.Dictionary<int, DateTime> _lastRun = new();
    private static DateTime _lastTick = DateTime.Now;

    public static void Start()
    {
        _timer.Tick += (_, __) => Tick();
        _timer.Start();
    }

    private static void Tick()
    {
        try
        {
            using var db = new SmartHomeDbContext();
            var nowDt = DateTime.Now;
            var startMin = new DateTime(_lastTick.Year, _lastTick.Month, _lastTick.Day, _lastTick.Hour, _lastTick.Minute, 0);
            var endMin = new DateTime(nowDt.Year, nowDt.Month, nowDt.Day, nowDt.Hour, nowDt.Minute, 0);
            var automations = db.Set<Automation>().Where(a => a.Enabled).ToList();

            for (var t = startMin; t <= endMin; t = t.AddMinutes(1))
            {
                foreach (var a in automations)
                {
                    if (a.TimeHHmm == t.ToString("HH:mm"))
                    {
                        if (_lastRun.TryGetValue(a.Id, out var last) && last == t) continue; // already ran for this exact minute
                        Run(a);
                        _lastRun[a.Id] = t;
                    }
                }
            }
            _lastTick = nowDt;
        }
        catch { }
    }

    public static void RunNow(int id)
    {
        try
        {
            using var db = new SmartHomeDbContext();
            var a = db.Set<Automation>().FirstOrDefault(x => x.Id == id);
            if (a != null)
            {
                // Mark as executed for this minute to avoid duplicate scheduled run
                var minute = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
                                          DateTime.Now.Hour, DateTime.Now.Minute, 0);
                _lastRun[a.Id] = minute;
                Run(a);
            }
        }
        catch { }
    }

    private static void Run(Automation a)
    {
        if (a.Action == "SetOnOff")
            DeviceService.SetOnOffPersist(a.DeviceId, a.Value >= 0.5);
        else if (a.Action == "Toggle")
            DeviceService.TogglePersist(a.DeviceId);
        else if (a.Action == "SetValue")
            DeviceService.SetValue(a.DeviceId, a.Value);
    }
}


