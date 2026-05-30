namespace OrgTrack.Application.DTOs;

public record MessageDto(
    Guid Id,
    Guid SenderId,
    string SenderName,
    string? SenderProfilePictureUrl,
    Guid ReceiverId,
    string Content,
    DateTime SentAt,
    bool IsRead
);

public record ConversationDto(
    Guid OtherUserId,
    string OtherUserName,
    string? OtherUserProfilePictureUrl,
    string LastMessageContent,
    DateTime LastMessageSentAt,
    bool IsLastMessageRead,
    bool IsLastMessageSentByMe
);
