<script setup lang="ts">
import { ref, onMounted, onUnmounted } from 'vue';
import { useAuthStore } from '../../stores/authStore';
import { useRouter } from 'vue-router';
import { LogOut, Bell, Search, Menu, User, Settings } from 'lucide-vue-next';

const authStore = useAuthStore();
const router = useRouter();

defineEmits(['toggle-sidebar']);

const isProfileMenuOpen = ref(false);
const dropdownRef = ref<HTMLElement | null>(null);

const handleLogout = () => {
  isProfileMenuOpen.value = false;
  authStore.logout();
  router.push('/login');
};

const closeDropdown = (e: MouseEvent) => {
  if (dropdownRef.value && !dropdownRef.value.contains(e.target as Node)) {
    isProfileMenuOpen.value = false;
  }
};

onMounted(() => {
  document.addEventListener('click', closeDropdown);
});

onUnmounted(() => {
  document.removeEventListener('click', closeDropdown);
});
</script>

<template>
  <header class="h-16 bg-dark-bg/80 backdrop-blur-md border-b border-dark-border sticky top-0 z-30 flex items-center justify-between px-4 sm:px-8">
    <div class="flex items-center flex-1 gap-4">
      <!-- Hamburger Menu (visible only on small screens) -->
      <button 
        @click="$emit('toggle-sidebar')"
        class="lg:hidden text-gray-400 hover:text-white p-1 rounded-lg hover:bg-dark-border/50 transition-colors"
      >
        <Menu class="w-6 h-6" />
      </button>

      <!-- Search placeholder -->
      <div class="relative w-full max-w-md hidden md:block">
        <div class="absolute inset-y-0 left-0 pl-3 flex items-center pointer-events-none">
          <Search class="h-4 w-4 text-gray-500" />
        </div>
        <input 
          type="text" 
          placeholder="Search for members, tasks or units..." 
          class="block w-full pl-10 pr-3 py-2 border border-dark-border rounded-lg bg-dark-surface text-gray-300 placeholder-gray-500 focus:outline-none focus:ring-1 focus:ring-emerald-500 focus:border-emerald-500 sm:text-sm transition-all"
        />
      </div>
    </div>

    <!-- Right side (Profile & Actions) -->
    <div class="flex items-center gap-6">
      <button class="text-gray-400 hover:text-white transition-colors relative">
        <Bell class="w-5 h-5" />
        <span class="absolute top-0 right-0 block h-2 w-2 rounded-full bg-emerald-500 ring-2 ring-dark-bg"></span>
      </button>

      <div class="h-6 w-px bg-dark-border"></div>

      <!-- User Profile Dropdown -->
      <div class="relative" ref="dropdownRef">
        <button 
          @click="isProfileMenuOpen = !isProfileMenuOpen"
          class="flex items-center gap-3 outline-none hover:opacity-80 transition-opacity"
        >
          <div class="flex flex-col items-end hidden sm:flex">
            <span class="text-sm font-medium text-white">{{ authStore.user?.firstName }} {{ authStore.user?.lastName }}</span>
            <span class="text-xs text-gray-400">{{ authStore.user?.email }}</span>
          </div>
          <div class="h-9 w-9 rounded-full bg-emerald-600 flex items-center justify-center text-white font-bold shadow-md cursor-pointer">
            {{ authStore.user?.firstName?.[0] }}{{ authStore.user?.lastName?.[0] }}
          </div>
        </button>

        <!-- Dropdown Menu -->
        <div 
          v-if="isProfileMenuOpen"
          class="absolute right-0 mt-3 w-56 bg-dark-surface border border-dark-border rounded-xl shadow-2xl py-2 z-50 transform origin-top-right transition-all"
        >
          <div class="px-4 py-2 border-b border-dark-border mb-2 sm:hidden">
             <p class="text-sm font-medium text-white truncate">{{ authStore.user?.firstName }} {{ authStore.user?.lastName }}</p>
             <p class="text-xs text-gray-400 truncate">{{ authStore.user?.email }}</p>
          </div>
          
          <router-link 
            to="/profile" 
            @click="isProfileMenuOpen = false"
            class="flex items-center gap-3 px-4 py-2 text-sm text-gray-300 hover:bg-dark-border/40 hover:text-white transition-colors"
          >
            <User class="w-4 h-4" />
            My Profile
          </router-link>
          
          <router-link 
            to="/settings" 
            @click="isProfileMenuOpen = false"
            class="flex items-center gap-3 px-4 py-2 text-sm text-gray-300 hover:bg-dark-border/40 hover:text-white transition-colors"
          >
            <Settings class="w-4 h-4" />
            Account Settings
          </router-link>
          
          <div class="h-px bg-dark-border my-2"></div>
          
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
