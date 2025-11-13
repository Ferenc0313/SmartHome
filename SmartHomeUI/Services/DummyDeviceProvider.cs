using System;
using System.Collections.Generic;
using System.Linq;
using STimer = System.Timers.Timer;

namespace SmartHomeUI.Services;

public sealed class DummyDeviceProvider : IDeviceProvider
{
    private readonly STimer _timer = new(1200);
    private readonly Random _rnd = new();
    private List<int> _ids = new();

    public event Action<int>? Ticked;

    public DummyDeviceProvider()
    {
        _timer.Elapsed += (_, __) =>
        {
            if (_ids.Count == 0) return;
            // randomly pick 1-2 devices to signal update
            var count = Math.Min(2, _ids.Count);
            foreach (var id in _ids.OrderBy(_ => _rnd.Next()).Take(count))
                Ticked?.Invoke(id);
        };
    }

    public void Start(IEnumerable<int> deviceIds)
    {
        _ids = deviceIds.Distinct().ToList();
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
        _ids.Clear();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}
