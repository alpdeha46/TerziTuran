using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Extensions;
using TerziTuran.Web.Models;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Services;

public interface IDashboardService
{
    Task<DashboardViewModel> GetDashboardAsync(DateTime? startDate, DateTime? endDate, OrderStatus? status, string? category);
}

public class DashboardService(AppDbContext context) : IDashboardService
{
    public async Task<DashboardViewModel> GetDashboardAsync(DateTime? startDate, DateTime? endDate, OrderStatus? status, string? category)
    {
        var orders = context.Orders.Include(x => x.Customer).Include(x => x.CreatedByUser).AsQueryable();
        if (startDate.HasValue) orders = orders.Where(x => x.CreatedAt >= startDate.Value);
        if (endDate.HasValue) orders = orders.Where(x => x.CreatedAt <= endDate.Value.AddDays(1));
        if (status.HasValue) orders = orders.Where(x => x.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(category)) orders = orders.Where(x => x.Category == category);

        var orderList = await orders.OrderByDescending(x => x.CreatedAt).ToListAsync();
        var monthGroups = orderList
            .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
            .OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month)
            .Select(x => new { label = $"{x.Key.Month:00}/{x.Key.Year}", value = x.Count() });

        var revenueGroups = orderList
            .GroupBy(x => new { x.CreatedAt.Year, x.CreatedAt.Month })
            .OrderBy(x => x.Key.Year).ThenBy(x => x.Key.Month)
            .Select(x => new { label = $"{x.Key.Month:00}/{x.Key.Year}", value = x.Sum(y => y.PaidAmount) });

        var statusGroups = orderList.GroupBy(x => x.Status).Select(x => new { label = x.Key.GetDisplayName(), value = x.Count() });

        return new DashboardViewModel
        {
            TotalCustomers = await context.Customers.CountAsync(),
            TotalOrders = orderList.Count,
            PendingOrders = orderList.Count(x => x.Status == OrderStatus.Pending),
            DeliveredOrders = orderList.Count(x => x.Status == OrderStatus.Delivered),
            TotalRevenue = orderList.Sum(x => x.PaidAmount),
            RemainingPayments = orderList.Sum(x => x.Price - x.PaidAmount),
            TodaysAppointments = await context.Appointments.CountAsync(x => x.AppointmentDate.Date == DateTime.Today),
            UpcomingDeliveries = orderList.Count(x => x.DeliveryDate.Date >= DateTime.Today && x.DeliveryDate.Date <= DateTime.Today.AddDays(7)),
            RecentCustomers = await context.Customers.OrderByDescending(x => x.CreatedAt).Take(5).ToListAsync(),
            RecentOrders = orderList.Take(5).ToList(),
            TodayAppointments = await context.Appointments.Include(x => x.Customer).Include(x => x.Order)
                .Where(x => x.AppointmentDate.Date == DateTime.Today).OrderBy(x => x.AppointmentDate).ToListAsync(),
            UpcomingDeliveriesList = orderList.Where(x => x.DeliveryDate.Date >= DateTime.Today).OrderBy(x => x.DeliveryDate).Take(5).ToList(),
            MonthlyOrderChartJson = JsonSerializer.Serialize(monthGroups),
            StatusPieChartJson = JsonSerializer.Serialize(statusGroups),
            MonthlyRevenueChartJson = JsonSerializer.Serialize(revenueGroups)
        };
    }
}
