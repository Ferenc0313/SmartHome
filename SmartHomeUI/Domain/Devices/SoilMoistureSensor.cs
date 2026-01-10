using System;

namespace SmartHomeUI.Domain.Devices;

public sealed class SoilMoistureSensor
{
    public event EventHandler<double>? MoistureChanged;

    public double MoisturePercent { get; private set; }

    public void Update(double newValue)
    {
        var clamped = Clamp(newValue, 0, 100);
        if (Math.Abs(clamped - MoisturePercent) < 0.001) return;
        MoisturePercent = clamped;
        MoistureChanged?.Invoke(this, MoisturePercent);
    }

    private static double Clamp(double value, double min, double max) =>
        value < min ? min : (value > max ? max : value);
}
