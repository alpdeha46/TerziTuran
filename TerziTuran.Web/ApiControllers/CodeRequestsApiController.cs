using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Services;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/code-requests")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = "AdminOnly")]
public class CodeRequestsApiController(AppDbContext context, IAuthService authService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetActiveRequests()
    {
        var requests = await context.UserPasswordRequests
            .Include(x => x.User)
            .Where(x => !x.IsUsed && !x.IsDispatched && x.ExpiresAt >= DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new CodeRequestDto
            {
                Id = x.Id,
                UserId = x.UserId,
                FullName = x.User!.FullName,
                Username = x.User.Username,
                Email = x.User.Email,
                RequestType = x.RequestType.ToString(),
                Code = x.Code,
                CreatedAt = x.CreatedAt,
                ExpiresAt = x.ExpiresAt
            })
            .ToListAsync();

        return Ok(ApiResponse<List<CodeRequestDto>>.Ok(requests));
    }

    [HttpPost("{id:int}/dispatch")]
    public async Task<IActionResult> Dispatch(int id)
    {
        var result = await authService.DispatchCodeRequestAsync(id);
        if (!result.Success || result.Request is null)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message));
        }

        return Ok(ApiResponse<object>.Ok(new { code = result.Request.Code }, result.Message));
    }
}
