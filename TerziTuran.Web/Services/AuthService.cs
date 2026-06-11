using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.Services;

public interface IAuthService
{
    Task<User?> ValidateUserAsync(string username, string password);
    Task<(bool Success, string Message, User? User)> RegisterCustomerAsync(RegisterRequestDto dto);
    Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    Task<(bool Success, string Message, User? User)> CreateUserAsync(User user);
    Task<(bool Success, string Message, UserPasswordRequest? Request)> CreateActivationRequestAsync(User user);
    Task<(bool Success, string Message)> StartForgotPasswordAsync(string email);
    Task<(bool Success, string Message, User? User)> CompleteActivationAsync(int userId, string code, string newPassword);
    Task<(bool Success, string Message, User? User)> CompleteActivationAsync(string username, string code, string newPassword);
    Task<(bool Success, string Message)> CompleteForgotPasswordAsync(string email, string code, string newPassword);
    Task<(bool Success, string Message, UserPasswordRequest? Request)> DispatchCodeRequestAsync(int requestId);
}

public class AuthService(AppDbContext context, IPasswordHasher<User> hasher) : IAuthService
{
    private const int PasswordRequestExpiryMinutes = 15;

    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        var user = await context.Users.FirstOrDefaultAsync(x => x.Username.ToLower() == normalizedUsername);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<(bool Success, string Message, User? User)> RegisterCustomerAsync(RegisterRequestDto dto)
    {
        return await RegisterInternalAsync(dto, UserRole.Customer, createCustomerRecord: true);
    }

    public async Task<(bool Success, string Message, User? User)> CreateUserAsync(User user)
    {
        user.Username = user.Username.Trim().ToLowerInvariant();
        user.Email = user.Email.Trim().ToLowerInvariant();
        user.FullName = user.FullName.Trim();
        user.Phone = string.IsNullOrWhiteSpace(user.Phone) ? null : user.Phone.Trim();

        if (await context.Users.AnyAsync(x => x.Username == user.Username || x.Email == user.Email))
        {
            return (false, "Kullanici adi veya e-posta zaten kullaniliyor.", null);
        }

        user.MustChangePassword = true;
        user.CreatedAt = DateTime.UtcNow;
        user.PasswordHash = hasher.HashPassword(user, GeneratePlaceholderPassword());
        context.Users.Add(user);
        await context.SaveChangesAsync();
        await CreatePasswordRequestAsync(user, PasswordRequestType.Activation);

        return (true, "Kullanici olusturuldu. Aktivasyon kodu sifre taleplerine dustu.", user);
    }

    private async Task<(bool Success, string Message, User? User)> RegisterInternalAsync(RegisterRequestDto dto, UserRole role, bool createCustomerRecord)
    {
        var username = dto.Username.Trim().ToLowerInvariant();
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await context.Users.AnyAsync(x => x.Username.ToLower() == username))
        {
            return (false, "Bu kullanici adi zaten kullaniliyor.", null);
        }

        if (await context.Users.AnyAsync(x => x.Email.ToLower() == email))
        {
            return (false, "Bu e-posta adresi zaten kayitli.", null);
        }

        await using var transaction = await context.Database.BeginTransactionAsync();
        Customer? customer = null;
        if (createCustomerRecord)
        {
            customer = new Customer
            {
                FullName = dto.FullName.Trim(),
                Email = email,
                Phone = dto.Phone?.Trim() ?? string.Empty,
                Address = string.Empty,
                Notes = "Musteri portali",
                CreatedAt = DateTime.UtcNow
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
        }

        var user = new User
        {
            FullName = dto.FullName.Trim(),
            Username = username,
            Email = email,
            Phone = dto.Phone?.Trim(),
            Role = role,
            CustomerId = customer?.Id,
            IsActive = true,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = hasher.HashPassword(user, GeneratePlaceholderPassword());
        context.Users.Add(user);
        await context.SaveChangesAsync();
        await CreatePasswordRequestAsync(user, PasswordRequestType.Activation);
        await transaction.CommitAsync();
        return (true, "Kullanici kaydi olusturuldu. Aktivasyon kodu yonetici taleplerine gonderildi.", user);
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null)
        {
            return (false, "Kullanici bulunamadi.");
        }

        var verify = hasher.VerifyHashedPassword(user, user.PasswordHash, currentPassword);
        if (verify == PasswordVerificationResult.Failed)
        {
            return (false, "Mevcut sifre yanlis.");
        }

        if (!PasswordPolicy.IsValid(newPassword))
        {
            return (false, PasswordPolicy.ErrorMessage);
        }

        user.PasswordHash = hasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        await context.SaveChangesAsync();
        return (true, "Sifre guncellendi.");
    }

    public async Task<(bool Success, string Message, UserPasswordRequest? Request)> CreateActivationRequestAsync(User user)
    {
        if (!user.IsActive)
        {
            return (false, "Pasif kullanici icin kod olusturulamaz.", null);
        }

        if (!user.MustChangePassword)
        {
            user.MustChangePassword = true;
            await context.SaveChangesAsync();
        }

        var request = await CreatePasswordRequestAsync(user, PasswordRequestType.Activation);
        return (true, "Aktivasyon kodu olusturuldu.", request);
    }

    public async Task<(bool Success, string Message)> StartForgotPasswordAsync(string email)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail && x.IsActive);
        if (user is null)
        {
            return (true, "Bu e-posta sistemde kayitliysa sifre yenileme kodu olusturuldu.");
        }

        await CreatePasswordRequestAsync(user, PasswordRequestType.ForgotPassword);
        return (true, "Bu e-posta sistemde kayitliysa sifre yenileme kodu olusturuldu.");
    }

    public async Task<(bool Success, string Message, User? User)> CompleteActivationAsync(int userId, string code, string newPassword)
    {
        var user = await context.Users.FindAsync(userId);
        if (user is null || !user.IsActive)
        {
            return (false, "Kullanici bulunamadi.", null);
        }

        if (!PasswordPolicy.IsValid(newPassword))
        {
            return (false, PasswordPolicy.ErrorMessage, null);
        }

        var request = await GetValidPasswordRequestAsync(user.Id, PasswordRequestType.Activation, code);
        if (request is null)
        {
            return (false, "Kod gecersiz, kullanilmis veya suresi dolmus.", null);
        }

        user.PasswordHash = hasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        request.IsUsed = true;
        request.UsedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return (true, "Kod dogrulandi. Yeni sifreniz kaydedildi.", user);
    }

    public async Task<(bool Success, string Message, User? User)> CompleteActivationAsync(string username, string code, string newPassword)
    {
        var normalizedUsername = username.Trim().ToLowerInvariant();
        var user = await context.Users.FirstOrDefaultAsync(x => x.Username.ToLower() == normalizedUsername && x.IsActive);
        if (user is null)
        {
            return (false, "Kullanici bulunamadi.", null);
        }

        return await CompleteActivationAsync(user.Id, code, newPassword);
    }

    public async Task<(bool Success, string Message)> CompleteForgotPasswordAsync(string email, string code, string newPassword)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await context.Users.FirstOrDefaultAsync(x => x.Email.ToLower() == normalizedEmail && x.IsActive);
        if (user is null)
        {
            return (false, "Kod gecersiz, kullanilmis veya suresi dolmus.");
        }

        if (!PasswordPolicy.IsValid(newPassword))
        {
            return (false, PasswordPolicy.ErrorMessage);
        }

        var request = await GetValidPasswordRequestAsync(user.Id, PasswordRequestType.ForgotPassword, code);
        if (request is null)
        {
            return (false, "Kod gecersiz, kullanilmis veya suresi dolmus.");
        }

        user.PasswordHash = hasher.HashPassword(user, newPassword);
        user.MustChangePassword = false;
        request.IsUsed = true;
        request.UsedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return (true, "Sifreniz yenilendi. Yeni sifrenizle giris yapabilirsiniz.");
    }

    public async Task<(bool Success, string Message, UserPasswordRequest? Request)> DispatchCodeRequestAsync(int requestId)
    {
        var request = await context.UserPasswordRequests
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == requestId);

        if (request is null)
        {
            return (false, "Kod talebi bulunamadi.", null);
        }

        if (request.IsUsed || request.ExpiresAt < DateTime.UtcNow)
        {
            return (false, "Kod talebi artik aktif degil.", null);
        }

        request.IsDispatched = true;
        request.DispatchedAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return (true, "Kod talebi kopyalandi ve listeden dusuruldu.", request);
    }

    private async Task<UserPasswordRequest> CreatePasswordRequestAsync(User user, PasswordRequestType requestType)
    {
        var openRequests = await context.UserPasswordRequests
            .Where(x => x.UserId == user.Id && x.RequestType == requestType && !x.IsUsed)
            .ToListAsync();

        foreach (var openRequest in openRequests)
        {
            openRequest.IsUsed = true;
            openRequest.UsedAt = DateTime.UtcNow;
        }

        var request = new UserPasswordRequest
        {
            UserId = user.Id,
            RequestType = requestType,
            Code = GenerateOneTimeCode(),
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddMinutes(PasswordRequestExpiryMinutes),
            IsDispatched = false
        };

        context.UserPasswordRequests.Add(request);
        await context.SaveChangesAsync();
        return request;
    }

    private async Task<UserPasswordRequest?> GetValidPasswordRequestAsync(int userId, PasswordRequestType requestType, string code)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        return await context.UserPasswordRequests
            .Where(x => x.UserId == userId &&
                        x.RequestType == requestType &&
                        !x.IsUsed &&
                        x.ExpiresAt >= DateTime.UtcNow &&
                        x.Code == normalizedCode)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();
    }

    private static string GenerateOneTimeCode()
    {
        const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = Random.Shared;
        return new string(Enumerable.Range(0, 6).Select(_ => alphabet[random.Next(alphabet.Length)]).ToArray());
    }

    private static string GeneratePlaceholderPassword()
        => $"Tmp!{Guid.NewGuid():N}Aa1";
}
