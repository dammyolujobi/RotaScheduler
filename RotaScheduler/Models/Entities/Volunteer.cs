namespace RotaScheduler.Models.Entities;

public class Volunteer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AvailabilityNotes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<VolunteerRole> VolunteerRoles { get; set; } = new List<VolunteerRole>();
    public ICollection<Unavailability> Unavailabilities { get; set; } = new List<Unavailability>();
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
