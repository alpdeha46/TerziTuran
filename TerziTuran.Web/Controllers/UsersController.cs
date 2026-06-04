using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class UsersController(AppDbContext context, IPasswordHasher<User> hasher) : Controller
{
    public async Task<IActionResult> Index() => View(await context.Users.OrderBy(x => x.FullName).ToListAsync());

    public IActionResult Create() => View(new User());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User model, string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 6)
        {
            ModelState.AddModelError(string.Empty, "Sifre en az 6 karakter olmalidir.");
        }

        if (!ModelState.IsValid) return View(model);

        model.PasswordHash = hasher.HashPassword(model, password);
        context.Users.Add(model);
        await context.SaveChangesAsync();
        TempData["Success"] = "Kullanici olusturuldu.";
        return RedirectToAction(nameof(Index));
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
        if (!ModelState.IsValid) return View(model);
        var existing = await context.Users.AsNoTracking().FirstAsync(x => x.Id == id);
        model.PasswordHash = existing.PasswordHash;
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
        user.IsActive = !user.IsActive;
        await context.SaveChangesAsync();
        TempData["Success"] = "Kullanici durumu guncellendi.";
        return RedirectToAction(nameof(Index));
    }
}
