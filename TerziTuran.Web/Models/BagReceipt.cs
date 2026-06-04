using System.ComponentModel.DataAnnotations;

namespace TerziTuran.Web.Models;

public class BagReceipt
{
    public int Id { get; set; }

    [Display(Name = "Sipariş")]
    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Display(Name = "Poşet Numarası")]
    [Range(1, 9999)]
    public int BagNumber { get; set; }

    [Display(Name = "Fiş Numarası")]
    [Required, StringLength(30)]
    public string ReceiptNumber { get; set; } = string.Empty;

    [Display(Name = "Teslim Kodu")]
    [Required, StringLength(10)]
    public string PickupCode { get; set; } = string.Empty;

    [Display(Name = "Poşet Adedi")]
    [Range(1, 20, ErrorMessage = "Poşet adedi en az 1 olmalıdır.")]
    public int BagCount { get; set; } = 1;

    [Display(Name = "Veriliş Tarihi")]
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Açıklama")]
    [StringLength(300)]
    public string? Note { get; set; }

    [Display(Name = "Teslim Alındı")]
    public bool IsDelivered { get; set; }

    [Display(Name = "Teslim Alınma Tarihi")]
    public DateTime? DeliveredAt { get; set; }
}
