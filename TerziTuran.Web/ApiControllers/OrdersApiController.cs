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
    public async Task<IActionResult> GetAll()
    {
        var user = await GetCurrentUserAsync();
        var query = context.Orders
            .Include(x => x.Customer)
            .Include(x => x.BagReceipts)
            .Include(x => x.Payments)
            .Include(x => x.OrderItems)
            .AsQueryable();

        if (user?.Role == UserRole.Customer)
        {
            if (user.CustomerId is null)
            {
                return Forbid();
            }

            query = query.Where(x => x.CustomerId == user.CustomerId.Value);
        }

        return Ok(ApiResponse<object>.Ok(await query.OrderByDescending(x => x.CreatedAt).ToListAsync(), "Siparisler getirildi."));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get(int id)
    {
        var user = await GetCurrentUserAsync();
        var item = await context.Orders
            .Include(x => x.OrderItems)
            .Include(x => x.Payments)
            .Include(x => x.Customer)
            .Include(x => x.BagReceipts)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (item is not null && user?.Role == UserRole.Customer && user.CustomerId != item.CustomerId)
        {
            return Forbid();
        }

        return item is null ? NotFound(ApiResponse<object>.Fail("Siparis bulunamadi.")) : Ok(ApiResponse<object>.Ok(item, "Siparis getirildi."));
    }

    [HttpPost]
    public async Task<IActionResult> Create(OrderDto dto)
    {
        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));

        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        var user = await GetCurrentUserAsync();

        var customerId = dto.CustomerId;
        var status = dto.Status;
        var priority = dto.Priority;
        var price = dto.Price;
        var paidAmount = dto.PaidAmount;
        var isCustomerRequest = false;

        if (user?.Role == UserRole.Customer)
        {
            if (user.CustomerId is null)
            {
                return Forbid();
            }

            customerId = user.CustomerId.Value;
            status = OrderStatus.Pending;
            priority = OrderPriority.Medium;
            price = 0;
            paidAmount = 0;
            isCustomerRequest = true;
        }

        var item = new Order
        {
            CustomerId = customerId,
            Title = dto.Title,
            Description = dto.Description,
            Category = dto.Category,
            ServiceType = dto.ServiceType,
            Status = status,
            Priority = priority,
            Price = price,
            PaidAmount = paidAmount,
            DeliveryDate = dto.DeliveryDate,
            BagCount = dto.BagCount is < 1 or > 20 ? 1 : dto.BagCount,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId,
            IsCustomerRequest = isCustomerRequest
        };

        context.Orders.Add(item);
        await context.SaveChangesAsync();
        return CreatedAtAction(nameof(Get), new { id = item.Id }, ApiResponse<object>.Ok(item, "Siparis olusturuldu."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, OrderDto dto)
    {
        if (User.IsInRole(UserRole.Customer.ToString()))
        {
            return Forbid();
        }

        if (!ModelState.IsValid) return BadRequest(ApiResponse<object>.Fail("Gecersiz veri.", ModelState));
        var item = await context.Orders.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Siparis bulunamadi."));
        item.CustomerId = dto.CustomerId; item.Title = dto.Title; item.Description = dto.Description; item.Category = dto.Category; item.ServiceType = dto.ServiceType; item.Status = dto.Status; item.Priority = dto.Priority; item.Price = dto.Price; item.PaidAmount = dto.PaidAmount; item.DeliveryDate = dto.DeliveryDate; item.BagCount = dto.BagCount;
        await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(item, "Siparis guncellendi."));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (User.IsInRole(UserRole.Customer.ToString()))
        {
            return Forbid();
        }

        var item = await context.Orders.FindAsync(id);
        if (item is null) return NotFound(ApiResponse<object>.Fail("Siparis bulunamadi."));
        context.Orders.Remove(item); await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Siparis silindi."));
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        return userId <= 0 ? null : await context.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }
}
