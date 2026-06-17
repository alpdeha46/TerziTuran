using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Models;
using System.Security.Claims;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/measurements")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class MeasurementsApiController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? customerId = null)
    {
        var user = await GetCurrentUserAsync();
        var query = context.Measurements.Include(x => x.Customer).AsQueryable();

        if (user?.Role == UserRole.Customer)
        {
            if (user.CustomerId is null)
            {
                return Forbid();
            }

            query = query.Where(x => x.CustomerId == user.CustomerId.Value);
        }
        else if (customerId.HasValue)
        {
            query = query.Where(x => x.CustomerId == customerId.Value);
        }

        return Ok(ApiResponse<object>.Ok(await query.OrderByDescending(x => x.CreatedAt).ToListAsync(), "Olculer getirildi."));
    }
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => await context.Measurements.FindAsync(id) is { } item ? Ok(ApiResponse<object>.Ok(item, "Olcu getirildi.")) : NotFound(ApiResponse<object>.Fail("Olcu bulunamadi."));
    [HttpPost]
    public async Task<IActionResult> Create(MeasurementDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = new Measurement { CustomerId = dto.CustomerId, Chest = dto.Chest, Waist = dto.Waist, Hip = dto.Hip, Shoulder = dto.Shoulder, Sleeve = dto.Sleeve, Inseam = dto.Inseam, Neck = dto.Neck, Height = dto.Height, Weight = dto.Weight, Notes = dto.Notes, CreatedAt = DateTime.UtcNow };
        context.Measurements.Add(item); await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<object>.Ok(item, "Olcu olusturuldu."));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, MeasurementDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = await context.Measurements.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Olcu bulunamadi."));
        item.CustomerId = dto.CustomerId; item.Chest = dto.Chest; item.Waist = dto.Waist; item.Hip = dto.Hip; item.Shoulder = dto.Shoulder; item.Sleeve = dto.Sleeve; item.Inseam = dto.Inseam; item.Neck = dto.Neck; item.Height = dto.Height; item.Weight = dto.Weight; item.Notes = dto.Notes;
        await context.SaveChangesAsync(); return Ok(ApiResponse<object>.Ok(item, "Olcu guncellendi."));
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await context.Measurements.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Olcu bulunamadi."));
        context.Measurements.Remove(item); await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Olcu silindi."));
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        return userId <= 0 ? null : await context.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }
}
