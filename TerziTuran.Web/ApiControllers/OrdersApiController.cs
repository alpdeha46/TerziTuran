using System.Security.Claims;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.DTOs;
using TerziTuran.Web.Models;
using TerziTuran.Web.Services;

namespace TerziTuran.Web.ApiControllers;

[ApiController]
[Route("api/orders")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class OrdersApiController(AppDbContext context, INotificationService notificationService, IWebHostEnvironment environment) : ControllerBase
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
            PhotoPath = SavePhoto(dto.PhotoBase64, dto.PhotoFileName),
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

        if (isCustomerRequest)
        {
            var customerName = await context.Customers
                .Where(x => x.Id == customerId)
                .Select(x => x.FullName)
                .FirstOrDefaultAsync()
                ?? "Musteri";

            await notificationService.CreateForRolesAsync(
                [UserRole.Admin, UserRole.Staff],
                "Yeni siparis talebi",
                $"{customerName} tarafindan \"{item.Title}\" baslikli yeni siparis olusturuldu.",
                "order_created",
                item.Id);
        }

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
        var previousStatus = item.Status;
        item.CustomerId = dto.CustomerId; item.Title = dto.Title; item.Description = dto.Description; item.Category = dto.Category; item.ServiceType = dto.ServiceType; item.Status = dto.Status; item.Priority = dto.Priority; item.Price = dto.Price; item.PaidAmount = dto.PaidAmount; item.DeliveryDate = dto.DeliveryDate; item.BagCount = dto.BagCount;
        if (!string.IsNullOrWhiteSpace(dto.PhotoBase64))
        {
            DeletePhotoIfExists(item.PhotoPath);
            item.PhotoPath = SavePhoto(dto.PhotoBase64, dto.PhotoFileName);
        }
        await context.SaveChangesAsync();

        if (previousStatus != item.Status)
        {
            var notification = BuildCustomerStatusNotification(item);
            if (notification is not null)
            {
                await notificationService.CreateForCustomerAsync(
                    item.CustomerId,
                    notification.Value.Title,
                    notification.Value.Message,
                    notification.Value.Type,
                    item.Id);
            }
        }

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
        DeletePhotoIfExists(item.PhotoPath);
        context.Orders.Remove(item); await context.SaveChangesAsync();
        return Ok(ApiResponse<object>.Ok(null, "Siparis silindi."));
    }

    private async Task<User?> GetCurrentUserAsync()
    {
        var userId = int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
        return userId <= 0 ? null : await context.Users.FirstOrDefaultAsync(x => x.Id == userId);
    }

    private static (string Title, string Message, string Type)? BuildCustomerStatusNotification(Order order)
        => order.Status switch
        {
            OrderStatus.Pending => (
                "Siparisiniz alindi",
                $"\"{order.Title}\" siparisiniz beklemeye alindi.",
                "order_pending"),
            OrderStatus.Measured => (
                "Siparisiniz olcu asamasinda",
                $"\"{order.Title}\" siparisinizin olculeri alindi.",
                "order_measured"),
            OrderStatus.Sewing => (
                "Siparisiniz dikimde",
                $"\"{order.Title}\" siparisiniz uretim asamasina gecti.",
                "order_sewing"),
            OrderStatus.Fitting => (
                "Siparisiniz prova asamasinda",
                $"\"{order.Title}\" siparisiniz prova surecine girdi.",
                "order_fitting"),
            OrderStatus.Ready => (
                "Siparisiniz hazir",
                $"\"{order.Title}\" siparisiniz tamamlandi ve teslime hazir.",
                "order_ready"),
            OrderStatus.Delivered => (
                "Siparisiniz teslim edildi",
                $"\"{order.Title}\" siparisiniz teslim edildi olarak isaretlendi.",
                "order_delivered"),
            OrderStatus.Cancelled => (
                "Siparisiniz iptal edildi",
                $"\"{order.Title}\" siparisiniz iptal edildi.",
                "order_cancelled"),
            _ => null
        };

    private string? SavePhoto(string? photoBase64, string? fileName)
    {
        if (string.IsNullOrWhiteSpace(photoBase64))
        {
            return null;
        }

        try
        {
            var uploadsPath = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), "uploads", "orders");
            Directory.CreateDirectory(uploadsPath);

            var normalized = photoBase64;
            var extension = ".jpg";
            var match = Regex.Match(photoBase64, @"^data:image/(?<ext>[a-zA-Z0-9+]+);base64,(?<data>.+)$");
            if (match.Success)
            {
                var ext = match.Groups["ext"].Value.ToLowerInvariant();
                extension = ext switch
                {
                    "png" => ".png",
                    "webp" => ".webp",
                    _ => ".jpg"
                };
                normalized = match.Groups["data"].Value;
            }
            else if (!string.IsNullOrWhiteSpace(fileName))
            {
                var candidate = Path.GetExtension(fileName);
                if (!string.IsNullOrWhiteSpace(candidate))
                {
                    extension = candidate.ToLowerInvariant();
                }
            }

            var bytes = Convert.FromBase64String(normalized);
            var safeFileName = $"order_{DateTime.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
            var fullPath = Path.Combine(uploadsPath, safeFileName);
            System.IO.File.WriteAllBytes(fullPath, bytes);
            return $"/uploads/orders/{safeFileName}";
        }
        catch
        {
            return null;
        }
    }

    private void DeletePhotoIfExists(string? photoPath)
    {
        if (string.IsNullOrWhiteSpace(photoPath))
        {
            return;
        }

        var relative = photoPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot"), relative);
        if (System.IO.File.Exists(fullPath))
        {
            System.IO.File.Delete(fullPath);
        }
    }
}
