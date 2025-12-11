using System.ComponentModel;
using System.Runtime.CompilerServices;
using SmartHomeUI.Domain.Devices;

namespace SmartHomeUI.Presentation.ViewModels;

public sealed class CoSafetyUiState : INotifyPropertyChanged
{
    private static readonly CoSafetyUiState _instance = new();

    private CoState _sensorState = CoState.Normal;
    private bool _detectorActive;
    private bool _doorEmergency;

    public static CoSafetyUiState Instance => _instance;

    public CoState SensorState
    {
        get => _sensorState;
        set
        {
            if (_sensorState == value) return;
            _sensorState = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsWarning));
            OnPropertyChanged(nameof(IsCritical));
        }
    }

    public bool IsWarning => SensorState == CoState.Warning;
    public bool IsCritical => SensorState == CoState.Critical;
    public bool DetectorActive
    {
        get => _detectorActive;
        set
        {
            if (_detectorActive == value) return;
            _detectorActive = value;
            OnPropertyChanged();
        }
    }

    public bool DoorEmergency
    {
        get => _doorEmergency;
        set
        {
            if (_doorEmergency == value) return;
            _doorEmergency = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
