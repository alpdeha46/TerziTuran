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
[Route("api/payments")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class PaymentsApiController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var user = await GetCurrentUserAsync();
        var query = context.Payments.Include(x => x.Order).AsQueryable();
        if (user?.Role == UserRole.Customer)
        {
            if (user.CustomerId is null)
            {
                return Forbid();
            }

            query = query.Where(x => x.Order != null && x.Order.CustomerId == user.CustomerId.Value);
        }

        return Ok(ApiResponse<object>.Ok(await query.OrderByDescending(x => x.PaymentDate).ToListAsync(), "Odemeler getirildi."));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var user = await GetCurrentUserAsync();
        var item = await context.Payments.Include(x => x.Order).FirstOrDefaultAsync(x => x.Id == id);
        if (item is not null && user?.Role == UserRole.Customer && user.CustomerId != item.Order?.CustomerId)
        {
            return Forbid();
        }

        return item is { } ? Ok(ApiResponse<object>.Ok(item, "Odeme getirildi.")) : NotFound(ApiResponse<object>.Fail("Odeme bulunamadi."));
    }

    [HttpPost]
    public async Task<IActionResult> Create(PaymentDto dto)
    {
        if (User.IsInRole(UserRole.Customer.ToString()))
        {
            return Forbid();
        }

        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = new Payment { OrderId = dto.OrderId, Amount = dto.Amount, PaymentType = dto.PaymentType, PaymentDate = dto.PaymentDate, Note = dto.Note };
        context.Payments.Add(item);
        var order = await context.Orders.FindAsync(dto.OrderId);
        if (order is not null) order.PaidAmount += dto.Amount;
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<object>.Ok(item, "Odeme olusturuldu."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, PaymentDto dto)
    {
        if (User.IsInRole(UserRole.Customer.ToString()))
        {
            return Forbid();
        }

        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = await context.Payments.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Odeme bulunamadi."));
        var oldAmount = item.Amount;
        item.OrderId = dto.OrderId; item.Amount = dto.Amount; item.PaymentType = dto.PaymentType; item.PaymentDate = dto.PaymentDate; item.Note = dto.Note;
        var order = await context.Orders.FindAsync(dto.OrderId);
        if (order is not null) order.PaidAmount += dto.Amount - oldAmount;
        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(item, "Odeme guncellendi."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (User.IsInRole(UserRole.Customer.ToString()))
        {
            return Forbid();
        }

        var item = await context.Payments.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Odeme bulunamadi."));
        var order = await context.Orders.FindAsync(item.OrderId);
        if (order is not null) order.PaidAmount -= item.Amount;
        context.Payments.Remove(item); await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Odeme silindi."));
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        return userId <= 0 ? null : await context.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }
}
