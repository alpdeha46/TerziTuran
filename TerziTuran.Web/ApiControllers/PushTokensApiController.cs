using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/push-tokens")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PushTokensApiController(AppDbContext context) : ControllerBase
{
    [HttpPost("register")]
    public async Task<IActionResult> Register(PushTokenRegisterDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ApiResponse<object>.Fail("Gecersiz push token bilgileri.", ModelState));
        }

        var userId = GetCurrentUserId();
        if (userId <= 0)
        {
            return Unauthorized(ApiResponse<object>.Fail("Oturum bilgisi bulunamadi."));
        }

        var token = await context.UserPushTokens
            .FirstOrDefaultAsync(x => x.Token == dto.Token);

        if (token is null)
        {
            token = new UserPushToken
            {
                UserId = userId,
                Token = dto.Token,
                Platform = dto.Platform,
                DeviceName = dto.DeviceName,
                CreatedAt = DateTime.UtcNow,
                LastSeenAt = DateTime.UtcNow,
                IsActive = true
            };
            context.UserPushTokens.Add(token);
        }
        else
        {
            token.UserId = userId;
            token.Platform = dto.Platform;
            token.DeviceName = dto.DeviceName;
            token.IsActive = true;
            token.LastSeenAt = DateTime.UtcNow;
        }

        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Push token kaydedildi."));
    }

    [HttpDelete("unregister")]
    public async Task<IActionResult> Unregister([FromQuery] string token)
    {
        var userId = GetCurrentUserId();
        var entity = await context.UserPushTokens
            .FirstOrDefaultAsync(x => x.UserId == userId && x.Token == token);

        if (entity is null)
        {
            return Ok(ApiResponse<object>.Ok(null, "Push token bulunamadi."));
        }

        entity.IsActive = false;
        entity.LastSeenAt = DateTime.UtcNow;
        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Push token kapatildi."));
    }

    private int GetCurrentUserId()
        => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
