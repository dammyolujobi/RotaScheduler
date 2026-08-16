using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaScheduler.Data;
using RotaScheduler.Models.Entities;
using RotaScheduler.ViewModels;

namespace RotaScheduler.Controllers;

public class UnavailabilitiesController : Controller
{
    private readonly ApplicationDbContext _context;

    public UnavailabilitiesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Unavailabilities
    public async Task<IActionResult> Index()
    {
        var unavailabilities = await _context.Unavailabilities
            .Include(u => u.Volunteer)
            .OrderByDescending(u => u.CreatedAt)
            .ToListAsync();

        return View(unavailabilities);
    }

    // GET: Unavailabilities/Create
    public IActionResult Create()
    {
        ViewBag.Volunteers = _context.Volunteers
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .ToList();
        
        return View();
    }

    // POST: Unavailabilities/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UnavailabilityViewModel viewModel)
    {
        if (viewModel.EndDate < viewModel.StartDate)
        {
            ModelState.AddModelError(nameof(viewModel.EndDate), "End date must be after start date.");
        }

        if (ModelState.IsValid)
        {
            var unavailability = new Unavailability
            {
                VolunteerId = viewModel.VolunteerId,
                StartDate = viewModel.StartDate.Date,
                EndDate = viewModel.EndDate.Date,
                Reason = viewModel.Reason
            };

            _context.Unavailabilities.Add(unavailability);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Unavailability period added successfully.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Volunteers = _context.Volunteers
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .ToList();

        return View(viewModel);
    }

    // GET: Unavailabilities/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var unavailability = await _context.Unavailabilities.FindAsync(id);
        if (unavailability == null) return NotFound();

        var volunteer = await _context.Volunteers.FindAsync(unavailability.VolunteerId);

        var viewModel = new UnavailabilityViewModel
        {
            Id = unavailability.Id,
            VolunteerId = unavailability.VolunteerId,
            VolunteerName = volunteer?.Name,
            StartDate = unavailability.StartDate,
            EndDate = unavailability.EndDate,
            Reason = unavailability.Reason
        };

        ViewBag.Volunteers = _context.Volunteers
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .ToList();

        return View(viewModel);
    }

    // POST: Unavailabilities/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, UnavailabilityViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();

        var unavailability = await _context.Unavailabilities.FindAsync(id);
        if (unavailability == null) return NotFound();

        if (viewModel.EndDate < viewModel.StartDate)
        {
            ModelState.AddModelError(nameof(viewModel.EndDate), "End date must be after start date.");
        }

        if (ModelState.IsValid)
        {
            unavailability.StartDate = viewModel.StartDate.Date;
            unavailability.EndDate = viewModel.EndDate.Date;
            unavailability.Reason = viewModel.Reason;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UnavailabilityExists(unavailability.Id))
                {
                    return NotFound();
                }
                throw;
            }

            TempData["SuccessMessage"] = "Unavailability period updated successfully.";
            return RedirectToAction(nameof(Index));
        }

        ViewBag.Volunteers = _context.Volunteers
            .Where(v => v.IsActive)
            .OrderBy(v => v.Name)
            .ToList();

        return View(viewModel);
    }

    // GET: Unavailabilities/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var unavailability = await _context.Unavailabilities
            .Include(u => u.Volunteer)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (unavailability == null) return NotFound();

        return View(unavailability);
    }

    // POST: Unavailabilities/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var unavailability = await _context.Unavailabilities.FindAsync(id);
        if (unavailability != null)
        {
            _context.Unavailabilities.Remove(unavailability);
            await _context.SaveChangesAsync();
        }

        TempData["SuccessMessage"] = "Unavailability period removed successfully.";
        return RedirectToAction(nameof(Index));
    }

    private bool UnavailabilityExists(int id)
    {
        return _context.Unavailabilities.Any(e => e.Id == id);
    }
}
