using TerziTuran.Web.Models;

namespace TerziTuran.Web.ViewModels;

public class CustomerDetailsViewModel
{
    public Customer Customer { get; set; } = new();
    public List<Measurement> Measurements { get; set; } = [];
    public List<Order> Orders { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<Appointment> Appointments { get; set; } = [];
}

public class OrderDetailsViewModel
{
    public Order Order { get; set; } = new();
    public Customer Customer { get; set; } = new();
    public List<OrderItem> OrderItems { get; set; } = [];
    public List<Payment> Payments { get; set; } = [];
    public List<BagReceipt> BagReceipts { get; set; } = [];
    public OrderItem NewOrderItem { get; set; } = new();
    public Payment NewPayment { get; set; } = new() { PaymentDate = DateTime.Today };
    public OrderStatus UpdateStatus { get; set; }
    public BagReceiptCreateViewModel NewBagReceipt { get; set; } = new();
}

public class CustomerPortalViewModel
{
    public Customer Customer { get; set; } = new();
    public List<Order> RecentOrders { get; set; } = [];
    public List<Appointment> UpcomingAppointments { get; set; } = [];
    public decimal TotalSpent { get; set; }
    public decimal RemainingAmount { get; set; }
}

public class CustomerOrderCreateViewModel
{
    public int CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public OrderServiceType ServiceType { get; set; } = OrderServiceType.Sewing;
    public string Title { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime DeliveryDate { get; set; } = DateTime.Today.AddDays(7);
    public List<string> SewingCategories { get; set; } = [];
    public List<string> RepairCategories { get; set; } = [];
}

public class DashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalOrders { get; set; }
    public int PendingOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public decimal TotalRevenue { get; set; }
    public decimal RemainingPayments { get; set; }
    public int TodaysAppointments { get; set; }
    public int UpcomingDeliveries { get; set; }
    public List<Customer> RecentCustomers { get; set; } = [];
    public List<Order> RecentOrders { get; set; } = [];
    public List<Appointment> TodayAppointments { get; set; } = [];
    public List<Order> UpcomingDeliveriesList { get; set; } = [];
    public string MonthlyOrderChartJson { get; set; } = "[]";
    public string StatusPieChartJson { get; set; } = "[]";
    public string MonthlyRevenueChartJson { get; set; } = "[]";
}

public class ReportsFilterViewModel
{
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? CustomerId { get; set; }
    public OrderStatus? OrderStatus { get; set; }
    public string? PaymentStatus { get; set; }
    public string? Category { get; set; }
    public List<Customer> Customers { get; set; } = [];
    public List<ReportRowViewModel> Rows { get; set; } = [];
}

public class ReportRowViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public string ServiceType { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string OrderTitle { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateTime DeliveryDate { get; set; }
    public DateTime CreatedDate { get; set; }
}

public class BagReceiptCreateViewModel
{
    public int OrderId { get; set; }
    public int BagCount { get; set; } = 1;
    public string? Note { get; set; }
}
