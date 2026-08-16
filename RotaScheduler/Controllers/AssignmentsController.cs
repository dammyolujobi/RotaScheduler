using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaScheduler.Data;
using RotaScheduler.Models.Entities;
using RotaScheduler.Services;
using RotaScheduler.ViewModels;

namespace RotaScheduler.Controllers;

public class AssignmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IAssignmentService _assignmentService;
    private readonly INotificationService _notificationService;

    public AssignmentsController(
        ApplicationDbContext context,
        IAssignmentService assignmentService,
        INotificationService notificationService)
    {
        _context = context;
        _assignmentService = assignmentService;
        _notificationService = notificationService;
    }

    // GET: Assignments
    public async Task<IActionResult> Index()
    {
        var assignments = await _context.Assignments
            .Include(a => a.Volunteer)
            .Include(a => a.Role)
            .Include(a => a.Service)
            .OrderByDescending(a => a.Service.ServiceDate)
            .ThenBy(a => a.Service.StartTime)
            .ToListAsync();

        return View(assignments);
    }

    // GET: Assignments/Create?serviceId=X&roleId=Y
    public async Task<IActionResult> Create(int? serviceId, int? roleId)
    {
        if (serviceId == null || roleId == null)
        {
            // Show selection view
            ViewBag.Services = await _context.Services
                .Where(s => s.IsActive && s.ServiceDate >= DateTime.Today)
                .OrderBy(s => s.ServiceDate)
                .ToListAsync();
            ViewBag.Roles = await _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .ToListAsync();
            return View("SelectServiceAndRole");
        }

        var service = await _context.Services.FindAsync(serviceId);
        var role = await _context.Roles.FindAsync(roleId);

        if (service == null || role == null) return NotFound();

        var eligibleVolunteers = await _assignmentService.GetEligibleVolunteersAsync(serviceId.Value, roleId.Value);
        var suggestedVolunteer = await _assignmentService.GetSuggestedVolunteerAsync(serviceId.Value, roleId.Value);

        var viewModel = new AssignmentViewModel
        {
            ServiceId = serviceId.Value,
            ServiceName = service.Name,
            ServiceDate = service.ServiceDate,
            StartTime = service.StartTime,
            RoleId = roleId.Value,
            RoleName = role.Name,
            SuggestedVolunteerId = suggestedVolunteer?.Id,
            EligibleVolunteers = eligibleVolunteers.Select(v =>
            {
                var lastServed = _context.Assignments
                    .Where(a => a.VolunteerId == v.Id && a.RoleId == roleId.Value)
                    .OrderByDescending(a => a.AssignedAt)
                    .Select(a => a.AssignedAt)
                    .FirstOrDefault();

                return new VolunteerSelectItem
                {
                    Id = v.Id,
                    Name = v.Name,
                    LastServedDate = lastServed == default ? null : lastServed
                };
            }).ToList()
        };

        return View(viewModel);
    }

    // POST: Assignments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(AssignmentViewModel viewModel)
    {
        if (!viewModel.VolunteerId.HasValue)
        {
            ModelState.AddModelError(nameof(viewModel.VolunteerId), "Please select a volunteer.");
        }

        if (ModelState.IsValid)
        {
            var assignment = await _assignmentService.CreateAssignmentAsync(
                viewModel.ServiceId,
                viewModel.RoleId,
                viewModel.VolunteerId.Value,
                viewModel.Notes);

            if (assignment == null)
            {
                ModelState.AddModelError("", "Unable to create assignment. The volunteer may not be eligible or is already booked for this service.");
                
                // Reload eligible volunteers for redisplay
                var service = await _context.Services.FindAsync(viewModel.ServiceId);
                var role = await _context.Roles.FindAsync(viewModel.RoleId);
                
                if (service != null && role != null)
                {
                    var eligibleVolunteers = await _assignmentService.GetEligibleVolunteersAsync(viewModel.ServiceId, viewModel.RoleId);
                    var suggestedVolunteer = await _assignmentService.GetSuggestedVolunteerAsync(viewModel.ServiceId, viewModel.RoleId);

                    viewModel.ServiceName = service.Name;
                    viewModel.ServiceDate = service.ServiceDate;
                    viewModel.StartTime = service.StartTime;
                    viewModel.RoleName = role.Name;
                    viewModel.SuggestedVolunteerId = suggestedVolunteer?.Id;
                    viewModel.EligibleVolunteers = eligibleVolunteers.Select(v =>
                        new VolunteerSelectItem
                        {
                            Id = v.Id,
                            Name = v.Name
                        }).ToList();
                    
                    return View(viewModel);
                }
                
                return RedirectToAction(nameof(Index));
            }

            // Send notification
            await _notificationService.SendAssignmentNotificationAsync(assignment);
            
            TempData["SuccessMessage"] = $"Assignment created successfully for {assignment.Volunteer.Name}.";
            return RedirectToAction(nameof(Details), new { id = assignment.Id });
        }

        return View(viewModel);
    }

    // GET: Assignments/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var assignment = await _context.Assignments
            .Include(a => a.Volunteer)
            .Include(a => a.Role)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (assignment == null) return NotFound();

        return View(assignment);
    }

    // GET: Assignments/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var assignment = await _context.Assignments
            .Include(a => a.Service)
            .Include(a => a.Role)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (assignment == null) return NotFound();

        var viewModel = new AssignmentViewModel
        {
            Id = assignment.Id,
            ServiceId = assignment.ServiceId,
            RoleId = assignment.RoleId,
            VolunteerId = assignment.VolunteerId,
            Notes = assignment.Notes,
            NotificationSent = assignment.NotificationSent
        };

        return View(viewModel);
    }

    // POST: Assignments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, AssignmentViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();

        var assignment = await _context.Assignments.FindAsync(id);
        if (assignment == null) return NotFound();

        if (ModelState.IsValid)
        {
            assignment.Notes = viewModel.Notes;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssignmentExists(assignment.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    // GET: Assignments/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var assignment = await _context.Assignments
            .Include(a => a.Volunteer)
            .Include(a => a.Role)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (assignment == null) return NotFound();

        return View(assignment);
    }

    // POST: Assignments/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _assignmentService.RemoveAssignmentAsync(id);
        TempData["SuccessMessage"] = "Assignment removed successfully.";
        return RedirectToAction(nameof(Index));
    }

    // POST: Assignments/SendNotification/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SendNotification(int id)
    {
        var assignment = await _context.Assignments
            .Include(a => a.Volunteer)
            .Include(a => a.Role)
            .Include(a => a.Service)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (assignment == null) return NotFound();

        var success = await _notificationService.SendAssignmentNotificationAsync(assignment);
        
        if (success)
        {
            assignment.NotificationSent = true;
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Notification sent successfully.";
        }
        else
        {
            TempData["ErrorMessage"] = "Failed to send notification.";
        }

        return RedirectToAction(nameof(Details), new { id = id });
    }

    private bool AssignmentExists(int id)
    {
        return _context.Assignments.Any(e => e.Id == id);
    }
}
