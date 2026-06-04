using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TerziTuran.Web.Models;

public class OrderItem
{
    [Display(Name = "No")]
    public int Id { get; set; }

    [Display(Name = "Siparis")]
    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Display(Name = "Urun / Is Kalemi")]
    [Required(ErrorMessage = "Urun adi zorunludur.")]
    [StringLength(150)]
    public string ProductName { get; set; } = string.Empty;

    [Display(Name = "Adet")]
    [Range(1, 999, ErrorMessage = "Adet 0'dan buyuk olmalidir.")]
    public int Quantity { get; set; } = 1;

    [Display(Name = "Birim Fiyat")]
    [Range(0, 999999, ErrorMessage = "Birim fiyat negatif olamaz.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    [Display(Name = "Toplam Tutar")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalPrice { get; set; }
}
