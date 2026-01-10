using System;

namespace SmartHomeUI.Domain.Devices;

public sealed class TemperatureSensor
{
    public event EventHandler<double>? TemperatureChanged;

    public double TemperatureC { get; private set; }

    public void Update(double newValue)
    {
        var clamped = Clamp(newValue, -20, 50);
        if (Math.Abs(clamped - TemperatureC) < 0.001) return;
        TemperatureC = clamped;
        TemperatureChanged?.Invoke(this, TemperatureC);
    }

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : (value > max ? max : value);
}
