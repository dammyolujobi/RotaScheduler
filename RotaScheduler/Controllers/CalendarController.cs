using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaScheduler.Data;
using RotaScheduler.Models.Entities;
using RotaScheduler.Services;
using RotaScheduler.ViewModels;

namespace RotaScheduler.Controllers;

public class CalendarController : Controller
{
    private readonly ApplicationDbContext _context;

    public CalendarController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Calendar
    public async Task<IActionResult> Index(int? year, int? month, string viewType = "monthly")
    {
        var currentDate = DateTime.Today;
        
        if (year.HasValue && month.HasValue)
        {
            try
            {
                currentDate = new DateTime(year.Value, month.Value, 1);
            }
            catch
            {
                // Invalid date, use today
            }
        }

        var viewModel = new CalendarViewModel
        {
            CurrentDate = currentDate,
            ViewType = viewType.ToLower()
        };

        // Determine date range based on view type
        DateTime startDate, endDate;
        if (viewType.ToLower() == "weekly")
        {
            // Get start of week (Sunday)
            var dayOfWeek = (int)currentDate.DayOfWeek;
            startDate = currentDate.AddDays(-dayOfWeek);
            endDate = startDate.AddDays(6);
        }
        else // monthly
        {
            startDate = new DateTime(currentDate.Year, currentDate.Month, 1);
            endDate = startDate.AddMonths(1).AddDays(-1);
        }

        // Get services in range
        var services = await _context.Services
            .Where(s => s.ServiceDate >= startDate.Date && 
                       s.ServiceDate <= endDate.Date &&
                       s.IsActive)
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Volunteer)
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Role)
            .OrderBy(s => s.ServiceDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();

        // Get all active roles
        var allRoles = await _context.Roles
            .Where(r => r.IsActive)
            .ToListAsync();

        // Build calendar slots
        foreach (var service in services)
        {
            var slot = new CalendarSlotViewModel
            {
                ServiceId = service.Id,
                ServiceName = service.Name,
                ServiceDate = service.ServiceDate,
                StartTime = service.StartTime,
                EndTime = service.EndTime,
                Assignments = service.Assignments.Select(a => new CalendarAssignmentViewModel
                {
                    AssignmentId = a.Id,
                    RoleId = a.RoleId,
                    RoleName = a.Role.Name,
                    VolunteerId = a.VolunteerId,
                    VolunteerName = a.Volunteer.Name
                }).ToList()
            };

            // Find missing roles
            var assignedRoleIds = service.Assignments.Select(a => a.RoleId).ToHashSet();
            slot.MissingRoles = allRoles
                .Where(r => !assignedRoleIds.Contains(r.Id))
                .Select(r => r.Name)
                .ToList();

            viewModel.Slots.Add(slot);
        }

        return View(viewModel);
    }

    // GET: Calendar/Weekly
    public async Task<IActionResult> Weekly(int? year, int? month)
    {
        return await Index(year, month, "weekly");
    }

    // GET: Calendar/Monthly
    public async Task<IActionResult> Monthly(int? year, int? month)
    {
        return await Index(year, month, "monthly");
    }
}
