using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin")]
public class MeasurementsController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index()
        => View(await context.Measurements.Include(x => x.Customer).OrderByDescending(x => x.CreatedAt).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var measurement = await context.Measurements.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        return measurement is null ? NotFound() : View(measurement);
    }

    public async Task<IActionResult> Create()
    {
        await LoadCustomersAsync();
        return View(new Measurement());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Measurement model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomersAsync();
            return View(model);
        }

        context.Measurements.Add(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Olcu kaydi eklendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var measurement = await context.Measurements.FindAsync(id);
        if (measurement is null) return NotFound();
        await LoadCustomersAsync();
        return View(measurement);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Measurement model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadCustomersAsync();
            return View(model);
        }

        context.Update(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Olcu kaydi guncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var measurement = await context.Measurements.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        return measurement is null ? NotFound() : View(measurement);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var measurement = await context.Measurements.FindAsync(id);
        if (measurement is null) return NotFound();
        context.Measurements.Remove(measurement);
        await context.SaveChangesAsync();
        TempData["Success"] = "Olcu kaydi silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadCustomersAsync()
        => ViewBag.Customers = new SelectList(await context.Customers.OrderBy(x => x.FullName).ToListAsync(), "Id", "FullName");
}
