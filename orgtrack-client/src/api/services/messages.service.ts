import { api } from '../axios';
import type { MessageDto, ConversationDto, SendMessageRequest } from '../../types/chat';

export const messagesService = {
  async getConversations(): Promise<ConversationDto[]> {
    const { data } = await api.get('/messages/conversations');
    return data;
  },

  async getConversationMessages(otherUserId: string): Promise<MessageDto[]> {
    const { data } = await api.get(`/messages/conversations/${otherUserId}`);
    return data;
  },

  async sendMessage(request: SendMessageRequest): Promise<MessageDto> {
    const { data } = await api.post('/messages/send', request);
    return data;
  },

  async markConversationAsRead(otherUserId: string): Promise<void> {
    await api.put(`/messages/conversations/${otherUserId}/read`);
  },

  async getUnreadCount(): Promise<number> {
    const { data } = await api.get('/messages/unread-count');
    return data.unreadCount;
  }
};
