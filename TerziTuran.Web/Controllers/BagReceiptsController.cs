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
    public async Task<IActionResult> Index()
        => RedirectToAction("Index", "Orders");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(BagReceiptCreateViewModel model)
    {
        if (model.BagCount < 1)
        {
            TempData["Error"] = "Poşet adedi en az 1 olmalıdır.";
            return RedirectToAction("Details", "Orders", new { id = model.OrderId });
        }

        var receipt = await bagReceiptService.CreateAsync(model.OrderId, model.BagCount, model.Note);
        TempData["Success"] = $"Poşet fişi oluşturuldu. Poşet no: {receipt.BagNumber}, teslim kodu: {receipt.PickupCode}";
        return RedirectToAction("Details", "Orders", new { id = model.OrderId });
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
        TempData["Success"] = receipt.IsDelivered ? "Fiş teslim edildi olarak işaretlendi." : "Fiş tekrar beklemede durumuna alındı.";
        return RedirectToAction("Details", "Orders", new { id = receipt.OrderId });
    }
}
