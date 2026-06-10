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
[Route("api/orders")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class OrdersApiController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(ApiResponse<object>.Ok(await context.Orders.Include(x => x.Customer).Include(x => x.BagReceipts).ToListAsync(), "Siparisler getirildi."));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var item = await context.Orders.Include(x => x.OrderItems).Include(x => x.Payments).Include(x => x.Customer).FirstOrDefaultAsync(x => x.Id == id);
        return item is null ? NotFound(ApiResponse<object>.Fail("Siparis bulunamadi.")) : Ok(ApiResponse<object>.Ok(item, "Siparis getirildi."));
    }
    [HttpPost]
    public async Task<IActionResult> Create(OrderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 1;
        var item = new Order { CustomerId = dto.CustomerId, Title = dto.Title, Description = dto.Description, Category = dto.Category, ServiceType = dto.ServiceType, Status = dto.Status, Priority = dto.Priority, Price = dto.Price, PaidAmount = dto.PaidAmount, DeliveryDate = dto.DeliveryDate, CreatedAt = DateTime.UtcNow, CreatedByUserId = userId };
        context.Orders.Add(item); await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<object>.Ok(item, "Siparis olusturuldu."));
    }
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, OrderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = await context.Orders.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Siparis bulunamadi."));
        item.CustomerId = dto.CustomerId; item.Title = dto.Title; item.Description = dto.Description; item.Category = dto.Category; item.ServiceType = dto.ServiceType; item.Status = dto.Status; item.Priority = dto.Priority; item.Price = dto.Price; item.PaidAmount = dto.PaidAmount; item.DeliveryDate = dto.DeliveryDate;
        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(item, "Siparis guncellendi."));
    }
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await context.Orders.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Siparis bulunamadi."));
        context.Orders.Remove(item); await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Siparis silindi."));
    }
}
