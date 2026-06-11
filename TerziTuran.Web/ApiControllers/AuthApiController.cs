using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Services;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/auth")]
[AllowAnonymous]
public class AuthApiController(IAuthService authService, IJwtService jwtService) : ControllerBase
{
    [HttpPost("login")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Login(LoginRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz istek.", ModelState));
        var user = await authService.ValidateUserAsync(dto.Username, dto.Password);
        if (user is null) return Unauthorized(ApiResponse<object>.Fail("Kullanici adi veya sifre hatali."));
        if (user.MustChangePassword)
        {
            await authService.CreateActivationRequestAsync(user);
            return Unauthorized(ApiResponse<ActivationRequiredDto>.Fail(
                "Aktivasyon kodu gerekli. Yoneticiden tek kullanimlik kod alip sifrenizi yenileyin.",
                new ActivationRequiredDto
                {
                    RequiresActivation = true,
                    UserId = user.Id,
                    Username = user.Username
                }));
        }
        return Ok(ApiResponse<AuthResultDto>.Ok(jwtService.CreateToken(user), "Giris basarili."));
    }

    [HttpPost("register")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> Register(RegisterRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz istek.", ModelState));
        var result = await authService.RegisterCustomerAsync(dto);
        if (!result.Success || result.User is null) return BadRequest(ApiResponse<object>.Fail(result.Message));
        return Ok(ApiResponse<ActivationRequiredDto>.Ok(
            new ActivationRequiredDto
            {
                RequiresActivation = true,
                UserId = result.User.Id,
                Username = result.User.Username
            },
            "Kayit alindi. Aktivasyon kodu yonetici taleplerine gonderildi."));
    }

    [HttpPost("forgot-password")]
    [EnableRateLimiting("register")]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz istek.", ModelState));
        var result = await authService.StartForgotPasswordAsync(dto.Email);
        return Ok(ApiResponse<object>.Ok(null, result.Message));
    }

    [HttpPost("activate")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> Activate(CompleteActivationRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz istek.", ModelState));
        var result = dto.UserId.HasValue && dto.UserId.Value > 0
            ? await authService.CompleteActivationAsync(dto.UserId.Value, dto.Code, dto.NewPassword)
            : await authService.CompleteActivationAsync(dto.Username, dto.Code, dto.NewPassword);
        if (!result.Success || result.User is null) return BadRequest(ApiResponse<object>.Fail(result.Message));
        return Ok(ApiResponse<AuthResultDto>.Ok(jwtService.CreateToken(result.User), "Aktivasyon tamamlandi."));
    }

    [HttpPost("reset-password")]
    [EnableRateLimiting("login")]
    public async Task<IActionResult> ResetPassword(ResetPasswordWithCodeRequestDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz istek.", ModelState));
        var result = await authService.CompleteForgotPasswordAsync(dto.Email, dto.Code, dto.NewPassword);
        if (!result.Success) return BadRequest(ApiResponse<object>.Fail(result.Message));
        return Ok(ApiResponse<object>.Ok(null, result.Message));
    }
}
