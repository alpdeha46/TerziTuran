using System.ComponentModel.DataAnnotations;

namespace TerziTuran.Web.ViewModels;

public class LoginViewModel
{
    [Display(Name = "Kullanıcı adı")]
    [Required(ErrorMessage = "Kullanici adi zorunludur.")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Şifre")]
    [Required(ErrorMessage = "Sifre zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Display(Name = "Ad soyad")]
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Kullanıcı adı")]
    [Required(ErrorMessage = "Kullanici adi zorunludur.")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Phone(ErrorMessage = "Gecerli bir telefon numarasi giriniz.")]
    public string? Phone { get; set; }
}

public class ForgotPasswordViewModel
{
    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    public string Email { get; set; } = string.Empty;
}

public class CompleteActivationViewModel
{
    public int? UserId { get; set; }

    [Display(Name = "Kullanıcı adı")]
    [Required(ErrorMessage = "Kullanici adi zorunludur.")]
    public string Username { get; set; } = string.Empty;

    [Display(Name = "Aktivasyon kodu")]
    [Required(ErrorMessage = "Kod zorunludur.")]
    [StringLength(12, MinimumLength = 6, ErrorMessage = "Kod 6 ile 12 karakter arasinda olmalidir.")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Yeni şifre")]
    [Required(ErrorMessage = "Yeni sifre zorunludur.")]
    [MinLength(10, ErrorMessage = "Yeni sifre en az 10 karakter olmalidir.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "Buyuk harf, kucuk harf, rakam ve ozel karakter kullaniniz.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Yeni şifre tekrarı")]
    [Required(ErrorMessage = "Yeni sifre tekrari zorunludur.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Sifreler ayni olmali.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ResetPasswordWithCodeViewModel
{
    [Display(Name = "E-posta")]
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Tek kullanımlık kod")]
    [Required(ErrorMessage = "Kod zorunludur.")]
    [StringLength(12, MinimumLength = 6, ErrorMessage = "Kod 6 ile 12 karakter arasinda olmalidir.")]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Yeni şifre")]
    [Required(ErrorMessage = "Yeni sifre zorunludur.")]
    [MinLength(10, ErrorMessage = "Yeni sifre en az 10 karakter olmalidir.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "Buyuk harf, kucuk harf, rakam ve ozel karakter kullaniniz.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Display(Name = "Yeni şifre tekrarı")]
    [Required(ErrorMessage = "Yeni sifre tekrari zorunludur.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Sifreler ayni olmali.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Mevcut sifre zorunludur.")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni sifre zorunludur.")]
    [MinLength(10, ErrorMessage = "Yeni sifre en az 10 karakter olmalidir.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "Buyuk harf, kucuk harf, rakam ve ozel karakter kullaniniz.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni sifre tekrari zorunludur.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Sifreler ayni olmali.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
