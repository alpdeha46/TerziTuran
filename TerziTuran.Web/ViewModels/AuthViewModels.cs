using System.ComponentModel.DataAnnotations;

namespace TerziTuran.Web.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Kullanici adi zorunludur.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre zorunludur.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Kullanici adi zorunludur.")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-posta zorunludur.")]
    [EmailAddress(ErrorMessage = "Gecerli bir e-posta giriniz.")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre zorunludur.")]
    [MinLength(6, ErrorMessage = "Sifre en az 6 karakter olmalidir.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Sifre tekrari zorunludur.")]
    [Compare(nameof(Password), ErrorMessage = "Sifreler ayni olmali.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;

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
    [MinLength(6, ErrorMessage = "Yeni sifre en az 6 karakter olmalidir.")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni sifre tekrari zorunludur.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Sifreler ayni olmali.")]
    [DataType(DataType.Password)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
