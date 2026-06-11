using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Services;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[Authorize]
public class ProfileController(AppDbContext context, IAuthService authService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await context.Users.FindAsync(userId);
        return user is null ? RedirectToAction("Login", "Auth") : View(user);
    }

    public async Task<IActionResult> Edit()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await context.Users.FindAsync(userId);
        return user is null ? RedirectToAction("Login", "Auth") : View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Models.User model)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var existing = await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Id == userId);
        if (existing is null) return RedirectToAction("Login", "Auth");
        model.Id = userId;
        model.Username = existing.Username;
        model.Role = existing.Role;
        model.PasswordHash = existing.PasswordHash;
        model.CustomerId = existing.CustomerId;
        model.CreatedAt = existing.CreatedAt;
        model.IsActive = existing.IsActive;
        model.MustChangePassword = existing.MustChangePassword;
        if (!ModelState.IsValid) return View(model);
        context.Update(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Profil guncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await authService.ChangePasswordAsync(userId, model.CurrentPassword, model.NewPassword);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }
        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
