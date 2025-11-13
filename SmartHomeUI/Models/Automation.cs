using System.ComponentModel.DataAnnotations;

namespace SmartHomeUI.Models;

public class Automation
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int UserId { get; set; }
    public int DeviceId { get; set; }
    public string TimeHHmm { get; set; } = "00:00"; // HH:mm
    public string Action { get; set; } = "Toggle"; // Toggle or SetValue
    public double Value { get; set; }
    public bool Enabled { get; set; } = true;
}
