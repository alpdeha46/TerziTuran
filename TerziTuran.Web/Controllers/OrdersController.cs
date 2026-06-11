using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Extensions;
using TerziTuran.Web.Models;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "StaffOrAdmin")]
public class OrdersController(AppDbContext context) : Controller
{
    public async Task<IActionResult> Index(
        string? search,
        string? status,
        string? category,
        string sortBy = "created",
        string sortDirection = "desc")
    {
        var orders = await context.Orders
            .Include(x => x.Customer)
            .Include(x => x.CreatedByUser)
            .Include(x => x.BagReceipts)
            .AsNoTracking()
            .ToListAsync();

        if (Enum.TryParse<OrderStatus>(status, true, out var parsedStatus))
        {
            orders = orders.Where(x => x.Status == parsedStatus).ToList();
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            var normalizedCategory = NormalizeSearchText(category);
            orders = orders.Where(x => NormalizeSearchText(x.Category).Contains(normalizedCategory)).ToList();
        }

        var suggestions = new List<string>();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var normalizedSearch = NormalizeSearchText(search);
            var searchableOrders = orders;
            orders = searchableOrders.Where(x => MatchesSearch(x, normalizedSearch)).ToList();

            if (orders.Count == 0)
            {
                suggestions = FindSuggestions(searchableOrders, normalizedSearch);
            }
        }

        sortBy = NormalizeSortBy(sortBy);
        sortDirection = string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase) ? "asc" : "desc";
        orders = SortOrders(orders, sortBy, sortDirection == "desc").ToList();

        return View(new OrdersIndexViewModel
        {
            Orders = orders,
            Search = search?.Trim(),
            Status = status,
            Category = category?.Trim(),
            SortBy = sortBy,
            SortDirection = sortDirection,
            Suggestions = suggestions
        });
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
            NewBagReceipt = new ViewModels.BagReceiptCreateViewModel { OrderId = id, BagCount = order.BagCount },
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

    private static bool MatchesSearch(Order order, string normalizedSearch)
    {
        var fields = new[]
        {
            order.Title,
            order.Category,
            order.Description,
            order.Customer?.FullName,
            order.CreatedByUser?.FullName,
            order.ServiceType.GetDisplayName(),
            order.Status.GetDisplayName()
        }.Concat(order.BagReceipts.SelectMany(x => new[] { x.ReceiptNumber, x.PickupCode, x.Note }));

        var combinedText = NormalizeSearchText(string.Join(' ', fields.Where(x => !string.IsNullOrWhiteSpace(x))));
        return normalizedSearch.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .All(term => combinedText.Contains(term, StringComparison.Ordinal));
    }

    private static List<string> FindSuggestions(IEnumerable<Order> orders, string normalizedSearch)
    {
        var candidates = orders
            .SelectMany(x => new[] { x.Title, x.Category, x.Customer?.FullName })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!.Trim())
            .Distinct(StringComparer.Create(new CultureInfo("tr-TR"), true))
            .Select(x => new
            {
                Value = x,
                Distance = SuggestionDistance(normalizedSearch, NormalizeSearchText(x))
            })
            .Where(x => x.Distance <= Math.Max(2, normalizedSearch.Length / 3))
            .OrderBy(x => x.Distance)
            .ThenBy(x => x.Value)
            .Take(5)
            .Select(x => x.Value)
            .ToList();

        return candidates;
    }

    private static int SuggestionDistance(string search, string candidate)
    {
        var distances = candidate
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Append(candidate)
            .Select(part => LevenshteinDistance(search, part));

        return distances.Min();
    }

    private static IEnumerable<Order> SortOrders(IEnumerable<Order> orders, string sortBy, bool descending)
    {
        Func<Order, object> keySelector = sortBy switch
        {
            "title" => x => NormalizeSearchText(x.Title),
            "customer" => x => NormalizeSearchText(x.Customer?.FullName),
            "service" => x => x.ServiceType,
            "status" => x => x.Status,
            "deliveryDate" => x => x.DeliveryDate,
            "receipt" => x => x.BagReceipts
                .Where(receipt => !receipt.IsDelivered)
                .OrderByDescending(receipt => receipt.IssuedAt)
                .Select(receipt => receipt.BagNumber)
                .FirstOrDefault(),
            _ => x => x.CreatedAt
        };

        return descending
            ? orders.OrderByDescending(keySelector).ThenByDescending(x => x.CreatedAt)
            : orders.OrderBy(keySelector).ThenByDescending(x => x.CreatedAt);
    }

    private static string NormalizeSortBy(string? sortBy)
        => sortBy is "title" or "customer" or "service" or "status" or "deliveryDate" or "receipt"
            ? sortBy
            : "created";

    private static string NormalizeSearchText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;

        var decomposed = value.Trim().ToLower(new CultureInfo("tr-TR")).Replace('ı', 'i').Normalize(NormalizationForm.FormD);
        var result = new StringBuilder(decomposed.Length);
        var previousWasSpace = false;

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark) continue;

            if (char.IsLetterOrDigit(character))
            {
                result.Append(character);
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                result.Append(' ');
                previousWasSpace = true;
            }
        }

        return result.ToString().Trim();
    }

    private static int LevenshteinDistance(string source, string target)
    {
        if (source.Length == 0) return target.Length;
        if (target.Length == 0) return source.Length;

        var previous = Enumerable.Range(0, target.Length + 1).ToArray();
        var current = new int[target.Length + 1];

        for (var sourceIndex = 1; sourceIndex <= source.Length; sourceIndex++)
        {
            current[0] = sourceIndex;
            for (var targetIndex = 1; targetIndex <= target.Length; targetIndex++)
            {
                var cost = source[sourceIndex - 1] == target[targetIndex - 1] ? 0 : 1;
                current[targetIndex] = Math.Min(
                    Math.Min(current[targetIndex - 1] + 1, previous[targetIndex] + 1),
                    previous[targetIndex - 1] + cost);
            }

            (previous, current) = (current, previous);
        }

        return previous[target.Length];
    }
}
