using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Models;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin")]
public class OrdersController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(string? status, string? category)
    {
        var query = context.Orders.Include(x => x.Customer).Include(x => x.CreatedByUser).Include(x => x.BagReceipts).AsQueryable();
        if (Enum.TryParse<OrderStatus>(status, out var parsedStatus)) query = query.Where(x => x.Status == parsedStatus);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(x => x.Category.Contains(category));
        ViewBag.Status = status;
        ViewBag.Category = category;
        return View(await query.OrderByDescending(x => x.CreatedAt).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var order = await context.Orders.Include(x => x.Customer).Include(x => x.CreatedByUser).FirstOrDefaultAsync(x => x.Id == id);
        if (order is null || order.Customer is null) return NotFound();

        return View(new OrderDetailsViewModel
        {
            Order = order,
            Customer = order.Customer,
            OrderItems = await context.OrderItems.Where(x => x.OrderId == id).ToListAsync(),
            Payments = await context.Payments.Where(x => x.OrderId == id).OrderByDescending(x => x.PaymentDate).ToListAsync(),
            BagReceipts = await context.BagReceipts.Where(x => x.OrderId == id).OrderByDescending(x => x.IssuedAt).ToListAsync(),
            NewOrderItem = new OrderItem { OrderId = id },
            NewPayment = new Payment { OrderId = id, PaymentDate = DateTime.Today },
            NewBagReceipt = new ViewModels.BagReceiptCreateViewModel { OrderId = id, BagCount = 1 },
            UpdateStatus = order.Status
        });
    }

    public async Task<IActionResult> Create()
    {
        await LoadCustomersAsync();
        return View(new Order { DeliveryDate = DateTime.Today.AddDays(7), ServiceType = OrderServiceType.Sewing });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Order model)
    {
        if (!ModelState.IsValid)
        {
            await LoadCustomersAsync();
            return View(model);
        }

        model.CreatedByUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        context.Orders.Add(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Siparis olusturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var order = await context.Orders.FindAsync(id);
        if (order is null) return NotFound();
        await LoadCustomersAsync();
        return View(order);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Order model)
    {
        if (id != model.Id) return NotFound();
        if (!ModelState.IsValid)
        {
            await LoadCustomersAsync();
            return View(model);
        }

        var existing = await context.Orders.AsNoTracking().FirstAsync(x => x.Id == id);
        model.CreatedByUserId = existing.CreatedByUserId;
        model.CreatedAt = existing.CreatedAt;
        context.Update(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Siparis guncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var order = await context.Orders.Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        return order is null ? NotFound() : View(order);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var order = await context.Orders.FindAsync(id);
        if (order is null) return NotFound();
        context.Orders.Remove(order);
        await context.SaveChangesAsync();
        TempData["Success"] = "Siparis silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddOrderItem(OrderItem model)
    {
        model.TotalPrice = model.Quantity * model.UnitPrice;
        if (!TryValidateModel(model))
        {
            TempData["Error"] = "Siparis kalemi eklenemedi.";
            return RedirectToAction(nameof(Details), new { id = model.OrderId });
        }

        context.OrderItems.Add(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Siparis kalemi eklendi.";
        return RedirectToAction(nameof(Details), new { id = model.OrderId });
    }

    public async Task<IActionResult> EditOrderItem(int id)
    {
        var item = await context.OrderItems.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditOrderItem(int id, OrderItem model)
    {
        if (id != model.Id) return NotFound();
        model.TotalPrice = model.Quantity * model.UnitPrice;
        if (!ModelState.IsValid) return View(model);
        context.Update(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Siparis kalemi guncellendi.";
        return RedirectToAction(nameof(Details), new { id = model.OrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteOrderItem(int id, int orderId)
    {
        var item = await context.OrderItems.FindAsync(id);
        if (item is not null)
        {
            context.OrderItems.Remove(item);
            await context.SaveChangesAsync();
        }
        TempData["Success"] = "Siparis kalemi silindi.";
        return RedirectToAction(nameof(Details), new { id = orderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, OrderStatus status)
    {
        var order = await context.Orders.FindAsync(id);
        if (order is null) return NotFound();
        order.Status = status;
        await context.SaveChangesAsync();
        TempData["Success"] = "Siparis durumu guncellendi.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPayment(Payment model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Odeme eklenemedi.";
            return RedirectToAction(nameof(Details), new { id = model.OrderId });
        }

        context.Payments.Add(model);
        var order = await context.Orders.FindAsync(model.OrderId);
        if (order is not null)
        {
            order.PaidAmount += model.Amount;
        }

        await context.SaveChangesAsync();
        TempData["Success"] = "Odeme kaydi eklendi.";
        return RedirectToAction(nameof(Details), new { id = model.OrderId });
    }

    private async Task LoadCustomersAsync()
        => ViewBag.Customers = new SelectList(await context.Customers.OrderBy(x => x.FullName).ToListAsync(), "Id", "FullName");
}
