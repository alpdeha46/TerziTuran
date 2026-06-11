using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Services;
using TerziTuran.Web.ViewModels;

namespace TerziTuran.Web.Controllers;

[Authorize(Policy = "AdminOnly")]
public class CodeRequestsController(AppDbContext context, IAuthService authService) : Controller
{
    public async Task<IActionResult> Index()
    {
        var model = new CodeRequestsViewModel
        {
            Requests = await context.UserPasswordRequests
                .AsNoTracking()
                .Include(x => x.User)
                .Where(x => !x.IsUsed && !x.IsDispatched && x.ExpiresAt >= DateTime.UtcNow)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Dispatch(int id)
    {
        var result = await authService.DispatchCodeRequestAsync(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }
}
