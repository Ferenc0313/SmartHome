using SmartHomeUI.Models;

namespace SmartHomeUI.Services;

public interface IDeviceTypeResolver
{
    DeviceType Resolve(SmartThingsDeviceDto dto);
}
