using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaScheduler.Data;
using RotaScheduler.Models.Entities;
using RotaScheduler.ViewModels;

namespace RotaScheduler.Controllers;

public class RolesController : Controller
{
    private readonly ApplicationDbContext _context;

    public RolesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Roles
    public async Task<IActionResult> Index()
    {
        var roles = await _context.Roles
            .Include(r => r.VolunteerRoles)
                .ThenInclude(vr => vr.Volunteer)
            .OrderBy(r => r.Name)
            .ToListAsync();
        
        return View(roles);
    }

    // GET: Roles/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var role = await _context.Roles
            .Include(r => r.VolunteerRoles)
                .ThenInclude(vr => vr.Volunteer)
            .Include(r => r.Assignments)
                .ThenInclude(a => a.Volunteer)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (role == null) return NotFound();

        return View(role);
    }

    // GET: Roles/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Roles/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RoleViewModel viewModel)
    {
        // Check for duplicate name
        if (await _context.Roles.AnyAsync(r => r.Name == viewModel.Name))
        {
            ModelState.AddModelError(nameof(viewModel.Name), "A role with this name already exists.");
        }

        if (ModelState.IsValid)
        {
            var role = new Role
            {
                Name = viewModel.Name,
                Description = viewModel.Description,
                IsActive = viewModel.IsActive
            };

            _context.Add(role);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    // GET: Roles/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var role = await _context.Roles.FindAsync(id);
        if (role == null) return NotFound();

        var viewModel = new RoleViewModel
        {
            Id = role.Id,
            Name = role.Name,
            Description = role.Description,
            IsActive = role.IsActive
        };

        return View(viewModel);
    }

    // POST: Roles/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RoleViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();

        var role = await _context.Roles.FindAsync(id);
        if (role == null) return NotFound();

        // Check for duplicate name (excluding current role)
        if (await _context.Roles.AnyAsync(r => r.Name == viewModel.Name && r.Id != id))
        {
            ModelState.AddModelError(nameof(viewModel.Name), "A role with this name already exists.");
        }

        if (ModelState.IsValid)
        {
            role.Name = viewModel.Name;
            role.Description = viewModel.Description;
            role.IsActive = viewModel.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!RoleExists(role.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    // GET: Roles/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var role = await _context.Roles
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (role == null) return NotFound();

        return View(role);
    }

    // POST: Roles/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var role = await _context.Roles.FindAsync(id);
        if (role != null)
        {
            // Soft delete by setting IsActive to false
            role.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private bool RoleExists(int id)
    {
        return _context.Roles.Any(e => e.Id == id);
    }
}
