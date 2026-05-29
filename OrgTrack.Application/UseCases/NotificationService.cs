using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.UseCases;

public class NotificationService(
    INotificationRepository notificationRepository,
    IRealtimeNotifier realtimeNotifier)
{
    public async Task<Notification> CreateAndSendAsync(
        Guid userId, string type, string title, string message,
        Guid? relatedEntityId = null, string? relatedEntityType = null, Guid? actorId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Type = type,
            Title = title,
            Message = message,
            RelatedEntityId = relatedEntityId,
            RelatedEntityType = relatedEntityType,
            ActorId = actorId
        };

        await notificationRepository.AddAsync(notification);

        // Send real-time notification via SignalR
        var unreadCount = await notificationRepository.GetUnreadCountAsync(userId);
        await realtimeNotifier.SendToUserAsync(userId, "ReceiveNotification", new
        {
            notification.Id,
            notification.Type,
            notification.Title,
            notification.Message,
            notification.RelatedEntityId,
            notification.RelatedEntityType,
            notification.CreatedAt,
            notification.IsRead,
            UnreadCount = unreadCount
        });

        return notification;
    }

    public async Task<IEnumerable<object>> GetUserNotificationsAsync(Guid userId, int limit = 50)
    {
        var notifications = await notificationRepository.GetByUserIdAsync(userId, limit);
        return notifications.Select(n => new
        {
            n.Id,
            n.Type,
            n.Title,
            n.Message,
            n.RelatedEntityId,
            n.RelatedEntityType,
            n.IsRead,
            n.CreatedAt,
            ActorName = n.Actor != null ? $"{n.Actor.FirstName} {n.Actor.LastName}".Trim() : null,
            ActorPictureUrl = n.Actor?.PictureUrl
        });
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await notificationRepository.GetUnreadCountAsync(userId);
    }

    public async Task MarkAsReadAsync(Guid notificationId)
    {
        await notificationRepository.MarkAsReadAsync(notificationId);
    }

    public async Task MarkAllAsReadAsync(Guid userId)
    {
        await notificationRepository.MarkAllAsReadAsync(userId);
    }
}
