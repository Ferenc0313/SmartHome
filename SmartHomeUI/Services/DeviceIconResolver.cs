namespace SmartHomeUI.Services;

public sealed class DeviceIconResolver : IDeviceIconResolver
{
    public string ResolveIcon(DeviceType type) => type switch
    {
        DeviceType.SmartPlug => "Assets/Icons/Smart_Plugg.png",
        DeviceType.SmartBulb => "Assets/Icons/Smart_Bulb.png",
        DeviceType.SmartDoor => "Assets/Icons/closed-door-with-border-silhouette.png",
        DeviceType.CoSafetyMcu => "Assets/Icons/MCU.png",
        _ => "E80F"                       // default home icon
    };
}
