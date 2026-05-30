export interface MessageDto {
  id: string;
  senderId: string;
  senderName: string;
  senderProfilePictureUrl?: string;
  receiverId: string;
  content: string;
  sentAt: string;
  isRead: boolean;
}

export interface ConversationDto {
  otherUserId: string;
  otherUserName: string;
  otherUserProfilePictureUrl?: string;
  lastMessageContent: string;
  lastMessageSentAt: string;
  isLastMessageRead: boolean;
  isLastMessageSentByMe: boolean;
}

export interface SendMessageRequest {
  receiverId: string;
  content: string;
}
