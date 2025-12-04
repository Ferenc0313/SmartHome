using SmartHomeUI.Services;

namespace SmartHomeUI.ViewModels;

public sealed class DeviceListItem
{
    // For virtual devices (DB)
    public int? DbId { get; set; }

    // For physical SmartThings devices
    public string? PhysicalId { get; set; }
    public bool IsPhysical { get; set; }

    public string Name { get; set; } = string.Empty;
    public string IconKey { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Room { get; set; } = string.Empty;
    public bool Favorite { get; set; }
    public bool IsOn { get; set; }
    public DeviceType DeviceType { get; set; } = DeviceType.Unknown;
}
