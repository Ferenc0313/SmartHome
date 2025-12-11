namespace SmartHomeUI.Domain.Devices;

public class SmartDoor
{
    public bool IsOpen { get; private set; }
    public bool IsLocked { get; private set; } = true;
    public DoorMode Mode { get; private set; } = DoorMode.Normal;

    public void OpenDoor()
    {
        IsOpen = true;
    }

    public void CloseDoor()
    {
        IsOpen = false;
    }

    public void Lock()
    {
        IsLocked = true;
    }

    public void Unlock()
    {
        IsLocked = false;
    }

    public void SetMode(DoorMode mode)
    {
        Mode = mode;
    }
}
