namespace RotaScheduler.Models.Entities;

public class Role
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<VolunteerRole> VolunteerRoles { get; set; } = new List<VolunteerRole>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
