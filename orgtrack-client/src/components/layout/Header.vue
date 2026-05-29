<script setup lang="ts">
import { ref, onMounted, onUnmounted, watch } from 'vue';
import { useAuthStore } from '../../stores/authStore';
import { useNotificationStore } from '../../stores/notificationStore';
import { useRouter } from 'vue-router';
import { organizationService } from '../../api/services/organization.service';
import type { NotificationDto } from '../../api/services/notifications.service';
import { LogOut, Bell, Search, Menu, User, Sun, Moon, Loader2 } from 'lucide-vue-next';
import { useDark, useToggle } from '@vueuse/core';

const isDark = useDark();
const toggleDark = useToggle(isDark);

const authStore = useAuthStore();
const notificationStore = useNotificationStore();
const router = useRouter();

defineEmits(['toggle-sidebar']);

const isProfileMenuOpen = ref(false);
const isNotificationOpen = ref(false);
const dropdownRef = ref<HTMLElement | null>(null);
const notificationDropdownRef = ref<HTMLElement | null>(null);

// --- Search state ---
const searchQuery = ref('');
const searchResults = ref<Array<{id: string, firstName: string, lastName: string, email: string, pictureUrl?: string}>>([]);
const isSearching = ref(false);
const showSearchDropdown = ref(false);
const searchContainerRef = ref<HTMLElement | null>(null);
let debounceTimer: ReturnType<typeof setTimeout> | null = null;

const performSearch = async (query: string) => {
  if (query.length < 2) {
    searchResults.value = [];
    showSearchDropdown.value = false;
    return;
  }
  isSearching.value = true;
  showSearchDropdown.value = true;
  try {
    searchResults.value = await organizationService.searchMembers(query);
  } catch (err) {
    console.error('Search failed:', err);
    searchResults.value = [];
  } finally {
    isSearching.value = false;
  }
};

watch(searchQuery, (val) => {
  if (debounceTimer) clearTimeout(debounceTimer);
  if (val.length < 2) {
    searchResults.value = [];
    showSearchDropdown.value = false;
    return;
  }
  debounceTimer = setTimeout(() => performSearch(val), 300);
});

const selectResult = (userId: string) => {
  searchQuery.value = '';
  searchResults.value = [];
  showSearchDropdown.value = false;
  router.push(`/profile/${userId}`);
};

const handleSearchKeydown = (e: KeyboardEvent) => {
  if (e.key === 'Escape') {
    showSearchDropdown.value = false;
    (e.target as HTMLElement)?.blur();
  }
};

// --- Dropdown / click-outside ---
const handleLogout = () => {
  isProfileMenuOpen.value = false;
  authStore.logout();
  window.location.href = '/login';
};

const handleClickOutside = (e: MouseEvent) => {
  if (dropdownRef.value && !dropdownRef.value.contains(e.target as Node)) {
    isProfileMenuOpen.value = false;
  }
  if (searchContainerRef.value && !searchContainerRef.value.contains(e.target as Node)) {
    showSearchDropdown.value = false;
  }
  if (notificationDropdownRef.value && !notificationDropdownRef.value.contains(e.target as Node)) {
    isNotificationOpen.value = false;
  }
};

const handleNotificationClick = (n: NotificationDto) => {
  if (!n.isRead) {
    notificationStore.markAsRead(n.id);
  }
  isNotificationOpen.value = false;
  // Navigate based on notification type
  if (n.relatedEntityType === 'Task') {
    router.push('/my-tasks');
  } else if (n.relatedEntityType === 'Event') {
    router.push('/events');
  }
};

const formatTimeAgo = (iso: string) => {
  const now = new Date();
  const date = new Date(iso);
  const diffMs = now.getTime() - date.getTime();
  const diffMin = Math.floor(diffMs / 60000);
  if (diffMin < 1) return 'just now';
  if (diffMin < 60) return `${diffMin}m ago`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr}h ago`;
  const diffDay = Math.floor(diffHr / 24);
  if (diffDay < 7) return `${diffDay}d ago`;
  return date.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
};

onMounted(() => {
  document.addEventListener('click', handleClickOutside);
});

onUnmounted(() => {
  document.removeEventListener('click', handleClickOutside);
  if (debounceTimer) clearTimeout(debounceTimer);
});
</script>

<template>
  <header class="h-16 bg-bg/80 backdrop-blur-md border-b border-border sticky top-0 z-30 flex items-center justify-between px-4 sm:px-8">
    <div class="flex items-center flex-1 gap-4">
      <!-- Hamburger Menu (visible only on small screens) -->
      <button 
        @click="$emit('toggle-sidebar')"
        class="lg:hidden text-text-muted hover:text-text-strong p-1 rounded-lg hover:bg-surface-hover transition-colors"
      >
        <Menu class="w-6 h-6" />
      </button>

      <!-- Search Bar -->
      <div class="relative w-full max-w-md hidden md:block" ref="searchContainerRef">
        <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
          <Search v-if="!isSearching" class="h-4 w-4 text-text-muted" />
          <Loader2 v-else class="h-4 w-4 text-emerald-400 animate-spin" />
        </div>
        <input 
          v-model="searchQuery"
          @keydown="handleSearchKeydown"
          @focus="searchQuery.length >= 2 && (showSearchDropdown = true)"
          type="text" 
          placeholder="Search members by name or email..." 
          class="block w-full pl-10 pr-3 py-2 border border-border rounded-lg bg-surface text-text-strong placeholder-gray-500 focus:outline-none focus:ring-1 focus:ring-emerald-500 focus:border-emerald-500 sm:text-sm transition-all"
        />

        <!-- Search Results Dropdown -->
        <div 
          v-if="showSearchDropdown"
          class="absolute top-full left-0 right-0 mt-2 bg-surface border border-border rounded-xl shadow-2xl overflow-hidden z-50 max-h-80 overflow-y-auto"
        >
          <!-- Loading -->
          <div v-if="isSearching && searchResults.length === 0" class="flex items-center gap-3 px-4 py-6 justify-center">
            <Loader2 class="w-5 h-5 text-emerald-400 animate-spin" />
            <span class="text-sm text-text-muted">Searching...</span>
          </div>

          <!-- No results -->
          <div v-else-if="!isSearching && searchResults.length === 0 && searchQuery.length >= 2" class="px-4 py-6 text-center">
            <p class="text-sm text-text-muted">No members found for "<span class="text-text-strong font-medium">{{ searchQuery }}</span>"</p>
          </div>

          <!-- Results list -->
          <button
            v-for="user in searchResults"
            :key="user.id"
            @click="selectResult(user.id)"
            class="w-full flex items-center gap-3 px-4 py-3 hover:bg-emerald-500/5 transition-colors text-left border-b border-border last:border-b-0"
          >
            <img 
              v-if="user.pictureUrl" 
              :src="user.pictureUrl" 
              class="w-9 h-9 rounded-full object-cover flex-shrink-0" 
              alt="" 
            />
            <div v-else class="w-9 h-9 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center font-bold text-sm flex-shrink-0">
              {{ user.firstName[0] }}{{ user.lastName[0] }}
            </div>
            <div class="min-w-0 flex-1">
              <p class="text-sm font-medium text-text-strong truncate">{{ user.firstName }} {{ user.lastName }}</p>
              <p class="text-xs text-text-muted truncate">{{ user.email }}</p>
            </div>
          </button>
        </div>
      </div>
    </div>

    <!-- Right side (Profile & Actions) -->
    <div class="flex items-center gap-4 sm:gap-6">
      <button @click="toggleDark()" class="text-text-muted hover:text-text-strong transition-colors p-2 rounded-full hover:bg-surface-hover">
        <Sun v-if="!isDark" class="w-5 h-5" />
        <Moon v-else class="w-5 h-5" />
      </button>

      <div class="relative" ref="notificationDropdownRef">
        <button @click="isNotificationOpen = !isNotificationOpen" class="text-text-muted hover:text-text-strong transition-colors relative p-2 rounded-full hover:bg-surface-hover">
          <Bell class="w-5 h-5" />
          <span v-if="notificationStore.unreadCount > 0" class="absolute -top-0.5 -right-0.5 min-w-[18px] h-[18px] rounded-full bg-red-500 text-white text-[10px] font-bold flex items-center justify-center px-1 ring-2 ring-bg">
            {{ notificationStore.unreadCount > 99 ? '99+' : notificationStore.unreadCount }}
          </span>
        </button>

        <!-- Notification Dropdown -->
        <div 
          v-if="isNotificationOpen"
          class="absolute right-0 mt-3 w-96 bg-surface border border-border rounded-xl shadow-2xl z-50 overflow-hidden max-h-[480px] flex flex-col"
        >
          <div class="px-4 py-3 border-b border-border flex items-center justify-between">
            <h3 class="text-sm font-semibold text-text-strong">Notifications</h3>
            <button 
              v-if="notificationStore.unreadCount > 0"
              @click="notificationStore.markAllAsRead()"
              class="text-xs text-emerald-400 hover:text-emerald-300 font-medium"
            >
              Mark all as read
            </button>
          </div>

          <div class="overflow-y-auto flex-1">
            <div v-if="notificationStore.notifications.length === 0" class="px-4 py-10 text-center">
              <Bell class="w-8 h-8 text-text-muted mx-auto mb-2 opacity-30" />
              <p class="text-sm text-text-muted">No notifications yet</p>
            </div>

            <button
              v-for="n in notificationStore.notifications"
              :key="n.id"
              @click="handleNotificationClick(n)"
              class="w-full text-left px-4 py-3 border-b border-border last:border-b-0 hover:bg-emerald-500/5 transition-colors flex items-start gap-3"
              :class="{ 'bg-emerald-500/5': !n.isRead }"
            >
              <div class="mt-0.5 w-2 h-2 rounded-full flex-shrink-0" :class="n.isRead ? 'bg-transparent' : 'bg-emerald-500'"></div>
              <div class="flex-1 min-w-0">
                <p class="text-sm font-medium text-text-strong truncate">{{ n.title }}</p>
                <p class="text-xs text-text-muted mt-0.5 line-clamp-2">{{ n.message }}</p>
                <p class="text-[10px] text-text-muted mt-1">{{ formatTimeAgo(n.createdAt) }}</p>
              </div>
            </button>
          </div>
        </div>
      </div>

      <div class="h-6 w-px bg-border"></div>

      <!-- User Profile Dropdown -->
      <div class="relative" ref="dropdownRef">
        <button 
          @click="isProfileMenuOpen = !isProfileMenuOpen"
          class="flex items-center gap-3 outline-none hover:opacity-80 transition-opacity"
        >
          <div class="flex flex-col items-end hidden sm:flex">
            <span class="text-sm font-medium text-text-strong">{{ authStore.user?.firstName }} {{ authStore.user?.lastName }}</span>
            <span class="text-xs text-text-muted">{{ authStore.user?.email }}</span>
          </div>
          <img 
            v-if="authStore.user?.pictureUrl" 
            :src="authStore.user.pictureUrl" 
            class="h-9 w-9 rounded-full object-cover shadow-md cursor-pointer" 
            alt="Profile Avatar" 
          />
          <div 
            v-else 
            class="h-9 w-9 rounded-full bg-emerald-600 flex items-center justify-center text-white font-bold shadow-md cursor-pointer"
          >
            {{ authStore.user?.firstName?.[0] }}{{ authStore.user?.lastName?.[0] }}
          </div>
        </button>

        <!-- Dropdown Menu -->
        <div 
          v-if="isProfileMenuOpen"
          class="absolute right-0 mt-3 w-56 bg-surface border border-border rounded-xl shadow-2xl py-2 z-50 transform origin-top-right transition-all"
        >
          <div class="px-4 py-2 border-b border-border mb-2 sm:hidden">
             <p class="text-sm font-medium text-text-strong truncate">{{ authStore.user?.firstName }} {{ authStore.user?.lastName }}</p>
             <p class="text-xs text-text-muted truncate">{{ authStore.user?.email }}</p>
          </div>
          
          <router-link 
            to="/profile" 
            @click="isProfileMenuOpen = false"
            class="flex items-center gap-3 px-4 py-2 text-sm text-text-muted hover:bg-border/40 hover:text-text-strong transition-colors"
          >
            <User class="w-4 h-4" />
            My Profile
          </router-link>
          
          
          
          <div class="h-px bg-border my-2"></div>
          
          <button 
            @click="handleLogout"
            class="w-full flex items-center gap-3 px-4 py-2 text-sm text-red-400 hover:bg-red-500/10 transition-colors"
          >
            <LogOut class="w-4 h-4" />
            Sign Out
          </button>
        </div>
      </div>
    </div>
  </header>
</template>
