using Microsoft.EntityFrameworkCore;
using RotaScheduler.Data;
using RotaScheduler.Models.Entities;

namespace RotaScheduler.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardDataAsync();
}

public class DashboardService : IDashboardService
{
    private readonly ApplicationDbContext _context;

    public DashboardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardViewModel> GetDashboardDataAsync()
    {
        var now = DateTime.UtcNow;
        var upcomingDays = 30;

        // Get upcoming services
        var upcomingServices = await _context.Services
            .Where(s => s.ServiceDate >= DateTime.Today && 
                       s.ServiceDate <= DateTime.Today.AddDays(upcomingDays) &&
                       s.IsActive)
            .OrderBy(s => s.ServiceDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        var serviceIds = upcomingServices.Select(s => s.Id).ToList();

        // Get all assignments for these services
        var assignments = await _context.Assignments
            .Include(a => a.Volunteer)
            .Include(a => a.Role)
            .Where(a => serviceIds.Contains(a.ServiceId))
            .ToListAsync();

        // Get all roles
        var allRoles = await _context.Roles.Where(r => r.IsActive).ToListAsync();

        // Calculate unfilled slots per service
        var unfilledSlots = new List<UnfilledSlotViewModel>();
        foreach (var service in upcomingServices)
        {
            var assignedRoles = assignments
                .Where(a => a.ServiceId == service.Id)
                .Select(a => a.RoleId)
                .ToHashSet();

            foreach (var role in allRoles)
            {
                if (!assignedRoles.Contains(role.Id))
                {
                    unfilledSlots.Add(new UnfilledSlotViewModel
                    {
                        ServiceId = service.Id,
                        ServiceName = service.Name,
                        ServiceDate = service.ServiceDate,
                        StartTime = service.StartTime,
                        RoleId = role.Id,
                        RoleName = role.Name
                    });
                }
            }
        }

        // Find volunteers overdue for rotation (not served in last 60 days for any role they're eligible for)
        var cutoffDate = now.AddDays(-60);
        var activeVolunteers = await _context.Volunteers
            .Where(v => v.IsActive)
            .Include(v => v.VolunteerRoles)
            .ToListAsync();

        var volunteerLastServed = await _context.Assignments
            .Where(a => a.AssignedAt >= cutoffDate)
            .GroupBy(a => a.VolunteerId)
            .Select(g => new
            {
                VolunteerId = g.Key,
                LastServedDate = g.Max(a => a.AssignedAt)
            })
            .ToDictionaryAsync(x => x.VolunteerId, x => x.LastServedDate);

        var overdueVolunteers = new List<OverdueVolunteerViewModel>();
        foreach (var volunteer in activeVolunteers)
        {
            if (!volunteerLastServed.ContainsKey(volunteer.Id))
            {
                // Never served or not served in lookback window
                var roleNames = volunteer.VolunteerRoles.Select(vr => vr.Role.Name).ToList();
                if (roleNames.Any())
                {
                    overdueVolunteers.Add(new OverdueVolunteerViewModel
                    {
                        VolunteerId = volunteer.Id,
                        VolunteerName = volunteer.Name,
                        Email = volunteer.Email,
                        Roles = string.Join(", ", roleNames),
                        DaysSinceLastServed = null
                    });
                }
            }
            else
            {
                var daysSince = (now - volunteerLastServed[volunteer.Id]).Days;
                if (daysSince > 60)
                {
                    var roleNames = volunteer.VolunteerRoles.Select(vr => vr.Role.Name).ToList();
                    if (roleNames.Any())
                    {
                        overdueVolunteers.Add(new OverdueVolunteerViewModel
                        {
                            VolunteerId = volunteer.Id,
                            VolunteerName = volunteer.Name,
                            Email = volunteer.Email,
                            Roles = string.Join(", ", roleNames),
                            DaysSinceLastServed = daysSince
                        });
                    }
                }
            }
        }

        return new DashboardViewModel
        {
            UpcomingServicesCount = upcomingServices.Count,
            UnfilledSlots = unfilledSlots.OrderByDescending(u => u.ServiceDate).Take(20).ToList(),
            OverdueVolunteers = overdueVolunteers.OrderBy(v => v.DaysSinceLastServed ?? 999).ToList(),
            TotalVolunteers = activeVolunteers.Count,
            TotalAssignments = assignments.Count
        };
    }
}

public class DashboardViewModel
{
    public int UpcomingServicesCount { get; set; }
    public List<UnfilledSlotViewModel> UnfilledSlots { get; set; } = new();
    public List<OverdueVolunteerViewModel> OverdueVolunteers { get; set; } = new();
    public int TotalVolunteers { get; set; }
    public int TotalAssignments { get; set; }
}

public class UnfilledSlotViewModel
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public DateTime ServiceDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public int RoleId { get; set; }
    public string RoleName { get; set; } = string.Empty;
}

public class OverdueVolunteerViewModel
{
    public int VolunteerId { get; set; }
    public string VolunteerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Roles { get; set; } = string.Empty;
    public int? DaysSinceLastServed { get; set; }
}
