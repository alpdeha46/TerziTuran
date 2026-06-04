using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin")]
public class AppointmentsController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index()
        => View(await context.Appointments.Include(x => x.Customer).Include(x => x.Order).OrderBy(x => x.AppointmentDate).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var appointment = await context.Appointments.Include(x => x.Customer).Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == id);
        return appointment is null ? NotFound() : View(appointment);
    }

    public async Task<IActionResult> Create()
    {
        await LoadLookupsAsync();
        return View(new Appointment { AppointmentDate = DateTime.Today.AddHours(10) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Appointment model)
    {
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }
        context.Appointments.Add(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Randevu olusturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var appointment = await context.Appointments.FindAsync(id);
        if (appointment is null) return NotFound();
        await LoadLookupsAsync();
        return View(appointment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Appointment model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadLookupsAsync();
            return View(model);
        }
        context.Update(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Randevu guncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var appointment = await context.Appointments.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        return appointment is null ? NotFound() : View(appointment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appointment = await context.Appointments.FindAsync(id);
        if (appointment is null) return NotFound();
        context.Appointments.Remove(appointment);
        await context.SaveChangesAsync();
        TempData["Success"] = "Randevu silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadLookupsAsync()
    {
        ViewBag.Customers = new SelectList(await context.Customers.OrderBy(x => x.FullName).ToListAsync(), "Id", "FullName");
        ViewBag.Orders = new SelectList(await context.Orders.OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, Text = x.Title }).ToListAsync(), "Id", "Text");
    }
}
