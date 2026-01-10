using System.Collections.Generic;

namespace SmartHomeUI.Models;


public sealed class SmartThingsDeviceDto
{
    public string DeviceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string OcfDeviceType { get; set; } = string.Empty;
    public List<string> Capabilities { get; set; } = new();
}
