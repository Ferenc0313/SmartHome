using System;

namespace SmartHomeUI.Domain.Devices;

public class VirtualCoEnvironment
{
    private double _coLevel;

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
}
