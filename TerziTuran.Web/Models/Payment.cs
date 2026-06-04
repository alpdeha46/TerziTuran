using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TerziTuran.Web.Models;

public class Payment
{
    [Display(Name = "No")]
    public int Id { get; set; }

    [Display(Name = "Siparis")]
    [Required]
    public int OrderId { get; set; }
    public Order? Order { get; set; }

    [Display(Name = "Odeme Tutari")]
    [Range(0.01, 999999, ErrorMessage = "Odeme tutari 0'dan buyuk olmalidir.")]
    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Display(Name = "Odeme Tipi")]
    [Required]
    public PaymentType PaymentType { get; set; }

    [Display(Name = "Odeme Tarihi")]
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;

    [Display(Name = "Aciklama / Not")]
    [StringLength(500)]
    public string? Note { get; set; }
}
