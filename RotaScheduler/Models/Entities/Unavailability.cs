namespace RotaScheduler.Models.Entities;

public class Unavailability
{
    public int Id { get; set; }
    public int VolunteerId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Volunteer Volunteer { get; set; } = null!;
}
