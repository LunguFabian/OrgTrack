import { defineStore } from 'pinia';
import { ref } from 'vue';
import { notificationsService, type NotificationDto } from '../api/services/notifications.service';
import { signalrService } from '../api/services/signalr.service';

export const useNotificationStore = defineStore('notification', () => {
  const notifications = ref<NotificationDto[]>([]);
  const unreadCount = ref(0);
  const isLoaded = ref(false);

  const fetchNotifications = async () => {
    try {
      const [data, count] = await Promise.all([
        notificationsService.getNotifications(30),
        notificationsService.getUnreadCount()
      ]);
      notifications.value = data;
      unreadCount.value = count;
      isLoaded.value = true;
    } catch (err) {
      console.error('Failed to fetch notifications:', err);
    }
  };

  const markAsRead = async (id: string) => {
    try {
      await notificationsService.markAsRead(id);
      const notification = notifications.value.find(n => n.id === id);
      if (notification && !notification.isRead) {
        notification.isRead = true;
        unreadCount.value = Math.max(0, unreadCount.value - 1);
      }
    } catch (err) {
      console.error('Failed to mark as read:', err);
    }
  };

  const markAllAsRead = async () => {
    try {
      await notificationsService.markAllAsRead();
      notifications.value.forEach(n => n.isRead = true);
      unreadCount.value = 0;
    } catch (err) {
      console.error('Failed to mark all as read:', err);
    }
  };

  const initRealtimeListeners = () => {
    signalrService.on('ReceiveNotification', (data: NotificationDto) => {
      // Add new notification to the top
      notifications.value.unshift(data);
      unreadCount.value = data.unreadCount ?? unreadCount.value + 1;
    });
  };

  return {
    notifications,
    unreadCount,
    isLoaded,
    fetchNotifications,
    markAsRead,
    markAllAsRead,
    initRealtimeListeners
  };
});
