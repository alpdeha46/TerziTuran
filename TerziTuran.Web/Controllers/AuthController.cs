using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Models;
using TerziTuran.Web.Services;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[AllowAnonymous]
public class AuthController(IAuthService authService) : Controller
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
            Password = model.Password,
            Phone = model.Phone
        });

        if (!result.Success || result.User is null)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        await SignInAsync(result.User);
        TempData["Success"] = "Hesabiniz guvenle olusturuldu. Hos geldiniz.";
        return RedirectToAction("Index", "CustomerPortal");
    }

    [HttpGet]
    public IActionResult ForgotPassword() => View(new ForgotPasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        TempData["Success"] = "Bu e-posta sistemde kayitliysa sifre yenileme talebi guvenli sekilde isleme alinmistir.";
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
