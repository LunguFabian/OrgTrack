<script setup lang="ts">
import { ref, nextTick, watch } from 'vue';
import { useChatStore } from '../../stores/chatStore';
import { useAuthStore } from '../../stores/authStore';
import { X, Send, ArrowLeft, Loader2, Check, CheckCheck } from 'lucide-vue-next';

const chatStore = useChatStore();
const authStore = useAuthStore();
const newMessage = ref('');
const messagesContainer = ref<HTMLElement | null>(null);

const scrollToBottom = async () => {
  await nextTick();
  if (messagesContainer.value) {
    messagesContainer.value.scrollTop = messagesContainer.value.scrollHeight;
  }
};

watch(() => chatStore.activeMessages.length, () => {
  scrollToBottom();
});

const handleSend = async () => {
  if (!newMessage.value.trim()) return;
  const content = newMessage.value;
  newMessage.value = '';
  await chatStore.sendMessage(content);
};

const formatDate = (dateStr: string) => {
  const d = new Date(dateStr);
  return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
};

const getOtherUser = () => {
  if (!chatStore.activeChatUserId) return null;
  return chatStore.conversations.find(c => c.otherUserId === chatStore.activeChatUserId);
};

const closeDrawer = () => {
  chatStore.closeDrawer();
};
</script>

<template>
  <div>
    <!-- Backdrop -->
    <div 
      v-if="chatStore.isDrawerOpen" 
      @click="closeDrawer"
      class="fixed inset-0 bg-black/50 backdrop-blur-sm z-40 transition-opacity"
    ></div>

    <!-- Drawer Panel -->
    <div 
      class="fixed top-0 right-0 h-full w-full sm:w-[400px] bg-surface shadow-2xl z-50 transform transition-transform duration-300 ease-in-out flex flex-col border-l border-border"
      :class="chatStore.isDrawerOpen ? 'translate-x-0' : 'translate-x-full'"
    >
      <!-- Mode 1: Conversations List -->
      <template v-if="!chatStore.activeChatUserId">
        <div class="p-4 border-b border-border/50 flex items-center justify-between shrink-0 bg-surface">
          <h2 class="text-lg font-bold text-text-strong">Messages</h2>
          <button @click="closeDrawer" class="p-2 rounded-lg hover:bg-bg text-text-muted transition-colors">
            <X class="w-5 h-5" />
          </button>
        </div>

        <div class="flex-1 overflow-y-auto custom-scrollbar">
          <div v-if="chatStore.conversations.length === 0" class="flex flex-col items-center justify-center h-full text-center p-6">
            <div class="w-16 h-16 rounded-full bg-emerald-500/10 text-emerald-400 flex items-center justify-center mb-4">
              <Send class="w-8 h-8 opacity-50" />
            </div>
            <p class="text-text-muted text-sm">No messages yet. Go to a user's profile to start a conversation!</p>
          </div>
          
          <div v-else class="divide-y divide-border/30">
            <button 
              v-for="conv in chatStore.conversations" 
              :key="conv.otherUserId"
              @click="chatStore.openChat(conv.otherUserId)"
              class="w-full p-4 flex items-center gap-3 hover:bg-bg transition-colors text-left"
            >
              <div class="relative shrink-0">
                <div v-if="conv.otherUserProfilePictureUrl" class="w-12 h-12 rounded-full overflow-hidden border border-border">
                  <img :src="conv.otherUserProfilePictureUrl" class="w-full h-full object-cover" />
                </div>
                <div v-else class="w-12 h-12 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center font-bold text-lg border border-emerald-500/30">
                  {{ conv.otherUserName.substring(0, 2).toUpperCase() }}
                </div>
                <div v-if="!conv.isLastMessageRead && !conv.isLastMessageSentByMe" class="absolute top-0 right-0 w-3.5 h-3.5 bg-red-500 border-2 border-surface rounded-full"></div>
              </div>
              
              <div class="flex-1 min-w-0">
                <div class="flex items-baseline justify-between mb-1">
                  <h3 class="font-semibold text-text-strong truncate pr-2" :class="{'font-bold': !conv.isLastMessageRead && !conv.isLastMessageSentByMe}">{{ conv.otherUserName }}</h3>
                  <span class="text-[10px] text-text-muted shrink-0">{{ formatDate(conv.lastMessageSentAt) }}</span>
                </div>
                <p class="text-xs truncate" :class="!conv.isLastMessageRead && !conv.isLastMessageSentByMe ? 'text-text-strong font-medium' : 'text-text-muted'">
                  <span v-if="conv.isLastMessageSentByMe" class="text-text-muted">You: </span>
                  {{ conv.lastMessageContent }}
                </p>
              </div>
            </button>
          </div>
        </div>
      </template>

      <!-- Mode 2: Active Chat -->
      <template v-else>
        <!-- Chat Header -->
        <div class="p-4 border-b border-border/50 flex items-center gap-3 shrink-0 bg-surface">
          <button @click="chatStore.activeChatUserId = null" class="p-2 rounded-lg hover:bg-bg text-text-muted transition-colors">
            <ArrowLeft class="w-5 h-5" />
          </button>
          
          <div class="flex items-center gap-3 flex-1 min-w-0">
            <div class="w-10 h-10 shrink-0 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center font-bold text-sm border border-emerald-500/30 overflow-hidden">
              <img v-if="getOtherUser()?.otherUserProfilePictureUrl" :src="getOtherUser()?.otherUserProfilePictureUrl" class="w-full h-full object-cover" />
              <span v-else>{{ getOtherUser()?.otherUserName.substring(0, 2).toUpperCase() || '?' }}</span>
            </div>
            <div class="min-w-0">
              <h2 class="text-sm font-bold text-text-strong truncate">{{ getOtherUser()?.otherUserName || 'Chat' }}</h2>
            </div>
          </div>

          <button @click="closeDrawer" class="p-2 rounded-lg hover:bg-bg text-text-muted transition-colors">
            <X class="w-5 h-5" />
          </button>
        </div>

        <!-- Chat Messages -->
        <div ref="messagesContainer" class="flex-1 overflow-y-auto p-4 space-y-4 custom-scrollbar bg-bg relative">
          <div v-if="chatStore.isLoading" class="absolute inset-0 flex items-center justify-center bg-bg/50 backdrop-blur-sm z-10">
            <Loader2 class="w-6 h-6 animate-spin text-emerald-500" />
          </div>

          <div 
            v-for="msg in chatStore.activeMessages" 
            :key="msg.id || (msg as any).Id"
            class="flex flex-col max-w-[85%]"
            :class="(msg.senderId || (msg as any).SenderId) === authStore.user?.id ? 'self-end items-end' : 'self-start items-start'"
          >
            <div 
              class="px-4 py-2 rounded-2xl shadow-sm text-sm break-words"
              :class="(msg.senderId || (msg as any).SenderId) === authStore.user?.id 
                ? 'bg-emerald-600 text-white rounded-br-sm' 
                : 'bg-surface border border-border/50 text-text-strong rounded-bl-sm'"
            >
              {{ msg.content || (msg as any).Content }}
            </div>
            <div class="flex items-center gap-1 mt-1 px-1">
              <span class="text-[9px] text-text-muted">{{ formatDate(msg.sentAt || (msg as any).SentAt) }}</span>
              <template v-if="(msg.senderId || (msg as any).SenderId) === authStore.user?.id">
                <CheckCheck v-if="msg.isRead || (msg as any).IsRead" class="w-3 h-3 text-emerald-400" />
                <Check v-else class="w-3 h-3 text-text-muted" />
              </template>
            </div>
          </div>
        </div>

        <!-- Chat Input -->
        <div class="p-4 border-t border-border/50 bg-surface shrink-0">
          <form @submit.prevent="handleSend" class="flex items-center gap-2 relative">
            <input 
              v-model="newMessage"
              type="text" 
              placeholder="Type a message..." 
              class="w-full bg-bg border border-border rounded-full pl-4 pr-12 py-2.5 text-sm text-text-strong placeholder-gray-500 focus:border-emerald-500 outline-none transition-colors"
            />
            <button 
              type="submit"
              :disabled="!newMessage.trim()"
              class="absolute right-1.5 p-1.5 rounded-full bg-emerald-600 hover:bg-emerald-500 text-white disabled:opacity-50 disabled:hover:bg-emerald-600 transition-colors"
            >
              <Send class="w-4 h-4" />
            </button>
          </form>
        </div>
      </template>
    </div>
  </div>
</template>
