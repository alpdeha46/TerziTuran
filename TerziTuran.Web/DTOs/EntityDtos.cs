using System.ComponentModel.DataAnnotations;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.DTOs;

public class CustomerDto
{
    public int Id { get; set; }
    [Required] public string FullName { get; set; } = string.Empty;
    [Required, Phone] public string Phone { get; set; } = string.Empty;
    [EmailAddress] public string? Email { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MeasurementDto
{
    public int Id { get; set; }
    [Required] public int CustomerId { get; set; }
    public decimal? Chest { get; set; }
    public decimal? Waist { get; set; }
    public decimal? Hip { get; set; }
    public decimal? Shoulder { get; set; }
    public decimal? Sleeve { get; set; }
    public decimal? Inseam { get; set; }
    public decimal? Neck { get; set; }
    public decimal? Height { get; set; }
    public decimal? Weight { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class OrderDto
{
    public int Id { get; set; }
    [Required] public int CustomerId { get; set; }
    [Required] public string Title { get; set; } = string.Empty;
    [Required] public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public OrderServiceType ServiceType { get; set; }
    public OrderStatus Status { get; set; }
    public OrderPriority Priority { get; set; }
    [Range(0, 999999)] public decimal Price { get; set; }
    [Range(0, 999999)] public decimal PaidAmount { get; set; }
    public DateTime DeliveryDate { get; set; }
    [Range(1, 20)] public int BagCount { get; set; } = 1;
    public DateTime CreatedAt { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    [Required] public int OrderId { get; set; }
    [Range(0.01, 999999)] public decimal Amount { get; set; }
    public PaymentType PaymentType { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? Note { get; set; }
}

public class AppointmentDto
{
    public int Id { get; set; }
    [Required] public int CustomerId { get; set; }
    public int? OrderId { get; set; }
    [Required] public DateTime AppointmentDate { get; set; }
    [Required] public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AppointmentStatus Status { get; set; }
}
