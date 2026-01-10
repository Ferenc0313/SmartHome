using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

namespace SmartHomeUI.Views;

public partial class AddDeviceDialog : Window
{
    public record DeviceOption(string Name, string IconKey, string Type);

    public DeviceOption? SelectedOption { get; private set; }

    public AddDeviceDialog()
    {
        InitializeComponent();
        DevicesList.ItemsSource = BuildOptions();
    }

    private IEnumerable<DeviceOption> BuildOptions() => new List<DeviceOption>
    {
        new("Smart Bulb", "Assets/Icons/Smart_Bulb.png", "Light"),
        new("Smart Plug", "Assets/Icons/Smart_Plugg.png", "Plug"),
        new("Smart Lock", "Assets/Icons/Smart_Lock.png", "Lock"),
        new("Thermostat", "Assets/Icons/Thermostat.png", "Thermostat"),
        new("Camera", "E722", "Camera"),
        new("Motion Sensor", "Assets/Icons/Motion_Sensor.png", "SensorMotion"),
        new("Smoke Alarm", "Assets/Icons/Smoke_Alarm.png", "Alarm"),
        new("Speaker", "E767", "Speaker"),
        new("TV", "E7F4", "TV"),
        new("Weather Station", "Assets/Icons/Weather_Station.png", "Weather"),
        // CO safety kit
        new("CO Sensor", "Assets/Icons/CO_Sensor.png", "CoSensor"),
        new("CO Detector", "Assets/Icons/CO_Detector.png", "CoDetector"),
        new("Smart Door", "Assets/Icons/closed-door-with-border-silhouette.png", "SmartDoor"),
        new("CO Safety MCU", "Assets/Icons/MCU.png", "CoSafetyMcu"),
        // Irrigation kit
        new("Sprinkler MCU", "Assets/Icons/MCU.png", "SprinklerMcu"),
        new("Soil Moisture Sensor", "Assets/Icons/Soil_Sensor.png", "SoilMoistureSensor"),
        new("Rain Sensor", "Assets/Icons/Rain_Sensor.png", "RainSensor"),
        new("Temperature Sensor", "Assets/Icons/Temperature_Sensor.png", "TempSensor"),
        new("Sprinkler Valve", "Assets/Icons/Sprinkler.png", "SprinklerValve"),
    };

    private void DeviceSelect_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is DeviceOption option)
        {
            SelectedOption = option;
            DialogResult = true;
            Close();
        }
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }
}
