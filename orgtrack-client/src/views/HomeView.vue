<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useAuthStore } from '../stores/authStore';
import SkeletonLoader from '../components/common/SkeletonLoader.vue';
import { organizationService } from '../api/services/organization.service';
import { tasksService } from '../api/services/tasks.service';
import { eventsService } from '../api/services/events.service';
import type { OrganizationUnitDto } from '../types/organization';
import type { TaskDto, EventDto } from '../types/unit';
import {
  CheckCircle2, Clock, AlertCircle, ListTodo, ChevronRight,
  Building2, Layers, Shield, Sparkles, ArrowUpRight,
  CalendarDays, Repeat
} from 'lucide-vue-next';
import { useRouter } from 'vue-router';

const authStore = useAuthStore();
const router = useRouter();

const isLoading = ref(true);
const myUnits = ref<OrganizationUnitDto[]>([]);
const myTasks = ref<TaskDto[]>([]);
const myEvents = ref<EventDto[]>([]);

onMounted(async () => {
  if (!authStore.user) return;
  isLoading.value = true;

  try {
    const [units, tasks, events] = await Promise.all([
      organizationService.getMyUnits(),
      tasksService.getMyTasks(),
      eventsService.getMyEvents()
    ]);
    myUnits.value = units;
    myTasks.value = tasks;
    myEvents.value = events;
  } catch (error) {
    console.error('Failed to load dashboard data', error);
  } finally {
    isLoading.value = false;
  }
});
const doneTasks = computed(() => myTasks.value.filter(t => t.status === 'Done').length);
const inProgressTasks = computed(() => myTasks.value.filter(t => t.status === 'InProgress').length);
const todoTasks = computed(() => myTasks.value.filter(t => t.status === 'ToDo').length);
const overdueTasks = computed(() =>
  myTasks.value.filter(t => t.deadline && new Date(t.deadline) < new Date() && t.status !== 'Done').length
);

const stats = computed(() => [
  { name: 'Completed', value: doneTasks.value, icon: CheckCircle2, color: 'text-emerald-400', bg: 'bg-emerald-500/10', border: 'border-emerald-500/20' },
  { name: 'In Progress', value: inProgressTasks.value, icon: Clock, color: 'text-blue-400', bg: 'bg-blue-500/10', border: 'border-blue-500/20' },
  { name: 'To Do', value: todoTasks.value, icon: ListTodo, color: 'text-amber-400', bg: 'bg-amber-500/10', border: 'border-amber-500/20' },
  { name: 'Overdue', value: overdueTasks.value, icon: AlertCircle, color: overdueTasks.value > 0 ? 'text-red-400' : 'text-text-muted', bg: overdueTasks.value > 0 ? 'bg-red-500/10' : 'bg-gray-500/10', border: overdueTasks.value > 0 ? 'border-red-500/20' : 'border-gray-700' },
]);
const upcomingDeadlines = computed(() =>
  myTasks.value
    .filter(t => t.deadline && t.status !== 'Done')
    .sort((a, b) => new Date(a.deadline!).getTime() - new Date(b.deadline!).getTime())
    .slice(0, 5)
);

const upcomingEvents = computed(() => {
  const now = new Date();
  return myEvents.value
    .filter(e => new Date(e.startDate) >= now)
    .sort((a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime())
    .slice(0, 5);
});

const formatEventDate = (iso: string) => {
  const d = new Date(iso);
  const now = new Date();
  const diffMs = d.getTime() - now.getTime();
  const diffDays = Math.ceil(diffMs / 86_400_000);

  if (diffDays === 0) return { text: 'Today', class: 'text-emerald-400' };
  if (diffDays === 1) return { text: 'Tomorrow', class: 'text-blue-400' };
  if (diffDays <= 7) return { text: `In ${diffDays}d`, class: 'text-blue-400' };
  return { text: d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' }), class: 'text-text-muted' };
};

const formatEventTime = (iso: string) =>
  new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });

const formatDeadline = (iso: string) => {
  const d = new Date(iso);
  const now = new Date();
  const diffMs = d.getTime() - now.getTime();
  const diffDays = Math.ceil(diffMs / 86_400_000);

  if (diffDays < 0) return { text: `${Math.abs(diffDays)}d overdue`, class: 'text-red-400' };
  if (diffDays === 0) return { text: 'Due today', class: 'text-amber-400' };
  if (diffDays === 1) return { text: 'Due tomorrow', class: 'text-amber-400' };
  if (diffDays <= 7) return { text: `${diffDays}d left`, class: 'text-blue-400' };
  return { text: d.toLocaleDateString('en-GB', { day: 'numeric', month: 'short' }), class: 'text-text-muted' };
};

const priorityStyle = (p: string) => {
  switch (p) {
    case 'Critical': return 'bg-red-500/15 text-red-400 border-red-500/25';
    case 'High': return 'bg-orange-500/15 text-orange-400 border-orange-500/25';
    case 'Medium': return 'bg-amber-500/15 text-amber-400 border-amber-500/25';
    default: return 'bg-gray-500/10 text-text-muted border-gray-600';
  }
};
const getUnitIcon = (type: string) => {
  switch (type) {
    case 'Committee': return Building2;
    case 'Department': return Layers;
    case 'Team': return Shield;
    default: return Building2;
  }
};

const getUnitColor = (type: string) => {
  switch (type) {
    case 'National': return { text: 'text-blue-400', bg: 'bg-blue-500/10', border: 'border-blue-500/30' };
    case 'Committee': return { text: 'text-emerald-400', bg: 'bg-emerald-500/10', border: 'border-emerald-500/30' };
    case 'Department': return { text: 'text-purple-400', bg: 'bg-purple-500/10', border: 'border-purple-500/30' };
    case 'Team': return { text: 'text-orange-400', bg: 'bg-orange-500/10', border: 'border-orange-500/30' };
    default: return { text: 'text-text-muted', bg: 'bg-gray-500/10', border: 'border-gray-500/30' };
  }
};
const greeting = computed(() => {
  const h = new Date().getHours();
  if (h < 12) return 'Good morning';
  if (h < 18) return 'Good afternoon';
  return 'Good evening';
});
</script>

<template>
  <div class="space-y-8 max-w-7xl mx-auto pb-20">
    <!-- Hero Header -->
    <div class="relative overflow-hidden bg-gradient-to-br from-dark-surface to-dark-bg p-8 rounded-2xl border border-border">
      <div class="relative z-10">
        <div class="flex items-center gap-2 mb-2">
          <Sparkles class="w-5 h-5 text-emerald-400" />
          <span class="text-sm text-emerald-400 font-medium">Dashboard</span>
        </div>
        <h1 class="text-3xl font-bold text-text-strong tracking-tight">
          {{ greeting }}, {{ authStore.user?.firstName }}!
        </h1>
        <p class="text-text-muted mt-2 max-w-lg">
          You have <span class="text-text-strong font-semibold">{{ myTasks.filter(t => t.status !== 'Done').length }}</span> active task{{ myTasks.filter(t => t.status !== 'Done').length !== 1 ? 's' : '' }}
          across <span class="text-text-strong font-semibold">{{ myUnits.length }}</span> unit{{ myUnits.length !== 1 ? 's' : '' }}.
        </p>
      </div>
      <!-- Decorative gradient orb -->
      <div class="absolute -top-20 -right-20 w-64 h-64 bg-emerald-500/5 rounded-full blur-3xl"></div>
      <div class="absolute -bottom-10 -right-10 w-40 h-40 bg-blue-500/5 rounded-full blur-2xl"></div>
    </div>

    <!-- Skeleton Loading State -->
    <div v-if="isLoading" class="space-y-6">
      <!-- Skeleton Stat Cards -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div v-for="i in 4" :key="i" class="p-5 rounded-2xl border border-border bg-surface flex items-center gap-4">
          <SkeletonLoader type="circular" width="44px" height="44px" class="rounded-xl" />
          <div class="space-y-2 flex-1">
            <SkeletonLoader width="60%" height="12px" />
            <SkeletonLoader width="40%" height="24px" />
          </div>
        </div>
      </div>

      <div class="grid grid-cols-1 lg:grid-cols-5 gap-6">
        <!-- Skeleton Upcoming Events -->
        <div class="lg:col-span-3 space-y-4">
          <div class="flex items-center justify-between">
            <SkeletonLoader width="150px" height="24px" />
            <SkeletonLoader width="80px" height="16px" />
          </div>
          <div class="bg-surface rounded-2xl border border-border p-4 space-y-4">
            <div v-for="i in 3" :key="i" class="flex items-start justify-between gap-3">
              <div class="space-y-2 flex-1">
                <SkeletonLoader width="70%" height="16px" />
                <SkeletonLoader width="40%" height="12px" />
              </div>
              <SkeletonLoader width="60px" height="16px" />
            </div>
          </div>
        </div>

        <!-- Skeleton Right Column -->
        <div class="lg:col-span-2 space-y-6">
          <div class="space-y-4">
            <div class="flex items-center justify-between">
              <SkeletonLoader width="160px" height="24px" />
              <SkeletonLoader width="70px" height="16px" />
            </div>
            <div class="bg-surface rounded-2xl border border-border p-4 space-y-4">
              <div v-for="i in 2" :key="i" class="flex items-start justify-between gap-3">
                <div class="space-y-2 flex-1">
                  <SkeletonLoader width="80%" height="16px" />
                  <SkeletonLoader width="50%" height="12px" />
                </div>
                <SkeletonLoader width="60px" height="16px" />
              </div>
            </div>
          </div>
        </div>
      </div>
    </div>

    <template v-else>
      <!-- Stat Cards -->
      <div class="grid grid-cols-2 lg:grid-cols-4 gap-4">
        <div
          v-for="stat in stats"
          :key="stat.name"
          :class="['p-5 rounded-2xl border shadow-sm flex items-center gap-4 transition-colors', stat.bg, stat.border]"
        >
          <div :class="['p-3 rounded-xl flex items-center justify-center', stat.bg]">
            <component :is="stat.icon" :class="['w-5 h-5', stat.color]" />
          </div>
          <div>
            <p class="text-xs font-medium text-text-muted uppercase tracking-wider">{{ stat.name }}</p>
            <p :class="['text-2xl font-bold mt-0.5', stat.color]">{{ stat.value }}</p>
          </div>
        </div>
      </div>

      <!-- Two-Column Layout -->
      <div class="grid grid-cols-1 lg:grid-cols-5 gap-6">

        <!-- Left: Upcoming Events (3 cols) -->
        <div class="lg:col-span-3 space-y-4">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-semibold text-text-strong">Upcoming Events</h2>
            <button
              @click="router.push('/events')"
              class="text-xs text-text-muted hover:text-emerald-400 transition-colors flex items-center gap-1"
            >
              All events <ArrowUpRight class="w-3 h-3" />
            </button>
          </div>

          <div class="bg-surface rounded-2xl border border-border overflow-hidden">
            <div v-if="upcomingEvents.length === 0" class="p-10 text-center">
              <CalendarDays class="w-8 h-8 text-emerald-500/40 mx-auto mb-3" />
              <p class="text-sm text-text-muted font-medium">No upcoming events</p>
              <p class="text-xs text-gray-600 mt-1">You're all clear for now.</p>
            </div>

            <div v-else class="divide-y divide-dark-border">
              <div
                v-for="event in upcomingEvents"
                :key="event.id"
                class="p-4 hover:bg-border/20 transition-colors cursor-pointer"
                @click="router.push('/events')"
              >
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0 flex-1">
                    <p class="text-sm text-text-strong font-medium truncate">{{ event.title }}</p>
                    <div class="flex items-center gap-2 mt-1.5">
                      <span class="flex items-center gap-1 text-xs text-text-muted">
                        <Clock class="w-3 h-3" />
                        {{ formatEventTime(event.startDate) }}
                      </span>
                      <span v-if="event.isRecurring" class="flex items-center gap-1 px-1.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-purple-500/15 text-purple-400 border border-purple-500/20">
                        <Repeat class="w-2.5 h-2.5" /> Recurring
                      </span>
                    </div>
                  </div>
                  <span :class="['text-xs font-medium whitespace-nowrap', formatEventDate(event.startDate).class]">
                    {{ formatEventDate(event.startDate).text }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right: Upcoming Deadlines (2 cols) -->
        <div class="lg:col-span-2 space-y-4">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-semibold text-text-strong">Upcoming Deadlines</h2>
            <button
              @click="router.push('/my-tasks')"
              class="text-xs text-text-muted hover:text-emerald-400 transition-colors flex items-center gap-1"
            >
              All tasks <ArrowUpRight class="w-3 h-3" />
            </button>
          </div>

          <div class="bg-surface rounded-2xl border border-border overflow-hidden">
            <div v-if="upcomingDeadlines.length === 0" class="p-10 text-center">
              <CheckCircle2 class="w-8 h-8 text-emerald-500/40 mx-auto mb-3" />
              <p class="text-sm text-text-muted font-medium">All caught up!</p>
              <p class="text-xs text-gray-600 mt-1">No upcoming deadlines.</p>
            </div>

            <div v-else class="divide-y divide-dark-border">
              <div
                v-for="task in upcomingDeadlines"
                :key="task.id"
                class="p-4 hover:bg-border/20 transition-colors cursor-pointer"
                @click="router.push('/my-tasks')"
              >
                <div class="flex items-start justify-between gap-3">
                  <div class="min-w-0 flex-1">
                    <p class="text-sm text-text-strong font-medium truncate">{{ task.title }}</p>
                    <div class="flex items-center gap-2 mt-1.5">
                      <span :class="['px-1.5 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border', priorityStyle(task.priority)]">
                        {{ task.priority }}
                      </span>
                      <span class="text-[10px] text-gray-600">•</span>
                      <span class="text-xs text-text-muted truncate">{{ task.assigneeName }}</span>
                    </div>
                  </div>
                  <span :class="['text-xs font-medium whitespace-nowrap', formatDeadline(task.deadline!).class]">
                    {{ formatDeadline(task.deadline!).text }}
                  </span>
                </div>
              </div>
            </div>
          </div>
        </div>

        <!-- Right bottom: My Units (2 cols) -->
        <div class="lg:col-span-2 space-y-4">
          <div class="flex items-center justify-between">
            <h2 class="text-lg font-semibold text-text-strong">My Units</h2>
            <button
              @click="router.push('/organization')"
              class="text-xs text-text-muted hover:text-emerald-400 transition-colors flex items-center gap-1"
            >
              View all <ArrowUpRight class="w-3 h-3" />
            </button>
          </div>

          <div v-if="myUnits.length === 0" class="bg-surface p-10 rounded-2xl border border-border text-center">
            <div class="w-14 h-14 bg-border rounded-2xl flex items-center justify-center mx-auto mb-4">
              <Building2 class="w-7 h-7 text-gray-600" />
            </div>
            <p class="text-text-muted font-medium">You're not part of any unit yet</p>
            <p class="text-sm text-gray-600 mt-1">Ask your team leader for an invite link.</p>
          </div>

          <div v-else class="space-y-2">
            <div
              v-for="unit in myUnits"
              :key="unit.id"
              @click="router.push(`/units/${unit.id}`)"
              class="bg-surface p-3.5 rounded-xl border border-border hover:border-emerald-500/40 transition-all cursor-pointer group relative overflow-hidden"
            >
              <div class="absolute inset-0 bg-gradient-to-br from-emerald-500/0 to-emerald-500/0 group-hover:from-emerald-500/[0.02] group-hover:to-transparent transition-all rounded-xl"></div>
              <div class="relative z-10 flex items-center justify-between">
                <div class="flex items-center gap-2.5">
                  <div :class="['w-8 h-8 rounded-lg flex items-center justify-center border', getUnitColor(unit.type).bg, getUnitColor(unit.type).border]">
                    <component :is="getUnitIcon(unit.type)" :class="['w-4 h-4', getUnitColor(unit.type).text]" />
                  </div>
                  <div>
                    <h3 class="text-sm font-semibold text-text-strong group-hover:text-emerald-400 transition-colors line-clamp-1">{{ unit.name }}</h3>
                    <span :class="['text-[10px] font-bold uppercase tracking-wider', getUnitColor(unit.type).text]">
                      {{ unit.type }}
                    </span>
                  </div>
                </div>
                <ChevronRight class="w-4 h-4 text-gray-700 group-hover:text-emerald-400 group-hover:translate-x-0.5 transition-all" />
              </div>
            </div>
          </div>
        </div>

      </div>
    </template>
  </div>
</template>
