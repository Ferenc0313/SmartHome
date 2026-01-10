using System;

namespace SmartHomeUI.Domain.Devices;

public sealed class RainSensor
{
    public event EventHandler<double>? RainChanged;

    public double RainLevel { get; private set; }

    public void Update(double newValue)
    {
        var clamped = Clamp(newValue, 0, 100);
        if (Math.Abs(clamped - RainLevel) < 0.001) return;
        RainLevel = clamped;
        RainChanged?.Invoke(this, RainLevel);
    }

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : (value > max ? max : value);
}
