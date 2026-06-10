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

    [Display(Name = "Şifre")]
    [Required(ErrorMessage = "Sifre zorunludur.")]
    [MinLength(10, ErrorMessage = "Sifre en az 10 karakter olmalidir.")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).+$", ErrorMessage = "Buyuk harf, kucuk harf, rakam ve ozel karakter kullaniniz.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Şifre tekrarı")]
    [Required(ErrorMessage = "Sifre tekrari zorunludur.")]
    [Compare(nameof(Password), ErrorMessage = "Sifreler ayni olmali.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Display(Name = "Telefon")]
    [Phone(ErrorMessage = "Gecerli bir telefon numarasi giriniz.")]
    public string? Phone { get; set; }
}

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    public string Email { get; set; } = string.Empty;
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
