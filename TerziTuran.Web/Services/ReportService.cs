using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Extensions;
using TerziTuran.Web.Models;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Services;

public interface IReportService
{
    Task<ReportsFilterViewModel> BuildAsync(ReportsFilterViewModel filter);
}

public class ReportService(AppDbContext context) : IReportService
{
    public async Task<ReportsFilterViewModel> BuildAsync(ReportsFilterViewModel filter)
    {
        var query = context.Orders.Include(x => x.Customer).Include(x => x.CreatedByUser).AsQueryable();

        if (filter.StartDate.HasValue) query = query.Where(x => x.CreatedAt >= filter.StartDate.Value);
        if (filter.EndDate.HasValue) query = query.Where(x => x.CreatedAt <= filter.EndDate.Value.AddDays(1));
        if (filter.CustomerId.HasValue) query = query.Where(x => x.CustomerId == filter.CustomerId);
        if (filter.OrderStatus.HasValue) query = query.Where(x => x.Status == filter.OrderStatus);
        if (!string.IsNullOrWhiteSpace(filter.Category)) query = query.Where(x => x.Category == filter.Category);
        if (!string.IsNullOrWhiteSpace(filter.PaymentStatus))
        {
            query = filter.PaymentStatus switch
            {
                "Unpaid" => query.Where(x => x.PaidAmount <= 0),
                "PartiallyPaid" => query.Where(x => x.PaidAmount > 0 && x.PaidAmount < x.Price),
                "Paid" => query.Where(x => x.PaidAmount >= x.Price),
                _ => query
            };
        }

        var rows = await query.OrderByDescending(x => x.CreatedAt)
            .Select(x => new ReportRowViewModel
            {
                CustomerName = x.Customer!.FullName,
                ServiceType = x.ServiceType == OrderServiceType.Sewing ? "Dikim" : "Tamir",
                CreatedBy = x.CreatedByUser!.FullName,
                OrderTitle = x.Title,
                Category = x.Category,
                Status = x.Status.GetDisplayName(),
                Price = x.Price,
                PaidAmount = x.PaidAmount,
                RemainingAmount = x.Price - x.PaidAmount,
                DeliveryDate = x.DeliveryDate,
                CreatedDate = x.CreatedAt
            }).ToListAsync();

        filter.Customers = await context.Customers.OrderBy(x => x.FullName).ToListAsync();
        filter.Rows = rows;
        return filter;
    }
}
