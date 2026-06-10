using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Services;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin")]
public class BagReceiptsController(AppDbContext context, IBagReceiptService bagReceiptService) : Controller
{
    public IActionResult Index()
        => RedirectToAction("Index", "Orders");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind(Prefix = "NewBagReceipt")] BagReceiptCreateViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Teslim fişi bilgileri geçersiz.";
            return RedirectToAction("Details", "Orders", new { id = model.OrderId });
        }

        try
        {
            var receipt = await bagReceiptService.CreateAsync(model.OrderId, model.BagCount, model.Note);
            TempData["Success"] = $"Teslim fişi siparişe atandı. Poşet no: {receipt.BagNumber}, teslim kodu: {receipt.PickupCode}";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Details", "Orders", new { id = model.OrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignToOrder(int orderId, int bagCount = 1, string? note = null)
    {
        try
        {
            var receipt = await bagReceiptService.CreateAsync(orderId, bagCount, note);
            TempData["Success"] = $"Teslim fişi atandı. Poşet no: {receipt.BagNumber}, teslim kodu: {receipt.PickupCode}";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction("Index", "Orders");
    }

    public async Task<IActionResult> Details(int id)
    {
        var receipt = await context.BagReceipts.Include(x => x.Order).ThenInclude(x => x!.Customer).FirstOrDefaultAsync(x => x.Id == id);
        return receipt is null ? NotFound() : View(receipt);
    }

    public async Task<IActionResult> Print(int id)
    {
        var receipt = await context.BagReceipts.Include(x => x.Order).ThenInclude(x => x!.Customer).FirstOrDefaultAsync(x => x.Id == id);
        return receipt is null ? NotFound() : View(receipt);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleDelivered(int id)
    {
        var receipt = await context.BagReceipts.FindAsync(id);
        if (receipt is null) return NotFound();

        receipt.IsDelivered = !receipt.IsDelivered;
        receipt.DeliveredAt = receipt.IsDelivered ? DateTime.Now : null;
        await context.SaveChangesAsync();
        TempData["Success"] = receipt.IsDelivered ? "Teslim fişi tamamlandı." : "Teslim fişi tekrar aktif duruma alındı.";
        return RedirectToAction("Details", "Orders", new { id = receipt.OrderId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var receipt = await context.BagReceipts.FindAsync(id);
        if (receipt is null) return NotFound();

        var orderId = receipt.OrderId;
        context.BagReceipts.Remove(receipt);
        await context.SaveChangesAsync();
        TempData["Success"] = "Teslim fişi siparişten kaldırıldı.";
        return RedirectToAction("Details", "Orders", new { id = orderId });
    }
}
