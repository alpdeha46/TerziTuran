using Microsoft.EntityFrameworkCore;
using TerziTuran.Web.Data;
using TerziTuran.Web.Models;

namespace TerziTuran.Web.Services;

public interface INotificationService
{
    Task CreateForRolesAsync(IEnumerable<UserRole> roles, string title, string message, string type, int? orderId = null);
    Task CreateForCustomerAsync(int customerId, string title, string message, string type, int? orderId = null);
}

public class NotificationService(AppDbContext context, IPushNotificationSender pushNotificationSender) : INotificationService
{
    public async Task CreateForRolesAsync(IEnumerable<UserRole> roles, string title, string message, string type, int? orderId = null)
    {
        var roleSet = roles.Distinct().ToList();
        if (roleSet.Count == 0)
        {
            return;
        }

        var userIds = await context.Users
            .Where(x => x.IsActive && roleSet.Contains(x.Role))
            .Select(x => x.Id)
            .ToListAsync();

        await CreateAsync(userIds, title, message, type, orderId);
    }

    public async Task CreateForCustomerAsync(int customerId, string title, string message, string type, int? orderId = null)
    {
        var userIds = await context.Users
            .Where(x => x.IsActive && x.Role == UserRole.Customer && x.CustomerId == customerId)
            .Select(x => x.Id)
            .ToListAsync();

        await CreateAsync(userIds, title, message, type, orderId);
    }

    private async Task CreateAsync(IEnumerable<int> userIds, string title, string message, string type, int? orderId)
    {
        var distinctUserIds = userIds.Distinct().ToList();
        if (distinctUserIds.Count == 0)
        {
            return;
        }

        var notifications = distinctUserIds.Select(userId => new AppNotification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            OrderId = orderId,
            CreatedAt = DateTime.UtcNow
        });

        context.AppNotifications.AddRange(notifications);
        await context.SaveChangesAsync();
        await pushNotificationSender.SendToUsersAsync(distinctUserIds, title, message, type, orderId);
    }
}
