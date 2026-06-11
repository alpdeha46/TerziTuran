using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Models;
using TerziTuran.Web.Services;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class UsersController(AppDbContext context, IAuthService authService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new UsersIndexViewModel
        {
            Users = await context.Users.OrderBy(x => x.FullName).ToListAsync(),
        };

        return View(model);
    }

    public IActionResult Create() => View(new User());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User model)
    {
        ModelState.Remove(nameof(TerziTuran.Web.Models.User.PasswordHash));
        if (!ModelState.IsValid) return View(model);

        var result = await authService.CreateUserAsync(model);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return View(model);
        }

        TempData["Success"] = result.Message;
        return RedirectToAction("Index", "CodeRequests");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var user = await context.Users.FindAsync(id);
        return user is null ? NotFound() : View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, User model)
    {
        if (id != model.Id) return NotFound();
        ModelState.Remove(nameof(TerziTuran.Web.Models.User.PasswordHash));
        if (!ModelState.IsValid) return View(model);
        var existing = await context.Users.AsNoTracking().FirstAsync(x => x.Id == id);
        model.PasswordHash = existing.PasswordHash;
        model.Username = existing.Username;
        model.CustomerId = existing.CustomerId;
        model.CreatedAt = existing.CreatedAt;
        model.MustChangePassword = existing.MustChangePassword;
        model.Email = model.Email.Trim().ToLowerInvariant();
        if (await context.Users.AnyAsync(x => x.Id != id && x.Email == model.Email))
        {
            ModelState.AddModelError(nameof(model.Email), "Bu e-posta zaten kullaniliyor.");
            return View(model);
        }
        context.Update(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Kullanici bilgileri guncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var user = await context.Users.FindAsync(id);
        return user is null ? NotFound() : View(user);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await context.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (user.Username == User.Identity?.Name)
        {
            TempData["Error"] = "Kendi hesabinizi silemezsiniz.";
            return RedirectToAction(nameof(Index));
        }
        if (user.Role == UserRole.Admin && await context.Users.CountAsync(x => x.Role == UserRole.Admin && x.IsActive) <= 1)
        {
            TempData["Error"] = "Son aktif yonetici hesabi silinemez.";
            return RedirectToAction(nameof(Index));
        }
        context.Users.Remove(user);
        await context.SaveChangesAsync();
        TempData["Success"] = "Kullanici silindi.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
    {
        var user = await context.Users.FindAsync(id);
        if (user is null) return NotFound();
        if (user.Username == User.Identity?.Name)
        {
            TempData["Error"] = "Kendi hesabinizi pasif duruma alamazsiniz.";
            return RedirectToAction(nameof(Index));
        }
        if (user.Role == UserRole.Admin && user.IsActive && await context.Users.CountAsync(x => x.Role == UserRole.Admin && x.IsActive) <= 1)
        {
            TempData["Error"] = "Son aktif yonetici hesabi pasif yapilamaz.";
            return RedirectToAction(nameof(Index));
        }
        user.IsActive = !user.IsActive;
        await context.SaveChangesAsync();
        TempData["Success"] = "Kullanici durumu guncellendi.";
        return RedirectToAction(nameof(Index));
    }
}
