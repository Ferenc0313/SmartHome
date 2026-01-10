using System;

namespace SmartHomeUI.Domain.Devices;

public class VirtualCoEnvironment
{
    private readonly Random _random = new();
    private double _coLevel = 18; // start with a mild background

    public event EventHandler<double>? CoLevelChanged;

    public double CoLevel
    {
        get => _coLevel;
        private set
        {
            var clamped = Math.Clamp(value, 0, 100);
            if (Math.Abs(clamped - _coLevel) < 0.0001) return;
            _coLevel = clamped;
            CoLevelChanged?.Invoke(this, _coLevel);
        }
    }

    public void SetCoLevel(double value) => CoLevel = value;

    /// <summary>
    /// Natural drift of CO level (background changes).
    /// </summary>
    public void Drift()
    {
        var delta = (_random.NextDouble() - 0.5) * 6; // -3..+3
        CoLevel = CoLevel + delta;
    }

    /// <summary>
    /// Ventilation when a door/window is open.
    /// </summary>
    public void Ventilate(double strength)
    {
        CoLevel = Math.Max(0, CoLevel - Math.Abs(strength));
    }
}
