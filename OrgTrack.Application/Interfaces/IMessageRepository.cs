using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.Interfaces;

public interface IMessageRepository
{
    Task<Message> AddAsync(Message message);
    Task<List<Message>> GetConversationAsync(Guid userId1, Guid userId2, int limit = 50, int offset = 0);
    Task<List<Message>> GetRecentMessagesAsync(Guid userId);
    Task MarkAsReadAsync(Guid senderId, Guid receiverId);
    Task<int> GetUnreadCountAsync(Guid userId);
}
