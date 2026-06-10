using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.Services;

public interface IBagReceiptService
{
    Task<BagReceipt> CreateAsync(int orderId, int bagCount, string? note);
    Task<List<BagReceipt>> GetRecentAsync();
}

public class BagReceiptService(AppDbContext context) : IBagReceiptService
{
    public async Task<BagReceipt> CreateAsync(int orderId, int bagCount, string? note)
    {
        if (!await context.Orders.AnyAsync(x => x.Id == orderId))
        {
            throw new InvalidOperationException("Fiş atanacak sipariş bulunamadı.");
        }

        if (await context.BagReceipts.AnyAsync(x => x.OrderId == orderId && !x.IsDelivered))
        {
            throw new InvalidOperationException("Bu siparişe ait aktif bir teslim fişi zaten bulunuyor.");
        }

        if (bagCount is < 1 or > 20)
        {
            throw new InvalidOperationException("Poşet adedi 1 ile 20 arasında olmalıdır.");
        }

        var todayPrefix = DateTime.Now.ToString("yyyyMMdd");
        var countToday = await context.BagReceipts.CountAsync(x => x.IssuedAt.Date == DateTime.Today);
        var receiptNumber = $"TT-{todayPrefix}-{(countToday + 1):D3}";
        var activeNumbers = await context.BagReceipts
            .Where(x => !x.IsDelivered)
            .Select(x => x.BagNumber)
            .OrderBy(x => x)
            .ToListAsync();

        var bagNumber = 1;
        foreach (var number in activeNumbers)
        {
            if (number == bagNumber)
            {
                bagNumber++;
                continue;
            }

            if (number > bagNumber)
            {
                break;
            }
        }

        var pickupCode = Random.Shared.Next(1000, 9999).ToString();

        while (await context.BagReceipts.AnyAsync(x => x.PickupCode == pickupCode && !x.IsDelivered))
        {
            pickupCode = Random.Shared.Next(1000, 9999).ToString();
        }

        var receipt = new BagReceipt
        {
            OrderId = orderId,
            BagCount = bagCount,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            BagNumber = bagNumber,
            ReceiptNumber = receiptNumber,
            PickupCode = pickupCode,
            IssuedAt = DateTime.Now,
            IsDelivered = false
        };

        context.BagReceipts.Add(receipt);
        await context.SaveChangesAsync();
        return receipt;
    }

    public async Task<List<BagReceipt>> GetRecentAsync()
        => await context.BagReceipts.Include(x => x.Order).ThenInclude(x => x!.Customer)
            .OrderByDescending(x => x.IssuedAt)
            .Take(100)
            .ToListAsync();
}
