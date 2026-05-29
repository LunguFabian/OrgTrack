using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.Interfaces;

public interface INotificationRepository
{
    Task AddAsync(Notification notification);
    Task<IEnumerable<Notification>> GetByUserIdAsync(Guid userId, int limit = 50);
    Task<int> GetUnreadCountAsync(Guid userId);
    Task MarkAsReadAsync(Guid notificationId);
    Task MarkAllAsReadAsync(Guid userId);
}
