using System.Collections.Generic;
using SmartHomeUI.Services;

namespace SmartHomeUI.Models;

public class PhysicalDevice
{
    public string Id { get; set; } = string.Empty; // SmartThings deviceId
    public string Name { get; set; } = string.Empty;
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
    public List<string> Capabilities { get; set; } = new();
    public string IconKey { get; set; } = string.Empty;
    public bool IsPhysicalDevice { get; set; } = true;
    public bool IsOn { get; set; }
}
