using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Models;
using TerziTuran.Web.Services;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[AllowAnonymous]
public class AuthController(IAuthService authService, AppDbContext context) : Controller
{
    [HttpGet]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await authService.ValidateUserAsync(model.Username, model.Password);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Kullanici adi veya sifre hatali.");
            return View(model);
        }

        if (user.MustChangePassword)
        {
            var activationResult = await authService.CreateActivationRequestAsync(user);
            if (!activationResult.Success)
            {
                ModelState.AddModelError(string.Empty, activationResult.Message);
                return View(model);
            }

            TempData["Success"] = "Tek kullanimlik aktivasyon kodu olusturuldu. Kodu yoneticiden alip yeni sifrenizi belirleyin.";
            return RedirectToAction(nameof(CompleteActivation), new { userId = user.Id, username = user.Username });
        }

        await SignInAsync(user);
        TempData["Success"] = "Hos geldiniz.";
        if (!string.IsNullOrWhiteSpace(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            return Redirect(model.ReturnUrl);
        }

        return user.Role == UserRole.Customer
            ? RedirectToAction("Index", "CustomerPortal")
            : RedirectToAction("Index", "Dashboard");
    }

    [HttpGet]
    public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await authService.RegisterCustomerAsync(new RegisterRequestDto
        {
            FullName = model.FullName,
            Username = model.Username,
            Email = model.Email,
            Phone = model.Phone
        });

        if (!result.Success || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["Success"] = "Kaydiniz alindi. Kod ekranina gecip aktivasyon kodunu girdiginizde hesabiniza dogrudan giris yapacaksiniz.";
        return RedirectToAction(nameof(CompleteActivation), new { userId = result.User.Id, username = result.User.Username });
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var result = await authService.StartForgotPasswordAsync(model.Email);
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(ResetPassword), new { email = model.Email });
    }

    [HttpGet]
    public async Task<IActionResult> CompleteActivation(int? userId = null, string? username = null)
    {
        if (userId.HasValue)
        {
            var user = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId.Value && x.IsActive);
            if (user is null)
            {
                TempData["Error"] = "Aktivasyon bekleyen kullanici bulunamadi.";
                return RedirectToAction(nameof(Login));
            }

            return View(new CompleteActivationViewModel
            {
                UserId = user.Id,
                Username = user.Username
            });
        }

        return View(new CompleteActivationViewModel
        {
            UserId = userId,
            Username = username ?? string.Empty
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> CompleteActivation(CompleteActivationViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = model.UserId.HasValue && model.UserId.Value > 0
            ? await authService.CompleteActivationAsync(model.UserId.Value, model.Code, model.NewPassword)
            : await authService.CompleteActivationAsync(model.Username, model.Code, model.NewPassword);
        if (!result.Success || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        await SignInAsync(result.User);
        TempData["Success"] = "Aktivasyon tamamlandi. Yeni sifrenizle giris yapildi.";
        return RedirectToAction(result.User.Role == UserRole.Customer ? "Index" : "Index", result.User.Role == UserRole.Customer ? "CustomerPortal" : "Dashboard");
    }

    [HttpGet]
    public IActionResult ResetPassword(string? email = null) => View(new ResetPasswordWithCodeViewModel { Email = email ?? string.Empty });

    [HttpPost]
    [ValidateAntiForgeryToken]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> ResetPassword(ResetPasswordWithCodeViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await authService.CompleteForgotPasswordAsync(model.Email, model.Code, model.NewPassword);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    private async Task SignInAsync(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.Username),
            new(ClaimTypes.Role, user.Role.ToString()),
            new("FullName", user.FullName)
        };

        var principal = new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
    }
}
