using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RotaScheduler.Data;
using RotaScheduler.Models.Entities;
using RotaScheduler.Services;
using RotaScheduler.ViewModels;

namespace RotaScheduler.Controllers;

public class ServicesController : Controller
{
    private readonly ApplicationDbContext _context;

    public ServicesController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Services
    public async Task<IActionResult> Index()
    {
        var services = await _context.Services
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Volunteer)
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Role)
            .OrderByDescending(s => s.ServiceDate)
            .ThenBy(s => s.StartTime)
            .ToListAsync();
        
        return View(services);
    }

    // GET: Services/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null) return NotFound();

        var service = await _context.Services
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Volunteer)
            .Include(s => s.Assignments)
                .ThenInclude(a => a.Role)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (service == null) return NotFound();

        return View(service);
    }

    // GET: Services/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Services/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ServiceViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            var service = new Service
            {
                Name = viewModel.Name,
                ServiceDate = viewModel.ServiceDate.Date,
                StartTime = viewModel.StartTime,
                EndTime = viewModel.EndTime,
                Notes = viewModel.Notes,
                IsActive = viewModel.IsActive
            };

            _context.Add(service);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    // GET: Services/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null) return NotFound();

        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        var viewModel = new ServiceViewModel
        {
            Id = service.Id,
            Name = service.Name,
            ServiceDate = service.ServiceDate,
            StartTime = service.StartTime,
            EndTime = service.EndTime,
            Notes = service.Notes,
            IsActive = service.IsActive
        };

        return View(viewModel);
    }

    // POST: Services/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ServiceViewModel viewModel)
    {
        if (id != viewModel.Id) return NotFound();

        var service = await _context.Services.FindAsync(id);
        if (service == null) return NotFound();

        if (ModelState.IsValid)
        {
            service.Name = viewModel.Name;
            service.ServiceDate = viewModel.ServiceDate.Date;
            service.StartTime = viewModel.StartTime;
            service.EndTime = viewModel.EndTime;
            service.Notes = viewModel.Notes;
            service.IsActive = viewModel.IsActive;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!ServiceExists(service.Id))
                {
                    return NotFound();
                }
                throw;
            }
            return RedirectToAction(nameof(Index));
        }

        return View(viewModel);
    }

    // GET: Services/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null) return NotFound();

        var service = await _context.Services
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (service == null) return NotFound();

        return View(service);
    }

    // POST: Services/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var service = await _context.Services.FindAsync(id);
        if (service != null)
        {
            // Soft delete by setting IsActive to false
            service.IsActive = false;
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    // GET: Services/Generate
    public IActionResult Generate()
    {
        var viewModel = new GenerateServicesViewModel
        {
            StartDate = GetNextSunday(),
            EndDate = GetNextSunday().AddDays(21),
            ServiceName = "Sunday Morning Service",
            StartTime = new TimeSpan(10, 0, 0),
            EndTime = new TimeSpan(11, 30, 0),
            DayOfWeek = DayOfWeek.Sunday
        };
        return View(viewModel);
    }

    // POST: Services/Generate
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(GenerateServicesViewModel viewModel)
    {
        if (ModelState.IsValid)
        {
            if (viewModel.EndDate < viewModel.StartDate)
            {
                ModelState.AddModelError("", "End date must be after start date.");
                return View(viewModel);
            }

            var services = new List<Service>();
            var currentDate = viewModel.StartDate;

            while (currentDate <= viewModel.EndDate)
            {
                if (currentDate.DayOfWeek == viewModel.DayOfWeek)
                {
                    services.Add(new Service
                    {
                        Name = viewModel.ServiceName,
                        ServiceDate = currentDate.Date,
                        StartTime = viewModel.StartTime,
                        EndTime = viewModel.EndTime,
                        IsActive = true
                    });
                }
                currentDate = currentDate.AddDays(1);
            }

            if (services.Any())
            {
                _context.Services.AddRange(services);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Generated {services.Count} services successfully.";
                return RedirectToAction(nameof(Index));
            }

            ModelState.AddModelError("", "No services found matching the specified criteria.");
        }

        return View(viewModel);
    }

    private bool ServiceExists(int id)
    {
        return _context.Services.Any(e => e.Id == id);
    }

    private static DateTime GetNextSunday()
    {
        var today = DateTime.Today;
        int daysUntilSunday = (7 - (int)today.DayOfWeek) % 7;
        if (daysUntilSunday == 0) daysUntilSunday = 7;
        return today.AddDays(daysUntilSunday);
    }
}
