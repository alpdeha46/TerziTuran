using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TerziTuran.Web.Services;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin")]
public class ReportsController(IReportService reportService, IPdfService pdfService) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index([FromQuery] ReportsFilterViewModel filter)
    {
        filter = await reportService.BuildAsync(filter);
        return View(filter);
    }

    [HttpGet]
    public IActionResult Reset() => RedirectToAction(nameof(Index));

    [HttpGet]
    public async Task<IActionResult> DownloadPdf([FromQuery] ReportsFilterViewModel filter)
    {
        filter = await reportService.BuildAsync(filter);
        var bytes = pdfService.GenerateReportPdf(filter);
        return File(bytes, "application/pdf", $"TerziTuran-Rapor-{DateTime.Now:yyyyMMddHHmm}.pdf");
    }
}
