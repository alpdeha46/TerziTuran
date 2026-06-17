using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/notifications")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class NotificationsApiController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] bool unreadOnly = false)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return Unauthorized(ApiResponse<object>.Fail("Oturum bilgisi bulunamadi."));
        }

        var query = context.AppNotifications
            .Where(x => x.UserId == userId);

        if (unreadOnly)
        {
            query = query.Where(x => !x.IsRead);
        }

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(50)
            .ToListAsync();

        return Ok(ApiResponse<object>.Ok(items, "Bildirimler getirildi."));
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id)
    {
        var userId = GetCurrentUserId();
        var item = await context.AppNotifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (item is null)
        {
            return NotFound(ApiResponse<object>.Fail("Bildirim bulunamadi."));
        }

        if (!item.IsRead)
        {
            item.IsRead = true;
            item.ReadAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        return Ok(ApiResponse<object>.Ok(item, "Bildirim okundu olarak isaretlendi."));
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead()
    {
        var userId = GetCurrentUserId();
        var items = await context.AppNotifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ToListAsync();

        foreach (var item in items)
        {
            item.IsRead = true;
            item.ReadAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(new { updatedCount = items.Count }, "Tum bildirimler okundu olarak isaretlendi."));
    }

    private int GetCurrentUserId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
