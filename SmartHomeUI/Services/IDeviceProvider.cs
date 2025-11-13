using System;
using System.Collections.Generic;

namespace SmartHomeUI.Services;

public interface IDeviceProvider : IDisposable
{
    void Start(IEnumerable<int> deviceIds);
    void Stop();
    event Action<int>? Ticked; // deviceId state updated
}

