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
[Route("api/customers")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class CustomersApiController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var user = await GetCurrentUserAsync();
        if (user?.Role == UserRole.Customer)
        {
            if (user.CustomerId is null)
            {
                return Forbid();
            }

            var customer = await context.Customers.Where(x => x.Id == user.CustomerId.Value).ToListAsync();
            return Ok(ApiResponse<object>.Ok(customer, "Musteri bilgileri getirildi."));
        }

        return Ok(ApiResponse<object>.Ok(await context.Customers.ToListAsync(), "Musteriler getirildi."));
    }

    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await GetCurrentUserAsync();
        if (user?.CustomerId is null)
        {
            return Forbid();
        }

        var customer = await context.Customers.FindAsync(user.CustomerId.Value);
        return customer is null
            ? NotFound(ApiResponse<object>.Fail("Musteri bulunamadi."))
            : Ok(ApiResponse<object>.Ok(customer, "Musteri bilgileri getirildi."));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user?.Role == UserRole.Customer && user.CustomerId != id)
        {
            return Forbid();
        }

        var item = await context.Customers.FindAsync(id);
        return item is null ? NotFound(ApiResponse<object>.Fail("Musteri bulunamadi.")) : Ok(ApiResponse<object>.Ok(item, "Musteri getirildi."));
    }

    [HttpPost]
    public async Task<IActionResult> Create(CustomerDto dto)
    {
        if (User.IsInRole(UserRole.Customer.ToString()))
        {
            return Forbid();
        }

        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = new Customer { FullName = dto.FullName, Phone = dto.Phone, Email = dto.Email, Address = dto.Address, Notes = dto.Notes, CreatedAt = DateTime.UtcNow };
        context.Customers.Add(item);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<object>.Ok(item, "Musteri olusturuldu."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, CustomerDto dto)
    {
        if (User.IsInRole(UserRole.Customer.ToString()))
        {
            return Forbid();
        }

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
        if (User.IsInRole(UserRole.Customer.ToString()))
        {
            return Forbid();
        }

        var item = await context.Customers.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Musteri bulunamadi."));
        context.Customers.Remove(item);
        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Musteri silindi."));
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        return userId <= 0 ? null : await context.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }
}
