<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useOrgStore } from '../../stores/orgStore';
import { useNotificationStore } from '../../stores/notificationStore';
import { useChatStore } from '../../stores/chatStore';
import { signalrService } from '../../api/services/signalr.service';
import Sidebar from './Sidebar.vue';
import Header from './Header.vue';
import ChatDrawer from '../chat/ChatDrawer.vue';

const isSidebarOpen = ref(false);
const orgStore = useOrgStore();
const notificationStore = useNotificationStore();
const chatStore = useChatStore();

const toggleSidebar = () => {
  isSidebarOpen.value = !isSidebarOpen.value;
};

onMounted(async () => {
  if (orgStore.tree.length === 0) {
    orgStore.fetchTree();
  }
  if (orgStore.myUnits.length === 0) {
    orgStore.fetchMyUnits();
  }

  // Start SignalR connection and fetch notifications
  await signalrService.start();
  notificationStore.initRealtimeListeners();
  notificationStore.fetchNotifications();
  chatStore.initRealtimeListeners();
  chatStore.fetchUnreadCount();
  chatStore.fetchConversations();
});

onUnmounted(() => {
  signalrService.stop();
});
</script>

<template>
  <div class="min-h-screen bg-bg text-text">
    <!-- Black background overlay for mobile -->
    <div 
      v-if="isSidebarOpen" 
      @click="isSidebarOpen = false"
      class="fixed inset-0 bg-black/50 z-40 lg:hidden"
    ></div>

    <!-- Fixed sidebar (hidden on mobile if isSidebarOpen is false) -->
    <Sidebar :is-open="isSidebarOpen" @close="isSidebarOpen = false" />

    <!-- Main container to the right of Sidebar on lg, full width on mobile -->
    <div class="lg:pl-64 flex flex-col min-h-screen transition-all duration-300">
      <Header @toggle-sidebar="toggleSidebar" />
      
      <!-- Main page content (Child routes) -->
      <main class="flex-1 p-4 sm:p-8 overflow-x-hidden">
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </main>
    </div>

    <!-- Chat Drawer (Global) -->
    <ChatDrawer />
  </div>
</template>

<style scoped>
/* Subtle transition between pages */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}
</style>
