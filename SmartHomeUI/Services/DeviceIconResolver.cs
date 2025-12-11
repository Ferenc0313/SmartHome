namespace SmartHomeUI.Services;

public sealed class DeviceIconResolver : IDeviceIconResolver
{
    public string ResolveIcon(DeviceType type) => type switch
    {
        DeviceType.SmartPlug => "Assets/Icons/Smart_Plug.png",   // custom Smart Plug image (pack URI built in converter)
        DeviceType.SmartBulb => "E7F8",   // bulb placeholder
        DeviceType.SmartDoor => "Assets/Icons/door.png", // door image
        _ => "E80F"                       // default home icon
    };
}
