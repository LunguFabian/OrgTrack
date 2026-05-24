<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { eventsService, type EventDto, type AttendanceReportItem } from '../api/services/events.service';
import { useAuthStore } from '../stores/authStore';
import { useToastStore } from '../stores/toastStore';
import { useOrgStore } from '../stores/orgStore';
import {
  CalendarDays, Plus, Clock, MapPin, Users, CheckCircle2,
  XCircle, HelpCircle, ChevronDown, ChevronUp, Loader2,
  Calendar, Repeat, AlarmClock
} from 'lucide-vue-next';

const authStore = useAuthStore();
const toastStore = useToastStore();
const orgStore = useOrgStore(); // For resolving unit names

const events = ref<EventDto[]>([]);
const isLoading = ref(true);
const expandedEventId = ref<string | null>(null);
const attendanceReports = ref<Record<string, AttendanceReportItem[]>>({});
const loadingAttendance = ref<string | null>(null);
const rsvpLoading = ref<string | null>(null);
const userRsvps = ref<Record<string, string>>({});
const fetchEvents = async () => {
  isLoading.value = true;
  try {
    events.value = await eventsService.getMyEvents();
    events.value.forEach(e => {
      if (e.currentUserRsvp && e.currentUserRsvp !== 'NotResponded') {
        userRsvps.value[e.id] = e.currentUserRsvp;
      }
    });
    if (orgStore.tree.length === 0) {
      await orgStore.fetchTree();
    }
  } catch (err: any) {
    toastStore.showToast(err.response?.data?.error || 'Failed to load events.', 'error');
  } finally {
    isLoading.value = false;
  }
};

onMounted(fetchEvents);
const now = new Date();

const upcomingEvents = computed(() =>
  events.value
    .filter(e => new Date(e.startDate) >= now)
    .sort((a, b) => new Date(a.startDate).getTime() - new Date(b.startDate).getTime())
);

const pastEvents = computed(() =>
  events.value
    .filter(e => new Date(e.startDate) < now)
    .sort((a, b) => new Date(b.startDate).getTime() - new Date(a.startDate).getTime())
);

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString('en-GB', {
    weekday: 'short', day: 'numeric', month: 'short', year: 'numeric'
  });

const formatTime = (iso: string) =>
  new Date(iso).toLocaleTimeString('en-GB', { hour: '2-digit', minute: '2-digit' });

const formatDuration = (start: string, end: string) => {
  const ms = new Date(end).getTime() - new Date(start).getTime();
  const hours = Math.floor(ms / 3_600_000);
  const minutes = Math.floor((ms % 3_600_000) / 60_000);
  if (hours === 0) return `${minutes}m`;
  return minutes > 0 ? `${hours}h ${minutes}m` : `${hours}h`;
};

const isToday = (iso: string) => {
  const d = new Date(iso);
  return d.toDateString() === now.toDateString();
};

const isTomorrow = (iso: string) => {
  const d = new Date(iso);
  const tomorrow = new Date(now);
  tomorrow.setDate(tomorrow.getDate() + 1);
  return d.toDateString() === tomorrow.toDateString();
};

const findUnitInTree = (nodes: any[], id: string): any => {
  for (const node of nodes) {
    if (node.id === id) return node;
    if (node.childUnits && node.childUnits.length > 0) {
      const found = findUnitInTree(node.childUnits, id);
      if (found) return found;
    }
  }
  return undefined;
};

const getUnitName = (unitId: string) => {
  const unit = findUnitInTree(orgStore.tree, unitId);
  return unit ? unit.name : 'Organization';
};
const rsvpOptions = [
  { status: 'Present', label: 'Going', icon: CheckCircle2, color: 'text-emerald-400 border-emerald-500/30 bg-emerald-500/10 hover:bg-emerald-500/20' },
  { status: 'Maybe',   label: 'Maybe',  icon: HelpCircle,   color: 'text-amber-400 border-amber-500/30 bg-amber-500/10 hover:bg-amber-500/20' },
  { status: 'Absent',  label: 'Not Going', icon: XCircle,   color: 'text-red-400 border-red-500/30 bg-red-500/10 hover:bg-red-500/20' },
];

const submitRsvp = async (event: EventDto, status: 'Present' | 'Absent' | 'Maybe') => {
  rsvpLoading.value = event.id;
  try {
    await eventsService.rsvp(event.organizationUnitId, event.id, status);
    userRsvps.value[event.id] = status;
    toastStore.showToast(`RSVP updated: ${status === 'Present' ? 'Going!' : status === 'Absent' ? 'Not going.' : 'Maybe.'}`, 'success');
  } catch (err: any) {
    toastStore.showToast(err.response?.data?.error || 'Failed to update RSVP.', 'error');
  } finally {
    rsvpLoading.value = null;
  }
};
const isLeaderOfEvent = (unitId: string) => {
  const authStore = useAuthStore();
  const role = authStore.userRoles.find(r => r.unitId === unitId);
  return role?.roleName === 'VP' || role?.roleName === 'TL';
};

const toggleExpand = async (event: EventDto) => {
  if (expandedEventId.value === event.id) {
    expandedEventId.value = null;
    return;
  }
  expandedEventId.value = event.id;
  
  if (isLeaderOfEvent(event.organizationUnitId) && !attendanceReports.value[event.id]) {
    loadingAttendance.value = event.id;
    try {
      attendanceReports.value[event.id] = await eventsService.getAttendanceReport(event.organizationUnitId, event.id);
    } catch {
    } finally {
      loadingAttendance.value = null;
    }
  }
};

const confirmAttendanceAsLeader = async (event: EventDto, userId: string, currentStatus: string) => {
  let nextStatus = 'Present';
  if (currentStatus === 'Present') nextStatus = 'Absent';
  if (currentStatus === 'Absent') nextStatus = 'Maybe';
  if (currentStatus === 'Maybe') nextStatus = 'Present';

  try {
    await eventsService.confirmAttendance(event.organizationUnitId, event.id, userId, nextStatus);
    const report = attendanceReports.value[event.id];
    const item = report.find(r => r.userId === userId);
    if (item) item.status = nextStatus;
    toastStore.showToast('Attendance updated', 'success');
  } catch (err: any) {
    toastStore.showToast(err.response?.data?.error || 'Failed to update attendance', 'error');
  }
};

const statusBadge = (status: string) => {
  if (status === 'Present') return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
  if (status === 'Absent')  return 'bg-red-500/10 text-red-400 border-red-500/20';
  if (status === 'NotResponded') return 'bg-gray-500/10 text-gray-400 border-gray-500/20';
  return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
};
</script>

<template>
  <div class="space-y-6 max-w-5xl mx-auto py-8 px-6 lg:px-12">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h2 class="text-2xl font-bold text-white tracking-tight">My Events</h2>
        <p class="text-gray-400 mt-1">
          Stay on top of upcoming meetings and activities across your units.
        </p>
      </div>
      <div class="flex items-center gap-4 bg-dark-bg border border-dark-border rounded-xl px-4 py-2">
        <div class="flex items-center gap-2">
          <CalendarDays class="w-4 h-4 text-emerald-400" />
          <span class="text-sm font-medium text-white">{{ upcomingEvents.length }} upcoming</span>
        </div>
        <div class="w-px h-4 bg-dark-border"></div>
        <div class="flex items-center gap-2">
          <Calendar class="w-4 h-4 text-gray-500" />
          <span class="text-sm font-medium text-gray-400">{{ pastEvents.length }} past</span>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex justify-center py-16">
      <Loader2 class="w-8 h-8 text-emerald-500 animate-spin" />
    </div>

    <template v-else>
      <!-- Empty state -->
      <div v-if="events.length === 0" class="flex flex-col items-center justify-center py-32 text-center bg-dark-surface border border-dark-border rounded-2xl">
        <div class="w-16 h-16 bg-dark-bg rounded-2xl flex items-center justify-center mb-4 ring-1 ring-dark-border shadow-lg">
          <CalendarDays class="w-8 h-8 text-gray-500" />
        </div>
        <p class="text-white font-medium text-lg">No events scheduled</p>
        <p class="text-gray-500 mt-2 max-w-sm">
          You don't have any upcoming or past events in your organization units.
        </p>
      </div>

      <template v-else>
        <!-- Upcoming Events -->
        <section v-if="upcomingEvents.length > 0">
          <h3 class="text-sm font-semibold uppercase tracking-widest text-emerald-400/80 mb-4 flex items-center gap-2">
            <CalendarDays class="w-4 h-4" /> Upcoming Events
          </h3>
          <div class="grid grid-cols-1 xl:grid-cols-2 gap-4">
            <div
              v-for="event in upcomingEvents"
              :key="event.id"
              class="bg-dark-surface border rounded-xl overflow-hidden transition-all hover:shadow-lg"
              :class="isToday(event.startDate)
                ? 'border-emerald-500/40 shadow-emerald-500/5'
                : 'border-dark-border hover:border-dark-border/80'"
            >
              <!-- Event Card Header -->
              <div class="p-5">
                <div class="flex items-start justify-between gap-4">
                  <!-- Left: Date Block -->
                  <div class="flex items-start gap-4">
                    <div class="flex-shrink-0 w-14 h-14 rounded-xl bg-dark-bg border border-dark-border flex flex-col items-center justify-center shadow-inner">
                      <div class="text-[10px] font-bold uppercase text-gray-500 tracking-wider">
                        {{ new Date(event.startDate).toLocaleString('en-GB', { month: 'short' }) }}
                      </div>
                      <div class="text-2xl font-black leading-none mt-0.5"
                        :class="isToday(event.startDate) ? 'text-emerald-400' : 'text-white'"
                      >
                        {{ new Date(event.startDate).getDate() }}
                      </div>
                    </div>

                    <!-- Content -->
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2 mb-1.5 flex-wrap">
                        <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-dark-bg text-gray-300 border border-dark-border flex items-center gap-1">
                          <Network class="w-3 h-3 text-gray-500" />
                          {{ getUnitName(event.organizationUnitId) }}
                        </span>
                        <span v-if="isToday(event.startDate)"
                          class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-emerald-500/15 text-emerald-400 border border-emerald-500/20">
                          Today
                        </span>
                        <span v-else-if="isTomorrow(event.startDate)"
                          class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-blue-500/15 text-blue-400 border border-blue-500/20">
                          Tomorrow
                        </span>
                        <span v-if="event.isRecurring"
                          class="flex items-center gap-1 px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-purple-500/15 text-purple-400 border border-purple-500/20">
                          <Repeat class="w-2.5 h-2.5" /> Recurring
                        </span>
                      </div>
                      <h4 class="text-white font-semibold text-base leading-tight group-hover:text-emerald-400 transition-colors">{{ event.title }}</h4>
                      <p v-if="event.description" class="text-sm text-gray-400 mt-1 line-clamp-2">{{ event.description }}</p>
                      
                      <div class="flex items-center gap-4 mt-3">
                        <span class="flex items-center gap-1.5 text-xs text-gray-500 font-medium">
                          <Clock class="w-3.5 h-3.5 text-gray-400" />
                          {{ formatTime(event.startDate) }} – {{ formatTime(event.endDate) }}
                        </span>
                        <span class="flex items-center gap-1.5 text-xs text-gray-500 font-medium">
                          <AlarmClock class="w-3.5 h-3.5 text-gray-400" />
                          {{ formatDuration(event.startDate, event.endDate) }}
                        </span>
                      </div>
                    </div>
                  </div>

                  <!-- Right: expand -->
                  <button @click="toggleExpand(event)" class="w-8 h-8 rounded-lg bg-dark-bg border border-dark-border flex items-center justify-center text-gray-500 hover:text-white hover:border-gray-600 transition-all flex-shrink-0 mt-1">
                    <ChevronDown v-if="expandedEventId !== event.id" class="w-4 h-4" />
                    <ChevronUp v-else class="w-4 h-4" />
                  </button>
                </div>

                <!-- RSVP Buttons -->
                <div class="flex items-center gap-3 mt-5 pt-4 border-t border-dark-border/50">
                  <span class="text-xs font-medium uppercase tracking-wider text-gray-500">Your RSVP</span>
                  <button
                    v-for="opt in rsvpOptions"
                    :key="opt.status"
                    @click="submitRsvp(event, opt.status as any)"
                    :disabled="rsvpLoading === event.id"
                    class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium border transition-all disabled:opacity-50 hover:shadow-lg"
                    :class="[opt.color, userRsvps[event.id] === opt.status ? 'ring-2 ring-offset-2 ring-offset-dark-surface ring-current shadow-lg shadow-current/20 scale-105' : '']"
                  >
                    <component :is="opt.icon" class="w-3.5 h-3.5" />
                    {{ opt.label }}
                  </button>
                  <Loader2 v-if="rsvpLoading === event.id" class="w-4 h-4 text-gray-400 animate-spin ml-auto" />
                </div>
              </div>

              <!-- Expanded: Attendance report (leaders only) -->
              <div v-if="expandedEventId === event.id" class="border-t border-dark-border bg-dark-bg p-5">
                <div v-if="loadingAttendance === event.id" class="flex justify-center py-6">
                  <Loader2 class="w-6 h-6 animate-spin text-gray-500" />
                </div>
                <template v-else-if="isLeaderOfEvent(event.organizationUnitId) && attendanceReports[event.id]">
                  <div class="flex items-center justify-between mb-4">
                    <h5 class="text-xs font-semibold text-white uppercase tracking-widest flex items-center gap-2">
                      <Users class="w-4 h-4 text-emerald-500" /> 
                      Attendance Report 
                      <span class="text-gray-500 font-normal normal-case tracking-normal">({{ attendanceReports[event.id].length }} members)</span>
                    </h5>
                    <span class="text-xs text-gray-500">Click on status to change</span>
                  </div>
                  
                  <div v-if="attendanceReports[event.id].length === 0" class="text-sm text-gray-500 text-center py-4 bg-dark-surface rounded-xl border border-dark-border">
                    No members found in this unit tree.
                  </div>
                  <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                    <div
                      v-for="item in attendanceReports[event.id]"
                      :key="item.userId"
                      class="flex items-center justify-between bg-dark-surface border border-dark-border rounded-lg p-2.5 transition-colors hover:border-gray-700"
                    >
                      <div class="flex items-center gap-3">
                        <div class="w-7 h-7 rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 flex items-center justify-center text-[10px] font-bold">
                          {{ item.userName.substring(0, 2).toUpperCase() }}
                        </div>
                        <span class="text-sm font-medium text-gray-200">{{ item.userName }}</span>
                      </div>
                      <button 
                        @click="confirmAttendanceAsLeader(event, item.userId, item.status)"
                        class="px-2.5 py-1 rounded-md text-[10px] font-bold uppercase tracking-wider border hover:scale-105 transition-transform"
                        :class="statusBadge(item.status)">
                        {{ item.status === 'NotResponded' ? 'No RSVP' : item.status }}
                      </button>
                    </div>
                  </div>
                </template>
                <div v-else class="text-sm text-gray-500 flex flex-col items-center justify-center py-6 text-center">
                  <Users class="w-8 h-8 text-dark-border mb-3" />
                  <p class="text-gray-300 font-medium">Only leaders can view attendance</p>
                  <p class="text-xs mt-1">If you are a leader of this unit, please check your permissions.</p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- Past Events -->
        <section v-if="pastEvents.length > 0" class="mt-10">
          <h3 class="text-sm font-semibold uppercase tracking-widest text-gray-500 mb-4 flex items-center gap-2">
            <Calendar class="w-4 h-4" /> Past Events
          </h3>
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            <div
              v-for="event in pastEvents"
              :key="event.id"
              class="bg-dark-surface border border-dark-border rounded-xl p-4 opacity-75 hover:opacity-100 transition-all group"
            >
              <div class="flex items-start justify-between">
                <div class="flex-1 min-w-0 pr-4">
                  <p class="text-sm font-medium text-gray-300 group-hover:text-white transition-colors truncate">{{ event.title }}</p>
                  <div class="flex items-center gap-2 mt-1.5">
                    <span class="text-xs text-gray-500">{{ formatDate(event.startDate) }}</span>
                    <span class="w-1 h-1 rounded-full bg-dark-border"></span>
                    <span class="text-[10px] font-medium text-gray-400 truncate">{{ getUnitName(event.organizationUnitId) }}</span>
                  </div>
                </div>
                <div class="w-8 h-8 rounded-lg bg-dark-bg border border-dark-border flex items-center justify-center text-gray-600">
                  <CheckCircle2 class="w-4 h-4" />
                </div>
              </div>
            </div>
          </div>
        </section>
      </template>
    </template>

  </div>
</template>
