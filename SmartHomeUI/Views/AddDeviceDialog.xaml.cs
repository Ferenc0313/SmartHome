using System.Collections.Generic;
using System.Windows;

namespace SmartHomeUI.Views;

public partial class AddDeviceDialog : Window
{
    public record DeviceOption(string Name, string Glyph, string IconKey, string Type);

    public DeviceOption? SelectedOption { get; private set; }

    public AddDeviceDialog()
    {
        InitializeComponent();
        DevicesList.ItemsSource = BuildOptions();
    }

    private IEnumerable<DeviceOption> BuildOptions() => new List<DeviceOption>
    {
        new("Smart Bulb", "\uE7F8", "E7F8", "Light"),
        new("Smart Plug", "\uE95F", "E95F", "Plug"),
        new("Smart Lock", "\uE72E", "E72E", "Lock"),
        new("Thermostat", "\uE814", "E814", "Thermostat"),
        new("Camera", "\uE722", "E722", "Camera"),
        new("Motion Sensor", "\uE7ED", "E7ED", "SensorMotion"),
        new("Smoke Alarm", "\uE7F4", "E7F4", "Alarm"),
        new("Speaker", "\uE767", "E767", "Speaker"),
        new("TV", "\uE7F4", "E7F4", "TV"),
        new("Weather Station", "\uE706", "E706", "Weather"),
        // CO safety kit
        new("CO Sensor", "\uE9CA", "E9CA", "CoSensor"),     // gas sensor glyph
        new("CO Detector", "\uE7E7", "E7E7", "CoDetector"), // alert/bell style
        new("Smart Door", "\uE8A7", "Assets/Icons/door.png", "SmartDoor"),   // neon door image
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
}
