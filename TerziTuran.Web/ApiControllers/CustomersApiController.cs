using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/customers")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CustomersApiController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(ApiResponse<object>.Ok(await context.Customers.ToListAsync(), "Musteriler getirildi."));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await context.Customers.FindAsync(id);
        return item is null ? NotFound(ApiResponse<object>.Fail("Musteri bulunamadi.")) : Ok(ApiResponse<object>.Ok(item, "Musteri getirildi."));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = new Customer { FullName = dto.FullName, Phone = dto.Phone, Email = dto.Email, Address = dto.Address, Notes = dto.Notes, CreatedAt = DateTime.UtcNow };
        context.Customers.Add(item);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<object>.Ok(item, "Musteri olusturuldu."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CustomerDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = await context.Customers.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Musteri bulunamadi."));
        item.FullName = dto.FullName; item.Phone = dto.Phone; item.Email = dto.Email; item.Address = dto.Address; item.Notes = dto.Notes;
        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(item, "Musteri guncellendi."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await context.Customers.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Musteri bulunamadi."));
        context.Customers.Remove(item);
        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Musteri silindi."));
    }
}
