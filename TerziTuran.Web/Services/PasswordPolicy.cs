namespace TerziTuran.Web.Services;

public static class PasswordPolicy
{
    public const int MinimumLength = 10;

    public static bool IsValid(string? password)
    {
        return !string.IsNullOrWhiteSpace(password)
               && password.Length >= MinimumLength
               && password.Any(char.IsUpper)
               && password.Any(char.IsLower)
               && password.Any(char.IsDigit)
               && password.Any(ch => !char.IsLetterOrDigit(ch));
    }

    public const string ErrorMessage =
        "Sifre en az 10 karakter olmali; buyuk harf, kucuk harf, rakam ve ozel karakter icermelidir.";
}
