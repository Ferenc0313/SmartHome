namespace SmartHomeUI.Domain.Devices;

public sealed class SprinklerValve
{
    public bool IsOpen { get; private set; }

    public void Open() => IsOpen = true;
    public void Close() => IsOpen = false;
}
