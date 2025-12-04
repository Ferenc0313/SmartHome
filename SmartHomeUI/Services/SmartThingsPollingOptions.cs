using System;

namespace SmartHomeUI.Services;

public sealed class SmartThingsPollingOptions
{
    /// <summary>
    /// Base interval between polls.
    /// </summary>
    public TimeSpan Interval { get; set; } = TimeSpan.FromSeconds(15);

    /// <summary>
    /// Optional jitter in milliseconds (+/- applied to Interval).
    /// </summary>
    public int JitterMilliseconds { get; set; } = 1000;
}

