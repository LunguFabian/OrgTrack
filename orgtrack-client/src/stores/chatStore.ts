import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { MessageDto, ConversationDto } from '../types/chat';
import { messagesService } from '../api/services/messages.service';
import { signalrService } from '../api/services/signalr.service';

export const useChatStore = defineStore('chat', () => {
  const isDrawerOpen = ref(false);
  const activeChatUserId = ref<string | null>(null);
  
  const conversations = ref<ConversationDto[]>([]);
  const activeMessages = ref<MessageDto[]>([]);
  const unreadTotal = ref(0);
  const isLoading = ref(false);

  const fetchConversations = async () => {
    try {
      conversations.value = await messagesService.getConversations();
    } catch (err) {
      console.error('Failed to load conversations', err);
    }
  };

  const fetchUnreadCount = async () => {
    try {
      unreadTotal.value = await messagesService.getUnreadCount();
    } catch (err) {
      console.error('Failed to load unread count', err);
    }
  };

  const openDrawer = (userId: string | null = null) => {
    isDrawerOpen.value = true;
    if (userId) {
      openChat(userId);
    } else {
      activeChatUserId.value = null;
    }
  };

  const closeDrawer = () => {
    isDrawerOpen.value = false;
    activeChatUserId.value = null;
  };

  const openChat = async (userId: string) => {
    activeChatUserId.value = userId;
    isLoading.value = true;
    try {
      activeMessages.value = await messagesService.getConversationMessages(userId);
      // Automatically mark as read
      await messagesService.markConversationAsRead(userId);
      await fetchUnreadCount();
      
      // Update local conversation list
      const conv = conversations.value.find(c => c.otherUserId === userId);
      if (conv && !conv.isLastMessageRead && !conv.isLastMessageSentByMe) {
        conv.isLastMessageRead = true;
      }
    } catch (err) {
      console.error('Failed to load messages', err);
    } finally {
      isLoading.value = false;
    }
  };

  const sendMessage = async (content: string) => {
    if (!activeChatUserId.value || !content.trim()) return;
    try {
      const msg: any = await messagesService.sendMessage({ receiverId: activeChatUserId.value, content });
      const incomingId = String(msg.id || msg.Id).toLowerCase();
      if (!activeMessages.value.some(m => String(m.id || (m as any).Id).toLowerCase() === incomingId)) {
        activeMessages.value.push(msg);
      }
      updateConversationWithNewMessage(msg);
    } catch (err) {
      console.error('Failed to send message', err);
    }
  };

  const updateConversationWithNewMessage = (msg: any) => {
    const senderId = msg.senderId || msg.SenderId;
    const receiverId = msg.receiverId || msg.ReceiverId;
    const content = msg.content || msg.Content;
    const sentAt = msg.sentAt || msg.SentAt;
    const isRead = msg.isRead !== undefined ? msg.isRead : msg.IsRead;

    const isSentByMe = senderId !== receiverId && activeChatUserId.value === receiverId;
    const otherId = isSentByMe ? receiverId : senderId;
    
    let conv = conversations.value.find(c => c.otherUserId === otherId);
    if (conv) {
      conv.lastMessageContent = content;
      conv.lastMessageSentAt = sentAt;
      conv.isLastMessageRead = isRead;
      conv.isLastMessageSentByMe = isSentByMe;
      // Move to top
      conversations.value = [
        conv,
        ...conversations.value.filter(c => c.otherUserId !== otherId)
      ];
    } else {
      // If we don't have this conversation in the list, just refetch
      fetchConversations();
    }
  };

  const initRealtimeListeners = () => {
    signalrService.on('ReceiveMessage', (msg: any) => {
      const senderId = msg.senderId || msg.SenderId;
      if (activeChatUserId.value === senderId) {
        const incomingId = String(msg.id || msg.Id).toLowerCase();
        if (!activeMessages.value.some(m => String(m.id || (m as any).Id).toLowerCase() === incomingId)) {
          activeMessages.value.push(msg);
        }
        messagesService.markConversationAsRead(senderId);
        msg.isRead = true;
        if (msg.IsRead !== undefined) msg.IsRead = true;
      } else {
        unreadTotal.value++;
      }
      updateConversationWithNewMessage(msg);
    });

    signalrService.on('MessageSent', (msg: any) => {
      // Received from another tab of the same user
      if (activeChatUserId.value === (msg.receiverId || msg.ReceiverId)) {
        const incomingId = String(msg.id || msg.Id).toLowerCase();
        if (!activeMessages.value.some(m => String(m.id || (m as any).Id).toLowerCase() === incomingId)) {
          activeMessages.value.push(msg);
        }
      }
      updateConversationWithNewMessage(msg);
    });

    signalrService.on('MessagesRead', (data: { readerId: string }) => {
      // Someone read our messages
      if (activeChatUserId.value === data.readerId) {
        activeMessages.value.forEach(m => {
          if (m.senderId !== data.readerId) m.isRead = true;
        });
      }
      const conv = conversations.value.find(c => c.otherUserId === data.readerId);
      if (conv && conv.isLastMessageSentByMe) {
        conv.isLastMessageRead = true;
      }
    });
  };

  return {
    isDrawerOpen,
    activeChatUserId,
    conversations,
    activeMessages,
    unreadTotal,
    isLoading,
    fetchConversations,
    fetchUnreadCount,
    openDrawer,
    closeDrawer,
    openChat,
    sendMessage,
    initRealtimeListeners
  };
});
