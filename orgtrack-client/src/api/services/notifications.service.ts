import { api } from '../axios';

export interface NotificationDto {
  id: string;
  type: string;
  title: string;
  message: string;
  relatedEntityId?: string;
  relatedEntityType?: string;
  isRead: boolean;
  createdAt: string;
  actorName?: string;
  actorPictureUrl?: string;
  unreadCount?: number;
}

export const notificationsService = {
  async getNotifications(limit = 50): Promise<NotificationDto[]> {
    const res = await api.get<NotificationDto[]>(`/notifications?limit=${limit}`);
    return res.data;
  },

  async getUnreadCount(): Promise<number> {
    const res = await api.get<{ count: number }>('/notifications/unread-count');
    return res.data.count;
  },

  async markAsRead(id: string): Promise<void> {
    await api.put(`/notifications/${id}/read`);
  },

  async markAllAsRead(): Promise<void> {
    await api.put('/notifications/read-all');
  }
};
