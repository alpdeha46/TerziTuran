using System.ComponentModel.DataAnnotations;

namespace TerziTuran.Web.Models;

public class Customer
{
    [Display(Name = "No")]
    public int Id { get; set; }

    [Display(Name = "Ad Soyad")]
    [Required(ErrorMessage = "Musteri adi zorunludur.")]
    [StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Required(ErrorMessage = "Telefon numarasi zorunludur.")]
    [Phone(ErrorMessage = "Gecerli bir telefon numarasi giriniz.")]
    [StringLength(20)]
    public string Phone { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta adresi giriniz.")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Display(Name = "Adres")]
    [StringLength(300)]
    public string? Address { get; set; }

    [Display(Name = "Notlar")]
    [StringLength(1000)]
    public string? Notes { get; set; }

    [Display(Name = "Kayit Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Measurement> Measurements { get; set; } = new List<Measurement>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
