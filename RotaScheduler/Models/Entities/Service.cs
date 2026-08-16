namespace RotaScheduler.Models.Entities;

public class Service
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
