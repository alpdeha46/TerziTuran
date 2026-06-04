using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Services;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthApiController(IAuthService authService, IJwtService jwtService) : ControllerBase
{
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz istek.", ModelState));
        var user = await authService.ValidateUserAsync(dto.Username, dto.Password);
        if (user is null) return Unauthorized(ApiResponse<object>.Fail("Kullanici adi veya sifre hatali."));
        return Ok(ApiResponse<AuthResultDto>.Ok(jwtService.CreateToken(user), "Giris basarili."));
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz istek.", ModelState));
        var result = await authService.RegisterAsync(dto);
        if (!result.Success || result.User is null) return BadRequest(ApiResponse<object>.Fail(result.Message));
        return Ok(ApiResponse<AuthResultDto>.Ok(jwtService.CreateToken(result.User), "Kayit basarili."));
    }
}
