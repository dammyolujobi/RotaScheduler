using System.ComponentModel.DataAnnotations;

namespace RotaScheduler.ViewModels;

public class VolunteerViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    [StringLength(256, ErrorMessage = "Email cannot exceed 256 characters")]
    public string Email { get; set; } = string.Empty;
    
    [Phone(ErrorMessage = "Invalid phone format")]
    [StringLength(20, ErrorMessage = "Phone cannot exceed 20 characters")]
    public string Phone { get; set; } = string.Empty;
    
    public string? AvailabilityNotes { get; set; }
    public bool IsActive { get; set; } = true;
    
    public List<int> SelectedRoleIds { get; set; } = new();
    public IEnumerable<RoleSelectItem> AvailableRoles { get; set; } = new List<RoleSelectItem>();
}

public class RoleSelectItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSelected { get; set; }
}

public class RoleViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
    public string? Description { get; set; }
    
    public bool IsActive { get; set; } = true;
}

public class ServiceViewModel
{
    public int Id { get; set; }
    
    [Required(ErrorMessage = "Name is required")]
    [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
    public string Name { get; set; } = string.Empty;
    
    [Required(ErrorMessage = "Date is required")]
    [Display(Name = "Service Date")]
    public DateTime ServiceDate { get; set; } = DateTime.Today;
    
    [Required(ErrorMessage = "Start time is required")]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; } = new TimeSpan(10, 0, 0);
    
    [Required(ErrorMessage = "End time is required")]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; } = new TimeSpan(11, 30, 0);
    
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;
}

public class GenerateServicesViewModel
{
    [Required(ErrorMessage = "Service name is required")]
    public string ServiceName { get; set; } = "Sunday Morning Service";
    
    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "Start Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;
    
    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "End Date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(28);
    
    [Required(ErrorMessage = "Start time is required")]
    [Display(Name = "Start Time")]
    public TimeSpan StartTime { get; set; } = new TimeSpan(10, 0, 0);
    
    [Required(ErrorMessage = "End time is required")]
    [Display(Name = "End Time")]
    public TimeSpan EndTime { get; set; } = new TimeSpan(11, 30, 0);
    
    [Display(Name = "Day of Week")]
    public DayOfWeek DayOfWeek { get; set; } = DayOfWeek.Sunday;
}

public class AssignmentViewModel
{
    public int Id { get; set; }
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public TimeSpan StartTime { get; set; }
    
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    
    public int? VolunteerId { get; set; }
    public string? VolunteerName { get; set; }
    
    public string? Notes { get; set; }
    public bool NotificationSent { get; set; }
    
    // For creating new assignments
    public IEnumerable<VolunteerSelectItem> EligibleVolunteers { get; set; } = new List<VolunteerSelectItem>();
    public int? SuggestedVolunteerId { get; set; }
}

public class VolunteerSelectItem
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int AssignmentCount { get; set; }
    public DateTime? LastServedDate { get; set; }
}

public class UnavailabilityViewModel
{
    public int Id { get; set; }
    
    public int VolunteerId { get; set; }
    public string? VolunteerName { get; set; }
    
    [Required(ErrorMessage = "Start date is required")]
    [Display(Name = "From Date")]
    public DateTime StartDate { get; set; } = DateTime.Today;
    
    [Required(ErrorMessage = "End date is required")]
    [Display(Name = "To Date")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);
    
    public string? Reason { get; set; }
}

public class CalendarViewModel
{
    public DateTime CurrentDate { get; set; }
    public string ViewType { get; set; } = "monthly"; // weekly or monthly
    public List<CalendarSlotViewModel> Slots { get; set; } = new();
}

public class CalendarSlotViewModel
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    
    public List<CalendarAssignmentViewModel> Assignments { get; set; } = new();
    public List<string> MissingRoles { get; set; } = new();
    public bool HasUnfilledRoles => MissingRoles.Any();
}

public class CalendarAssignmentViewModel
{
    public int AssignmentId { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
    public int VolunteerId { get; set; }
    public string VolunteerName { get; set; } = string.Empty;
}
