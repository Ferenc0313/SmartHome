using Microsoft.EntityFrameworkCore;
using SmartHomeUI.Models;

namespace SmartHomeUI.Data;

public class SmartHomeDbContext : DbContext
{
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Device> Devices { get; set; } = null!;
    public DbSet<SmartHomeUI.Models.Automation> Automations { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder options)
        => options.UseSqlite("Data Source=smarthome.db");

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>()
            .HasMany(u => u.Devices)
            .WithOne(d => d.User!)
            .HasForeignKey(d => d.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}



