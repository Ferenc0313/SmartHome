using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SmartHomeUI.Models;

public class User
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool IsOnline { get; set; }
    public string? SmartThingsPatEncrypted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginCount { get; set; }
    public DateTime? LockedUntil { get; set; }

    public ICollection<Device> Devices { get; set; } = new List<Device>();

    // Authentication fields (PBKDF2)
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
}
