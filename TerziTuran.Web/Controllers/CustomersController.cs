using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Models;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin")]
public class CustomersController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? search)
    {
        var query = context.Customers
            .Include(x => x.Measurements)
            .Include(x => x.Orders)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x => x.FullName.Contains(search) || x.Phone.Contains(search));
        }

        ViewBag.Search = search;
        return View(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var customer = await context.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        var model = new CustomerDetailsViewModel
        {
            Customer = customer,
            Measurements = await context.Measurements.Where(x => x.CustomerId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(),
            Orders = await context.Orders.Where(x => x.CustomerId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(),
            Payments = await context.Payments.Include(x => x.Order).Where(x => x.Order!.CustomerId == id).OrderByDescending(x => x.PaymentDate).ToListAsync(),
            Appointments = await context.Appointments.Where(x => x.CustomerId == id).OrderBy(x => x.AppointmentDate).ToListAsync()
        };
        return View(model);
    }

    public IActionResult Create() => View(new Customer());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Customer model)
    {
        if (!ModelState.IsValid) return View(model);
        context.Customers.Add(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Musteri kaydi olusturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var customer = await context.Customers.FindAsync(id);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Customer model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid) return View(model);

        context.Update(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Musteri bilgileri guncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var customer = await context.Customers.FindAsync(id);
        return customer is null ? NotFound() : View(customer);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var customer = await context.Customers.FindAsync(id);
        if (customer is null) return NotFound();

        context.Customers.Remove(customer);
        await context.SaveChangesAsync();
        TempData["Success"] = "Musteri kaydi silindi.";
        return RedirectToAction(nameof(Index));
    }
}
