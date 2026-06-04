using System.ComponentModel.DataAnnotations;

namespace TerziTuran.Web.Models;

public class User
{
    [Display(Name = "No")]
    public int Id { get; set; }

    [Display(Name = "Ad Soyad")]
    [Required, StringLength(150)]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Kullanici Adi")]
    [Required, StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [Required, EmailAddress, StringLength(150)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [Display(Name = "Rol")]
    [Required]
    public UserRole Role { get; set; }

    [Display(Name = "Telefon")]
    [Phone, StringLength(20)]
    public string? Phone { get; set; }

    [Display(Name = "Bagli Musteri")]
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    [Display(Name = "Kayit Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Display(Name = "Aktif Mi?")]
    public bool IsActive { get; set; } = true;

    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
