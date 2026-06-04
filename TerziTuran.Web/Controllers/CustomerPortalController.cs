using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Models;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "CustomerOnly")]
public class CustomerPortalController(AppDbContext context) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await GetCurrentUserAsync();
        if (user?.CustomerId is null) return RedirectToAction("AccessDenied", "Auth");

        var customer = await context.Customers.FindAsync(user.CustomerId.Value);
        if (customer is null) return RedirectToAction("AccessDenied", "Auth");

        var orders = await context.Orders
            .Include(x => x.Payments)
            .Where(x => x.CustomerId == customer.Id)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        var appointments = await context.Appointments
            .Where(x => x.CustomerId == customer.Id && x.AppointmentDate >= DateTime.Now)
            .OrderBy(x => x.AppointmentDate)
            .ToListAsync();

        return View(new CustomerPortalViewModel
        {
            Customer = customer,
            RecentOrders = orders.Take(6).ToList(),
            UpcomingAppointments = appointments.Take(5).ToList(),
            TotalSpent = orders.Sum(x => x.PaidAmount),
            RemainingAmount = orders.Sum(x => x.Price - x.PaidAmount)
        });
    }

    [HttpGet]
    public async Task<IActionResult> Orders()
    {
        var user = await GetCurrentUserAsync();
        if (user?.CustomerId is null) return RedirectToAction("AccessDenied", "Auth");

        var orders = await context.Orders
            .Include(x => x.CreatedByUser)
            .Include(x => x.OrderItems)
            .Include(x => x.Payments)
            .Where(x => x.CustomerId == user.CustomerId.Value)
            .OrderByDescending(x => x.CreatedAt)
            .ToListAsync();

        return View(orders);
    }

    [HttpGet]
    public async Task<IActionResult> CreateOrder()
    {
        var user = await GetCurrentUserAsync();
        if (user?.CustomerId is null) return RedirectToAction("AccessDenied", "Auth");

        var customer = await context.Customers.FindAsync(user.CustomerId.Value);
        if (customer is null) return RedirectToAction("AccessDenied", "Auth");

        return View(BuildCustomerOrderForm(customer));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateOrder(CustomerOrderCreateViewModel model)
    {
        var user = await GetCurrentUserAsync();
        if (user?.CustomerId is null) return RedirectToAction("AccessDenied", "Auth");

        var customer = await context.Customers.FindAsync(user.CustomerId.Value);
        if (customer is null) return RedirectToAction("AccessDenied", "Auth");

        if (string.IsNullOrWhiteSpace(model.Title))
        {
            ModelState.AddModelError(nameof(model.Title), "Siparis basligi zorunludur.");
        }

        if (string.IsNullOrWhiteSpace(model.Category))
        {
            ModelState.AddModelError(nameof(model.Category), "Kategori seciniz.");
        }

        if (!ModelState.IsValid)
        {
            return View(BuildCustomerOrderForm(customer, model));
        }

        var order = new Order
        {
            CustomerId = customer.Id,
            Title = model.Title,
            Category = model.Category,
            Description = model.Description,
            ServiceType = model.ServiceType,
            Status = OrderStatus.Pending,
            Priority = OrderPriority.Medium,
            Price = 0,
            PaidAmount = 0,
            DeliveryDate = model.DeliveryDate,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = user.Id,
            IsCustomerRequest = true
        };

        context.Orders.Add(order);
        await context.SaveChangesAsync();
        TempData["Success"] = "Siparis talebiniz atolye ekibine iletildi.";
        return RedirectToAction(nameof(Orders));
    }

    [HttpGet]
    public async Task<IActionResult> OrderDetails(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user?.CustomerId is null) return RedirectToAction("AccessDenied", "Auth");

        var order = await context.Orders
            .Include(x => x.Customer)
            .Include(x => x.OrderItems)
            .Include(x => x.Payments)
            .Include(x => x.Appointments)
            .FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == user.CustomerId.Value);

        if (order?.Customer is null) return NotFound();

        return View(new OrderDetailsViewModel
        {
            Order = order,
            Customer = order.Customer,
            OrderItems = order.OrderItems.ToList(),
            Payments = order.Payments.OrderByDescending(x => x.PaymentDate).ToList(),
            BagReceipts = await context.BagReceipts.Where(x => x.OrderId == order.Id).OrderByDescending(x => x.IssuedAt).ToListAsync()
        });
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return await context.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }

    private static CustomerOrderCreateViewModel BuildCustomerOrderForm(Customer customer, CustomerOrderCreateViewModel? current = null)
    {
        current ??= new CustomerOrderCreateViewModel();
        current.CustomerId = customer.Id;
        current.CustomerName = customer.FullName;
        current.SewingCategories =
        [
            "Takim Elbise", "Gomlek", "Pantolon", "Ceket", "Abiye", "Yelek", "Etek", "Ozel Tasarim"
        ];
        current.RepairCategories =
        [
            "Paca Kisaltma", "Bel Daraltma", "Fermuar Degisimi", "Sokuk Onarimi", "Astar Degisimi", "Kol Boyu", "Genisletme / Daraltma"
        ];
        return current;
    }
}
