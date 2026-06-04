using System.ComponentModel.DataAnnotations;

namespace TerziTuran.Web.Models;

public class Appointment
{
    [Display(Name = "No")]
    public int Id { get; set; }

    [Display(Name = "Musteri")]
    [Required]
    public int CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Siparis")]
    public int? OrderId { get; set; }
    public Order? Order { get; set; }

    [Display(Name = "Randevu Tarihi")]
    [Required(ErrorMessage = "Randevu tarihi zorunludur.")]
    public DateTime AppointmentDate { get; set; }

    [Display(Name = "Baslik")]
    [Required(ErrorMessage = "Baslik zorunludur.")]
    [StringLength(150)]
    public string Title { get; set; } = string.Empty;

    [Display(Name = "Aciklama")]
    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Durum")]
    [Required]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
}
