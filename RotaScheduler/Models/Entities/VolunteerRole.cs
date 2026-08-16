namespace RotaScheduler.Models.Entities;

public class VolunteerRole
{
    public int Id { get; set; }
    public int VolunteerId { get; set; }
    public int RoleId { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Volunteer Volunteer { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
