using System;

namespace SmartHomeUI.Domain.Devices;

public class CoStateChangedEventArgs : EventArgs
{
    public CoStateChangedEventArgs(CoState previousState, CoState newState, double level)
    {
        PreviousState = previousState;
        NewState = newState;
        Level = level;
    }

    public CoState PreviousState { get; }
    public CoState NewState { get; }
    public double Level { get; }
}

public class CoSensor
{
    private double _warningThreshold = 30.0;
    private double _criticalThreshold = 60.0;

    private readonly VirtualCoEnvironment _environment;
    private CoState _state = CoState.Normal;

    public CoSensor(VirtualCoEnvironment environment)
    {
        _environment = environment;
        _environment.CoLevelChanged += OnEnvironmentLevelChanged;
    }

    public double CurrentLevel { get; private set; }
    public CoState State => _state;

    public event EventHandler<CoStateChangedEventArgs>? CoStateChanged;

    private void OnEnvironmentLevelChanged(object? sender, double newLevel)
    {
        CurrentLevel = newLevel;
        var newState = EvaluateState(newLevel);
        if (newState == _state) return;

        var previous = _state;
        _state = newState;
        CoStateChanged?.Invoke(this, new CoStateChangedEventArgs(previous, _state, newLevel));
    }

    public void UpdateThresholds(double warning, double critical)
    {
        _warningThreshold = Math.Clamp(warning, 5, 95);
        _criticalThreshold = Math.Clamp(critical, _warningThreshold + 1, 100);
    }

    private CoState EvaluateState(double level)
    {
        if (level >= _criticalThreshold) return CoState.Critical;
        if (level >= _warningThreshold) return CoState.Warning;
        return CoState.Normal;
    }
}
