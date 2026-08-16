using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaScheduler.Data;
using RotaScheduler.Models.Entities;
using RotaScheduler.ViewModels;

namespace RotaScheduler.Controllers;

public class VolunteersController : Controller
{
    private readonly ApplicationDbContext _context;

    public VolunteersController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Volunteers
    public async Task<IActionResult> Index()
    {
        var volunteers = await _context.Volunteers
            .Include(v => v.VolunteerRoles)
                .ThenInclude(vr => vr.Role)
            .OrderBy(v => v.Name)
            .ToListAsync();
        
        return View(volunteers);
    }

    // GET: Volunteers/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var volunteer = await _context.Volunteers
            .Include(v => v.VolunteerRoles)
                .ThenInclude(vr => vr.Role)
            .Include(v => v.Assignments)
                .ThenInclude(a => a.Service)
            .Include(v => v.Assignments)
                .ThenInclude(a => a.Role)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (volunteer == null) return NotFound();

        return View(volunteer);
    }

    // GET: Volunteers/Create
    public IActionResult Create()
    {
        var viewModel = new VolunteerViewModel
        {
            AvailableRoles = _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .Select(r => new RoleSelectItem { Id = r.Id, Name = r.Name })
        };
        return View(viewModel);
    }

    // POST: Volunteers/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VolunteerViewModel viewModel)
    {
        // Check for duplicate email
        if (await _context.Volunteers.AnyAsync(v => v.Email == viewModel.Email))
        {
            ModelState.AddModelError(nameof(viewModel.Email), "A volunteer with this email already exists.");
        }

        // Check for duplicate phone (if provided)
        if (!string.IsNullOrEmpty(viewModel.Phone) && 
            await _context.Volunteers.AnyAsync(v => v.Phone == viewModel.Phone))
        {
            ModelState.AddModelError(nameof(viewModel.Phone), "A volunteer with this phone number already exists.");
        }

        if (ModelState.IsValid)
        {
            var volunteer = new Volunteer
            {
                Name = viewModel.Name,
                Email = viewModel.Email,
                Phone = viewModel.Phone,
                AvailabilityNotes = viewModel.AvailabilityNotes,
                IsActive = viewModel.IsActive
            };

            _context.Volunteers.Add(volunteer);
            await _context.SaveChangesAsync();

            // Add role associations
            if (viewModel.SelectedRoleIds.Any())
            {
                foreach (var roleId in viewModel.SelectedRoleIds)
                {
                    _context.VolunteerRoles.Add(new VolunteerRole
                    {
                        VolunteerId = volunteer.Id,
                        RoleId = roleId
                    });
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        viewModel.AvailableRoles = _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new RoleSelectItem { Id = r.Id, Name = r.Name });

        return View(viewModel);
    }

    // GET: Volunteers/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var volunteer = await _context.Volunteers.FindAsync(id);
        if (volunteer == null) return NotFound();

        var selectedRoleIds = await _context.VolunteerRoles
            .Where(vr => vr.VolunteerId == id)
            .Select(vr => vr.RoleId)
            .ToListAsync();

        var viewModel = new VolunteerViewModel
        {
            Id = volunteer.Id,
            Name = volunteer.Name,
            Email = volunteer.Email,
            Phone = volunteer.Phone,
            AvailabilityNotes = volunteer.AvailabilityNotes,
            IsActive = volunteer.IsActive,
            SelectedRoleIds = selectedRoleIds,
            AvailableRoles = _context.Roles
                .Where(r => r.IsActive)
                .OrderBy(r => r.Name)
                .Select(r => new RoleSelectItem 
                { 
                    Id = r.Id, 
                    Name = r.Name,
                    IsSelected = selectedRoleIds.Contains(r.Id)
                })
        };

        return View(viewModel);
    }

    // POST: Volunteers/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VolunteerViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();

        var volunteer = await _context.Volunteers.FindAsync(id);
        if (volunteer == null) return NotFound();

        // Check for duplicate email (excluding current volunteer)
        if (await _context.Volunteers.AnyAsync(v => v.Email == viewModel.Email && v.Id != id))
        {
            ModelState.AddModelError(nameof(viewModel.Email), "A volunteer with this email already exists.");
        }

        // Check for duplicate phone (excluding current volunteer)
        if (!string.IsNullOrEmpty(viewModel.Phone) && 
            await _context.Volunteers.AnyAsync(v => v.Phone == viewModel.Phone && v.Id != id))
        {
            ModelState.AddModelError(nameof(viewModel.Phone), "A volunteer with this phone number already exists.");
        }

        if (ModelState.IsValid)
        {
            volunteer.Name = viewModel.Name;
            volunteer.Email = viewModel.Email;
            volunteer.Phone = viewModel.Phone;
            volunteer.AvailabilityNotes = viewModel.AvailabilityNotes;
            volunteer.IsActive = viewModel.IsActive;

            try
            {
                // Update role associations
                var existingRoleIds = _context.VolunteerRoles
                    .Where(vr => vr.VolunteerId == id)
                    .Select(vr => vr.RoleId)
                    .ToList();

                var rolesToAdd = viewModel.SelectedRoleIds.Except(existingRoleIds);
                var rolesToRemove = existingRoleIds.Except(viewModel.SelectedRoleIds);

                foreach (var roleId in rolesToAdd)
                {
                    _context.VolunteerRoles.Add(new VolunteerRole
                    {
                        VolunteerId = id,
                        RoleId = roleId
                    });
                }

                foreach (var roleId in rolesToRemove)
                {
                    var volunteerRole = await _context.VolunteerRoles
                        .FirstOrDefaultAsync(vr => vr.VolunteerId == id && vr.RoleId == roleId);
                    if (volunteerRole != null)
                    {
                        _context.VolunteerRoles.Remove(volunteerRole);
                    }
                }

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!VolunteerExists(volunteer.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        viewModel.AvailableRoles = _context.Roles
            .Where(r => r.IsActive)
            .OrderBy(r => r.Name)
            .Select(r => new RoleSelectItem 
            { 
                Id = r.Id, 
                Name = r.Name,
                IsSelected = viewModel.SelectedRoleIds.Contains(r.Id)
            });

        return View(viewModel);
    }

    // GET: Volunteers/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var volunteer = await _context.Volunteers
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (volunteer == null) return NotFound();

        return View(volunteer);
    }

    // POST: Volunteers/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var volunteer = await _context.Volunteers.FindAsync(id);
        if (volunteer != null)
        {
            // Soft delete by setting IsActive to false
            volunteer.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool VolunteerExists(int id)
    {
        return _context.Volunteers.Any(e => e.Id == id);
    }
}
