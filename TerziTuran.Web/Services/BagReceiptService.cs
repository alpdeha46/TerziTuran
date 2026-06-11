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
        var order = await context.Orders.FirstOrDefaultAsync(x => x.Id == orderId);
        if (order is null)
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

        if (order.BagCount != bagCount)
        {
            order.BagCount = bagCount;
        }

        var receiptNumber = await GenerateNextReceiptNumberAsync();
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
            BagCount = order.BagCount,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            BagNumber = bagNumber,
            ReceiptNumber = receiptNumber,
            PickupCode = pickupCode,
            IssuedAt = DateTime.Now,
            IsDelivered = false
        };

        context.BagReceipts.Add(receipt);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            throw new InvalidOperationException("Teslim fişi oluşturulurken numara çakıştı. Lütfen tekrar deneyin.");
        }

        return receipt;
    }

    public async Task<List<BagReceipt>> GetRecentAsync()
        => await context.BagReceipts.Include(x => x.Order).ThenInclude(x => x!.Customer)
            .OrderByDescending(x => x.IssuedAt)
            .Take(100)
            .ToListAsync();

    private async Task<string> GenerateNextReceiptNumberAsync()
    {
        var todayPrefix = DateTime.Now.ToString("yyyyMMdd");
        var prefix = $"TT-{todayPrefix}-";

        var todayNumbers = await context.BagReceipts
            .Where(x => x.ReceiptNumber.StartsWith(prefix))
            .Select(x => x.ReceiptNumber)
            .ToListAsync();

        var nextSequence = todayNumbers
            .Select(x =>
            {
                var suffix = x.Length > prefix.Length ? x[prefix.Length..] : string.Empty;
                return int.TryParse(suffix, out var value) ? value : 0;
            })
            .DefaultIfEmpty(0)
            .Max() + 1;

        return $"{prefix}{nextSequence:D3}";
    }
}
