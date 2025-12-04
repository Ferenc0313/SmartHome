using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SmartHomeUI.Models;

public class Device
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = string.Empty;

    // MDL2 glyph hex code or identifier (e.g. E80F)
    public string IconKey { get; set; } = string.Empty;


    // Extended fields
    public string Type { get; set; } = string.Empty; // e.g., Light, Plug, Thermostat
    public string Room { get; set; } = string.Empty;
    public bool IsOn { get; set; }
    public bool IsOnline { get; set; } = true;
    public int Battery { get; set; } = 100;
    public double Value { get; set; }
    public bool Favorite { get; set; }
    public bool IsPhysical { get; set; }
    public string PhysicalDeviceId { get; set; } = string.Empty;
    public DateTime? LastSeen { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(User))]
    public int UserId { get; set; }

    public User? User { get; set; }
}

