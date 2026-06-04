using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/appointments")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AppointmentsApiController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(ApiResponse<object>.Ok(await context.Appointments.Include(x => x.Customer).Include(x => x.Order).ToListAsync(), "Randevular getirildi."));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => await context.Appointments.FindAsync(id) is { } item ? Ok(ApiResponse<object>.Ok(item, "Randevu getirildi.")) : NotFound(ApiResponse<object>.Fail("Randevu bulunamadi."));
    [HttpPost]
    public async Task<IActionResult> Create(AppointmentDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = new Appointment { CustomerId = dto.CustomerId, OrderId = dto.OrderId, AppointmentDate = dto.AppointmentDate, Title = dto.Title, Description = dto.Description, Status = dto.Status };
        context.Appointments.Add(item); await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<object>.Ok(item, "Randevu olusturuldu."));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, AppointmentDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = await context.Appointments.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Randevu bulunamadi."));
        item.CustomerId = dto.CustomerId; item.OrderId = dto.OrderId; item.AppointmentDate = dto.AppointmentDate; item.Title = dto.Title; item.Description = dto.Description; item.Status = dto.Status;
        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(item, "Randevu guncellendi."));
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await context.Appointments.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Randevu bulunamadi."));
        context.Appointments.Remove(item); await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Randevu silindi."));
    }
}
