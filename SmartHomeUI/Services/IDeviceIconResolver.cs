namespace SmartHomeUI.Services;

public interface IDeviceIconResolver
{
    string ResolveIcon(DeviceType type);
}
