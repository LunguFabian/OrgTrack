using Microsoft.EntityFrameworkCore;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;
using OrgTrack.Infrastructure.Persistence;

namespace OrgTrack.Infrastructure.Persistence;

public class MessageRepository : IMessageRepository
{
    private readonly OrgTrackDbContext _context;

    public MessageRepository(OrgTrackDbContext context)
    {
        _context = context;
    }

    public async Task<Message> AddAsync(Message message)
    {
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<List<Message>> GetConversationAsync(Guid userId1, Guid userId2, int limit = 50, int offset = 0)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => (m.SenderId == userId1 && m.ReceiverId == userId2) ||
                        (m.SenderId == userId2 && m.ReceiverId == userId1))
            .OrderByDescending(m => m.SentAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(); // Will reverse in the service or UI to show oldest first
    }

    public async Task<List<Message>> GetRecentMessagesAsync(Guid userId)
    {
        // Get the latest message per conversation
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .OrderByDescending(m => m.SentAt)
            .ToListAsync();

        // Group by the "other" user
        var latestMessages = messages
            .GroupBy(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Select(g => g.First())
            .ToList();

        return latestMessages;
    }

    public async Task MarkAsReadAsync(Guid senderId, Guid receiverId)
    {
        var unreadMessages = await _context.Messages
            .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead)
            .ToListAsync();

        if (unreadMessages.Any())
        {
            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task<int> GetUnreadCountAsync(Guid userId)
    {
        return await _context.Messages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
    }
}
