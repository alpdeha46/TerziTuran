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
    public async Task<IActionResult> GetAll() => Ok(ApiResponse<object>.Ok(await context.Payments.Include(x => x.Order).ToListAsync(), "Odemeler getirildi."));
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id) => await context.Payments.FindAsync(id) is { } item ? Ok(ApiResponse<object>.Ok(item, "Odeme getirildi.")) : NotFound(ApiResponse<object>.Fail("Odeme bulunamadi."));
    [HttpPost]
    public async Task<IActionResult> Create(PaymentDto dto)
    {
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
        var item = await context.Payments.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Odeme bulunamadi."));
        var order = await context.Orders.FindAsync(item.OrderId);
        if (order is not null) order.PaidAmount -= item.Amount;
        context.Payments.Remove(item); await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Odeme silindi."));
    }
}
