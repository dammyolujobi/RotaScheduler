using Microsoft.EntityFrameworkCore;
using RotaScheduler.Data;
using RotaScheduler.Models.Entities;

namespace RotaScheduler.Services;

public interface IAssignmentService
{
    Task<List<Volunteer>> GetEligibleVolunteersAsync(int serviceId, int roleId);
    Task<Volunteer?> GetSuggestedVolunteerAsync(int serviceId, int roleId);
    Task<Assignment?> CreateAssignmentAsync(int serviceId, int roleId, int volunteerId, string? notes = null);
    Task<bool> IsVolunteerAvailableAsync(int volunteerId, DateTime startDate, DateTime endDate);
    Task<bool> IsVolunteerDoubleBookedAsync(int volunteerId, int serviceId);
    Task RemoveAssignmentAsync(int assignmentId);
}

public class AssignmentService : IAssignmentService
{
    private readonly ApplicationDbContext _context;

    public AssignmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Gets volunteers eligible for a specific role in a service.
    /// Filters by: role eligibility, active status, and availability (no unavailability conflicts).
    /// </summary>
    public async Task<List<Volunteer>> GetEligibleVolunteersAsync(int serviceId, int roleId)
    {
        var service = await _context.Services.FindAsync(serviceId);
        if (service == null || !service.IsActive)
            return new List<Volunteer>();

        var serviceStart = service.ServiceDate.Add(service.StartTime);
        var serviceEnd = service.ServiceDate.Add(service.EndTime);

        // Get volunteers who have this role and are active
        var eligibleVolunteerIds = _context.VolunteerRoles
            .Where(vr => vr.RoleId == roleId)
            .Join(_context.Volunteers.Where(v => v.IsActive),
                vr => vr.VolunteerId,
                v => v.Id,
                (vr, v) => v.Id)
            .ToList();

        var eligibleVolunteers = new List<Volunteer>();

        foreach (var volId in eligibleVolunteerIds)
        {
            // Check unavailability
            var hasUnavailability = _context.Unavailabilities
                .Any(u => u.VolunteerId == volId &&
                         u.StartDate <= serviceEnd &&
                         u.EndDate >= serviceStart);

            if (!hasUnavailability)
            {
                var volunteer = await _context.Volunteers.FindAsync(volId);
                if (volunteer != null)
                    eligibleVolunteers.Add(volunteer);
            }
        }

        return eligibleVolunteers;
    }

    /// <summary>
    /// Uses fairness rotation algorithm to suggest the best volunteer.
    /// Ranks by least-recently-served per role, considering only assignments in the lookback window.
    /// </summary>
    public async Task<Volunteer?> GetSuggestedVolunteerAsync(int serviceId, int roleId)
    {
        var eligibleVolunteers = await GetEligibleVolunteersAsync(serviceId, roleId);
        if (!eligibleVolunteers.Any())
            return null;

        var service = await _context.Services.FindAsync(serviceId);
        if (service == null) return null;

        var serviceStart = service.ServiceDate.Add(service.StartTime);

        // Lookback window: 90 days
        var lookbackDate = serviceStart.AddDays(-90);

        // Get last assignment date per volunteer for this role within lookback window
        var volunteerLastServed = _context.Assignments
            .Where(a => a.RoleId == roleId && a.AssignedAt >= lookbackDate)
            .GroupBy(a => a.VolunteerId)
            .Select(g => new
            {
                VolunteerId = g.Key,
                LastServedDate = g.Max(a => a.AssignedAt)
            })
            .ToDictionary(x => x.VolunteerId, x => x.LastServedDate);

        // Rank volunteers: those never served first, then by oldest last-served date
        var rankedVolunteers = eligibleVolunteers
            .OrderByDescending(v =>
            {
                if (!volunteerLastServed.ContainsKey(v.Id))
                    return DateTime.MinValue; // Never served - highest priority
                
                return volunteerLastServed[v.Id]; // Earlier dates = higher priority
            })
            .ThenBy(v => _context.Assignments.Count(a => a.VolunteerId == v.Id && a.RoleId == roleId))
            .ToList();

        return rankedVolunteers.FirstOrDefault();
    }

    /// <summary>
    /// Creates an assignment after validating all business rules.
    /// </summary>
    public async Task<Assignment?> CreateAssignmentAsync(int serviceId, int roleId, int volunteerId, string? notes = null)
    {
        var service = await _context.Services.FindAsync(serviceId);
        var role = await _context.Roles.FindAsync(roleId);
        var volunteer = await _context.Volunteers.FindAsync(volunteerId);

        if (service == null || role == null || volunteer == null)
            return null;

        // Validate volunteer has this role
        var hasRole = _context.VolunteerRoles
            .Any(vr => vr.VolunteerId == volunteerId && vr.RoleId == roleId);

        if (!hasRole)
            return null;

        // Check for double-booking
        var isDoubleBooked = await IsVolunteerDoubleBookedAsync(volunteerId, serviceId);
        if (isDoubleBooked)
            return null;

        // Check availability
        var serviceStart = service.ServiceDate.Add(service.StartTime);
        var serviceEnd = service.ServiceDate.Add(service.EndTime);
        var isAvailable = await IsVolunteerAvailableAsync(volunteerId, serviceStart, serviceEnd);
        if (!isAvailable)
            return null;

        var assignment = new Assignment
        {
            VolunteerId = volunteerId,
            RoleId = roleId,
            ServiceId = serviceId,
            Notes = notes,
            AssignedAt = DateTime.UtcNow
        };

        _context.Assignments.Add(assignment);
        await _context.SaveChangesAsync();

        return assignment;
    }

    /// <summary>
    /// Checks if volunteer is available during the specified time range.
    /// </summary>
    public async Task<bool> IsVolunteerAvailableAsync(int volunteerId, DateTime startDate, DateTime endDate)
    {
        // Check for unavailability conflicts
        var hasConflict = _context.Unavailabilities
            .Any(u => u.VolunteerId == volunteerId &&
                     u.StartDate <= endDate &&
                     u.EndDate >= startDate);

        if (hasConflict)
            return false;

        return true;
    }

    /// <summary>
    /// Checks if volunteer is already assigned to another role in the same service.
    /// </summary>
    public async Task<bool> IsVolunteerDoubleBookedAsync(int volunteerId, int serviceId)
    {
        var isAssigned = _context.Assignments
            .Any(a => a.VolunteerId == volunteerId && a.ServiceId == serviceId);

        return isAssigned;
    }

    /// <summary>
    /// Removes an assignment.
    /// </summary>
    public async Task RemoveAssignmentAsync(int assignmentId)
    {
        var assignment = await _context.Assignments.FindAsync(assignmentId);
        if (assignment != null)
        {
            _context.Assignments.Remove(assignment);
            await _context.SaveChangesAsync();
        }
    }
}
