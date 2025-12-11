using System;

namespace SmartHomeUI.Domain.Devices;

public class CoDetector
{
    private readonly CoSensor _sensor;
    private bool _isActive;

    public CoDetector(CoSensor sensor)
    {
        _sensor = sensor;
        _sensor.CoStateChanged += OnSensorStateChanged;
    }

    public bool IsActive => _isActive;

    public event EventHandler? CoAlarmRaised;
    public event EventHandler? CoAlarmCleared;

    private void OnSensorStateChanged(object? sender, CoStateChangedEventArgs e)
    {
        if (e.NewState == CoState.Critical && !_isActive)
        {
            _isActive = true;
            CoAlarmRaised?.Invoke(this, EventArgs.Empty);
        }
        else if (e.NewState == CoState.Normal && _isActive)
        {
            _isActive = false;
            CoAlarmCleared?.Invoke(this, EventArgs.Empty);
        }
    }
}
