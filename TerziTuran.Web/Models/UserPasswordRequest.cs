using System.ComponentModel.DataAnnotations;

namespace TerziTuran.Web.Models;

public class UserPasswordRequest
{
    [Display(Name = "No")]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }
    public User? User { get; set; }

    [Required, StringLength(12)]
    [Display(Name = "Kod")]
    public string Code { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Talep Turu")]
    public PasswordRequestType RequestType { get; set; }

    [Display(Name = "Talep Tarihi")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Display(Name = "Son Gecerlilik")]
    public DateTime ExpiresAt { get; set; }

    [Display(Name = "Kullanildi Mi?")]
    public bool IsUsed { get; set; }

    [Display(Name = "Kullanilma Tarihi")]
    public DateTime? UsedAt { get; set; }

    [Display(Name = "Kopyalanip Gonderildi Mi?")]
    public bool IsDispatched { get; set; }

    [Display(Name = "Kopyalanma Tarihi")]
    public DateTime? DispatchedAt { get; set; }
}
