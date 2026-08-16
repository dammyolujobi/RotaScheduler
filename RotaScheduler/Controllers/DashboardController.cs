using Microsoft.AspNetCore.Mvc;
using RotaScheduler.Services;

namespace RotaScheduler.Controllers;

public class DashboardController : Controller
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    // GET: Dashboard
    public async Task<IActionResult> Index()
    {
        var viewModel = await _dashboardService.GetDashboardDataAsync();
        return View(viewModel);
    }
}
