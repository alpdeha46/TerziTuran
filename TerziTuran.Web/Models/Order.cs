using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TerziTuran.Web.Models;

public class Order
{
    [Display(Name = "No")]
    public int Id { get; set; }

    [Display(Name = "Musteri")]
    [Required]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Siparis Basligi")]
    [Required(ErrorMessage = "Siparis basligi zorunludur.")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Aciklama")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Referans Fotograf")]
    [StringLength(300)]
    public string? PhotoPath { get; set; }

    [Display(Name = "Kategori")]
    [Required(ErrorMessage = "Kategori zorunludur.")]
    [StringLength(100)]
    public string Category { get; set; } = string.Empty;

    [Display(Name = "Hizmet Tipi")]
    [Required]
    public OrderServiceType ServiceType { get; set; } = OrderServiceType.Sewing;

    [Display(Name = "Durum")]
    [Required]
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    [Display(Name = "Oncelik")]
    [Required]
    public OrderPriority Priority { get; set; } = OrderPriority.Medium;

    [Display(Name = "Toplam Tutar")]
    [Range(0, 999999, ErrorMessage = "Fiyat negatif olamaz.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Display(Name = "Odenen Tutar")]
    [Range(0, 999999, ErrorMessage = "Odenen tutar negatif olamaz.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal PaidAmount { get; set; }

    [Display(Name = "Teslim Tarihi")]
    [DataType(DataType.Date)]
    public DateTime DeliveryDate { get; set; }

    [Display(Name = "Poşet Adedi")]
    [Range(1, 20, ErrorMessage = "Poşet adedi 1 ile 20 arasında olmalıdır.")]
    public int BagCount { get; set; } = 1;

    [Display(Name = "Olusturma Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Display(Name = "Musteri Talebi")]
    public bool IsCustomerRequest { get; set; }

    [Display(Name = "Olusturan Kullanici")]
    [Required]
    public int CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<BagReceipt> BagReceipts { get; set; } = new List<BagReceipt>();

    [NotMapped]
    public decimal RemainingAmount => Math.Max(0, Price - PaidAmount);
}
