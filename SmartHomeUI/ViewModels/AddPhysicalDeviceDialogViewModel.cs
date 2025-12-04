using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SmartHomeUI.Models;
using SmartHomeUI.Services;

namespace SmartHomeUI.ViewModels;

public class AddPhysicalDeviceDialogViewModel
{
    public ObservableCollection<SelectableDevice> Devices { get; } = new();

    public void LoadDevices(IEnumerable<SmartThingsDeviceDto> dtos, IDeviceTypeResolver typeResolver, IDeviceIconResolver iconResolver)
    {
        Devices.Clear();
        foreach (var dto in dtos ?? Enumerable.Empty<SmartThingsDeviceDto>())
        {
            var type = typeResolver.Resolve(dto);
            var icon = iconResolver.ResolveIcon(type);
            Devices.Add(new SelectableDevice
            {
                Device = new PhysicalDevice
                {
                    Id = dto.DeviceId,
                    Name = string.IsNullOrWhiteSpace(dto.Label) ? dto.Name : dto.Label,
                    DeviceType = type,
                    Capabilities = dto.Capabilities ?? new List<string>(),
                    IconKey = icon,
                    IsPhysicalDevice = true
                }
            });
        }
    }

    public IReadOnlyList<PhysicalDevice> GetSelectedDevices() =>
        Devices.Where(d => d.IsSelected).Select(d => d.Device).ToList();
}

public sealed class SelectableDevice
{
    public PhysicalDevice Device { get; set; } = new PhysicalDevice();
    public bool IsSelected { get; set; }
}
