using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.Services;

public interface IAuthService
{
    Task<User?> ValidateUserAsync(string username, string password);
    Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterRequestDto dto);
    Task<(bool Success, string Message, User? User)> RegisterCustomerAsync(RegisterRequestDto dto);
    Task<(bool Success, string Message)> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
}

public class AuthService(AppDbContext context, IPasswordHasher<User> hasher) : IAuthService
{
    public async Task<User?> ValidateUserAsync(string username, string password)
    {
        var user = await context.Users.FirstOrDefaultAsync(x => x.Username == username);
        if (user is null || !user.IsActive)
        {
            return null;
        }

        var result = hasher.VerifyHashedPassword(user, user.PasswordHash, password);
        return result == PasswordVerificationResult.Failed ? null : user;
    }

    public async Task<(bool Success, string Message, User? User)> RegisterAsync(RegisterRequestDto dto)
    {
        return await RegisterInternalAsync(dto, dto.Role, createCustomerRecord: dto.Role == UserRole.Customer);
    }

    public async Task<(bool Success, string Message, User? User)> RegisterCustomerAsync(RegisterRequestDto dto)
    {
        return await RegisterInternalAsync(dto, UserRole.Customer, createCustomerRecord: true);
    }

    private async Task<(bool Success, string Message, User? User)> RegisterInternalAsync(RegisterRequestDto dto, UserRole role, bool createCustomerRecord)
    {
        if (await context.Users.AnyAsync(x => x.Username == dto.Username))
        {
            return (false, "Bu kullanici adi zaten kullaniliyor.", null);
        }

        if (await context.Users.AnyAsync(x => x.Email == dto.Email))
        {
            return (false, "Bu e-posta adresi zaten kayitli.", null);
        }

        Customer? customer = null;
        if (createCustomerRecord)
        {
            customer = new Customer
            {
                FullName = dto.FullName,
                Email = dto.Email,
                Phone = dto.Phone ?? string.Empty,
                Address = "Musteri panelinden kayit olusturuldu.",
                Notes = "Portal kullanicisi",
                CreatedAt = DateTime.UtcNow
            };
            context.Customers.Add(customer);
            await context.SaveChangesAsync();
        }

        var user = new User
        {
            FullName = dto.FullName,
            Username = dto.Username,
            Email = dto.Email,
            Phone = dto.Phone,
            Role = role,
            CustomerId = customer?.Id,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = hasher.HashPassword(user, dto.Password);
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return (true, "Kullanici kaydi olusturuldu.", user);
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

        user.PasswordHash = hasher.HashPassword(user, newPassword);
        await context.SaveChangesAsync();
        return (true, "Sifre guncellendi.");
    }
}
