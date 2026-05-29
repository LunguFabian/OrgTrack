using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Infrastructure.Persistence;

public class NotificationRepository(OrgTrackDbContext context) : INotificationRepository
{
    public async Task AddAsync(Notification notification)
    {
        context.Notifications.Add(notification);
        await context.SaveChangesAsync();
    }

    public async Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int limit = 50)
    {
        return await context.Notifications
            .Include(n => n.Actor)
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .CountAsync();
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        var notification = await context.Notifications.FindAsync(notificationId);
        if (notification != null)
        {
            notification.IsRead = true;
            await context.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}
