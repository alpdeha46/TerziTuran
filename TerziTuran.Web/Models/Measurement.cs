using System.ComponentModel.DataAnnotations;

namespace TerziTuran.Web.Models;

public class Measurement
{
    [Display(Name = "No")]
    public int Id { get; set; }

    [Display(Name = "Musteri")]
    [Required]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Gogus")]
    [Range(0, 999, ErrorMessage = "Olcu degeri negatif olamaz.")]
    public decimal? Chest { get; set; }

    [Display(Name = "Bel")]
    [Range(0, 999, ErrorMessage = "Olcu degeri negatif olamaz.")]
    public decimal? Waist { get; set; }

    [Display(Name = "Kalca")]
    [Range(0, 999, ErrorMessage = "Olcu degeri negatif olamaz.")]
    public decimal? Hip { get; set; }

    [Display(Name = "Omuz")]
    [Range(0, 999, ErrorMessage = "Olcu degeri negatif olamaz.")]
    public decimal? Shoulder { get; set; }

    [Display(Name = "Kol")]
    [Range(0, 999, ErrorMessage = "Olcu degeri negatif olamaz.")]
    public decimal? Sleeve { get; set; }

    [Display(Name = "Paca Ici")]
    [Range(0, 999, ErrorMessage = "Olcu degeri negatif olamaz.")]
    public decimal? Inseam { get; set; }

    [Display(Name = "Yaka")]
    [Range(0, 999, ErrorMessage = "Olcu degeri negatif olamaz.")]
    public decimal? Neck { get; set; }

    [Display(Name = "Boy")]
    [Range(0, 300, ErrorMessage = "Boy negatif olamaz.")]
    public decimal? Height { get; set; }

    [Display(Name = "Kilo")]
    [Range(0, 500, ErrorMessage = "Kilo negatif olamaz.")]
    public decimal? Weight { get; set; }

    [Display(Name = "Notlar")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Display(Name = "Kayit Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
