namespace RotaScheduler.Models.Entities;

public class Assignment
{
    public int Id { get; set; }
    public int VolunteerId { get; set; }
    public int RoleId { get; set; }
    public int ServiceId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public bool NotificationSent { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Volunteer Volunteer { get; set; } = null!;
    public Role Role { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
