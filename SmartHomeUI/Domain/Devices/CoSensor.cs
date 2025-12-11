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
    private const double WarningThreshold = 30.0;
    private const double CriticalThreshold = 60.0;

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

    private static CoState EvaluateState(double level)
    {
        if (level >= CriticalThreshold) return CoState.Critical;
        if (level >= WarningThreshold) return CoState.Warning;
        return CoState.Normal;
    }
}
