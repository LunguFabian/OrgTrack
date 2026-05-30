using OrgTrack.Application.DTOs;
using OrgTrack.Application.Interfaces;
using OrgTrack.Domain.Entities;

namespace OrgTrack.Application.UseCases;

public class MessageService
{
    private readonly IMessageRepository _messageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IRealtimeNotifier _realtimeNotifier;

    public MessageService(
        IMessageRepository messageRepository,
        IUserRepository userRepository,
        IRealtimeNotifier realtimeNotifier)
    {
        _messageRepository = messageRepository;
        _userRepository = userRepository;
        _realtimeNotifier = realtimeNotifier;
    }

    public async Task<MessageDto> SendMessageAsync(Guid senderId, Guid receiverId, string content)
    {
        var sender = await _userRepository.GetByIdAsync(senderId);
        if (sender == null) throw new ArgumentException("Sender not found");
        
        var receiver = await _userRepository.GetByIdAsync(receiverId);
        if (receiver == null) throw new ArgumentException("Receiver not found");

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        await _messageRepository.AddAsync(message);

        var dto = new MessageDto(
            message.Id,
            message.SenderId,
            $"{sender.FirstName} {sender.LastName}".Trim(),
            sender.PictureUrl,
            message.ReceiverId,
            message.Content,
            message.SentAt,
            message.IsRead
        );

        // Notify Receiver
        await _realtimeNotifier.SendToUserAsync(receiverId, "ReceiveMessage", dto);
        
        // Notify Sender (useful if sender has multiple tabs open)
        await _realtimeNotifier.SendToUserAsync(senderId, "MessageSent", dto);

        return dto;
    }

    public async Task<List<MessageDto>> GetConversationAsync(Guid currentUserId, Guid otherUserId)
    {
        var messages = await _messageRepository.GetConversationAsync(currentUserId, otherUserId);
        
        // We reverse because the repo returns the newest first (for efficient Take/Skip), but UI needs chronological
        messages.Reverse();

        return messages.Select(m => new MessageDto(
            m.Id,
            m.SenderId,
            $"{m.Sender.FirstName} {m.Sender.LastName}".Trim(),
            m.Sender.PictureUrl,
            m.ReceiverId,
            m.Content,
            m.SentAt,
            m.IsRead
        )).ToList();
    }

    public async Task<List<ConversationDto>> GetConversationsListAsync(Guid userId)
    {
        var recentMessages = await _messageRepository.GetRecentMessagesAsync(userId);

        var conversations = recentMessages.Select(m =>
        {
            bool isSentByMe = m.SenderId == userId;
            var otherUser = isSentByMe ? m.Receiver : m.Sender;

            return new ConversationDto(
                otherUser.Id,
                $"{otherUser.FirstName} {otherUser.LastName}".Trim(),
                otherUser.PictureUrl,
                m.Content,
                m.SentAt,
                m.IsRead,
                isSentByMe
            );
        }).ToList();

        return conversations.OrderByDescending(c => c.LastMessageSentAt).ToList();
    }

    public async Task MarkConversationAsReadAsync(Guid currentUserId, Guid otherUserId)
    {
        // currentUserId is the one reading the messages sent by otherUserId
        await _messageRepository.MarkAsReadAsync(senderId: otherUserId, receiverId: currentUserId);

        // Notify the other user that their messages were read
        await _realtimeNotifier.SendToUserAsync(otherUserId, "MessagesRead", new { ReaderId = currentUserId });
    }

    public async Task<int> GetUnreadTotalCountAsync(Guid userId)
    {
        return await _messageRepository.GetUnreadCountAsync(userId);
    }
}
