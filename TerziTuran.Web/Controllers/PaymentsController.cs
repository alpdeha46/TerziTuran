using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin")]
public class PaymentsController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index()
        => View(await context.Payments.Include(x => x.Order).ThenInclude(x => x!.Customer).OrderByDescending(x => x.PaymentDate).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var payment = await context.Payments.Include(x => x.Order).ThenInclude(x => x!.Customer).FirstOrDefaultAsync(x => x.Id == id);
        return payment is null ? NotFound() : View(payment);
    }

    public async Task<IActionResult> Create()
    {
        await LoadOrdersAsync();
        return View(new Payment { PaymentDate = DateTime.Today });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Payment model)
    {
        if (!ModelState.IsValid)
        {
            await LoadOrdersAsync();
            return View(model);
        }
        context.Payments.Add(model);
        var order = await context.Orders.FindAsync(model.OrderId);
        if (order is not null) order.PaidAmount += model.Amount;
        await context.SaveChangesAsync();
        TempData["Success"] = "Odeme eklendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var payment = await context.Payments.FindAsync(id);
        if (payment is null) return NotFound();
        await LoadOrdersAsync();
        return View(payment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Payment model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadOrdersAsync();
            return View(model);
        }

        var existing = await context.Payments.AsNoTracking().FirstAsync(x => x.Id == id);
        context.Update(model);
        var order = await context.Orders.FindAsync(model.OrderId);
        if (order is not null) order.PaidAmount += model.Amount - existing.Amount;
        await context.SaveChangesAsync();
        TempData["Success"] = "Odeme guncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var payment = await context.Payments.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == id);
        return payment is null ? NotFound() : View(payment);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var payment = await context.Payments.FindAsync(id);
        if (payment is null) return NotFound();
        var order = await context.Orders.FindAsync(payment.OrderId);
        if (order is not null) order.PaidAmount -= payment.Amount;
        context.Payments.Remove(payment);
        await context.SaveChangesAsync();
        TempData["Success"] = "Odeme silindi.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadOrdersAsync()
        => ViewBag.Orders = new SelectList(await context.Orders.Include(x => x.Customer).OrderByDescending(x => x.CreatedAt)
            .Select(x => new { x.Id, Text = $"{x.Title} - {x.Customer!.FullName}" }).ToListAsync(), "Id", "Text");
}
