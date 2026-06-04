using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TerziTuran.Web.Models;
using TerziTuran.Web.Services;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin")]
public class DashboardController(IDashboardService dashboardService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(DateTime? startDate, DateTime? endDate, OrderStatus? status, string? category)
    {
        var model = await dashboardService.GetDashboardAsync(startDate, endDate, status, category);
        ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        ViewBag.Status = status;
        ViewBag.Category = category;
        return View(model);
    }
}
