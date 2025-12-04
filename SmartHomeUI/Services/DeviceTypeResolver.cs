using System.Linq;
using SmartHomeUI.Models;

namespace SmartHomeUI.Services;

public sealed class DeviceTypeResolver : IDeviceTypeResolver
{
    public DeviceType Resolve(SmartThingsDeviceDto dto)
    {
        if (dto == null) return DeviceType.Unknown;
        var ocf = dto.OcfDeviceType?.Trim();
        if (!string.IsNullOrEmpty(ocf) && ocf.Contains("plug", System.StringComparison.OrdinalIgnoreCase))
            return DeviceType.SmartPlug;

        if (dto.Capabilities != null)
        {
            var caps = dto.Capabilities;
            bool hasSwitch = caps.Any(c => c.Equals("switch", System.StringComparison.OrdinalIgnoreCase));
            bool hasPower = caps.Any(c => c.Equals("powerMeter", System.StringComparison.OrdinalIgnoreCase));
            bool hasEnergy = caps.Any(c => c.Equals("energyMeter", System.StringComparison.OrdinalIgnoreCase));
            if (hasSwitch && (hasPower || hasEnergy))
                return DeviceType.SmartPlug;
        }

        // Name/Label heuristics
        var nameLabel = (dto.Label + " " + dto.Name).ToLowerInvariant();
        if (nameLabel.Contains("plug") || nameLabel.Contains("p110") || nameLabel.Contains("outlet"))
            return DeviceType.SmartPlug;

        return DeviceType.Unknown;
    }
}
