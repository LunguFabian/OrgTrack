<script setup lang="ts">
import { computed } from 'vue';
import { useRoute } from 'vue-router';
import { useOrgStore } from '../../stores/orgStore';
import { 
  LayoutDashboard, 
  Network, 
  CalendarDays, 
  CheckSquare, 
  TrendingUp,
  X
} from 'lucide-vue-next';

defineProps<{
  isOpen: boolean
}>();

defineEmits(['close']);

const route = useRoute();
const orgStore = useOrgStore();

const navigation = computed(() => {
  const links = [
    { name: 'Dashboard', href: '/', icon: LayoutDashboard },
    { name: 'Organization', href: '/organization', icon: Network },
  ];

  if (orgStore.myUnits.some(u => u.type === 'National')) {
    links.push({ name: 'National Dashboard', href: '/national-dashboard', icon: TrendingUp });
  }

  links.push(
    { name: 'My Tasks', href: '/my-tasks', icon: CheckSquare },
    { name: 'My Events', href: '/events', icon: CalendarDays }
  );

  return links;
});
</script>

<template>
  <div 
    class="flex flex-col w-64 bg-surface border-r border-border min-h-screen fixed left-0 top-0 z-50 transition-transform duration-300 ease-in-out lg:translate-x-0"
    :class="isOpen ? 'translate-x-0' : '-translate-x-full'"
  >
    <!-- Logo Area -->
    <div class="h-16 flex items-center justify-between px-6 border-b border-border">
      <div class="flex items-center gap-2">
        <div class="w-8 h-8 rounded-lg bg-emerald-500 flex items-center justify-center shadow-lg shadow-emerald-500/20">
          <Network class="w-5 h-5 text-text-strong" />
        </div>
        <span class="text-xl font-bold text-text-strong tracking-tight">OrgTrack</span>
      </div>
      <!-- Close button on mobile -->
      <button @click="$emit('close')" class="lg:hidden text-text-muted hover:text-text-strong p-1">
        <X class="w-5 h-5" />
      </button>
    </div>

    <!-- Navigation Links -->
    <nav class="flex-1 px-4 py-6 space-y-2 overflow-y-auto">
      <router-link
        v-for="item in navigation"
        :key="item.name"
        :to="item.href"
        @click="$emit('close')"
        class="flex items-center gap-3 px-3 py-2.5 rounded-lg transition-all group"
        :class="[
          route.path === item.href || (item.href !== '/' && route.path.startsWith(item.href))
            ? 'bg-emerald-500/10 text-emerald-400 font-medium'
            : 'text-text-muted hover:bg-surface-hover hover:text-text'
        ]"
      >
        <component 
          :is="item.icon" 
          class="w-5 h-5 transition-colors"
          :class="[
            route.path === item.href || (item.href !== '/' && route.path.startsWith(item.href))
              ? 'text-emerald-400'
              : 'text-text-muted group-hover:text-text-muted'
          ]"
        />
        {{ item.name }}
      </router-link>
    </nav>
  </div>
</template>
