using System;

namespace SmartHomeUI.Domain.Mcu;

public class LogEntry
{
    public LogEntry(DateTime timestamp, string message)
    {
        Timestamp = timestamp;
        Message = message;
    }

    public DateTime Timestamp { get; }
    public string Message { get; }
}
