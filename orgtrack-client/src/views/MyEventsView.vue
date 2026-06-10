<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { eventsService, type AttendanceReportItem, type RsvpSummaryItem } from '../api/services/events.service';
import type { EventDto } from '../types/unit';
import { useAuthStore } from '../stores/authStore';
import { useToastStore } from '../stores/toastStore';
import { useOrgStore } from '../stores/orgStore';
import {
  CalendarDays, Clock, Users, CheckCircle2,
  XCircle, HelpCircle, ChevronDown, ChevronUp, Loader2,
  Calendar, Repeat, AlarmClock, Network, ClipboardCheck
} from 'lucide-vue-next';

const authStore = useAuthStore();
const toastStore = useToastStore();
const orgStore = useOrgStore();

const events = ref<EventDto[]>([]);
const isLoading = ref(true);
const expandedEventId = ref<string | null>(null);
const rsvpSummaries = ref<Record<string, RsvpSummaryItem[]>>({});
const attendanceReports = ref<Record<string, AttendanceReportItem[]>>({});
const loadingExpanded = ref<string | null>(null);
const rsvpLoading = ref<string | null>(null);
const userRsvps = ref<Record<string, string>>({});

const fetchEvents = async () => {
  isLoading.value = true;
  try {
    events.value = await eventsService.getMyEvents();
    events.value.forEach(e => {
      if (e.currentUserRsvp && e.currentUserRsvp !== 'NoResponse') {
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

const isPastEvent = (iso: string) => new Date(iso) < now;

const findUnitInTree = (nodes: any[], id: string): any => {
  for (const node of nodes) {
    if (node.id === id) return node;
    if (node.children && node.children.length > 0) {
      const found = findUnitInTree(node.children, id);
      if (found) return found;
    }
  }
  return undefined;
};

const getUnitName = (unitId: string) => {
  const unit = findUnitInTree(orgStore.tree, unitId);
  return unit ? unit.name : 'Organization';
};

const isLeaderOfEvent = (unitId: string) => {
  const unit = findUnitInTree(orgStore.tree, unitId);
  if (!unit?.members) return true; // If no members info, assume higher-level access
  const membership = unit.members.find((m: any) => m.userId === authStore.user?.id);
  if (!membership) return true; // Not a direct member = inspecting top-down as admin/president
  return membership.roleName !== 'Member';
};

const rsvpOptions = [
  { status: 'Going',    label: 'Going',     icon: CheckCircle2, color: 'text-emerald-400 border-emerald-500/30 bg-emerald-500/10 hover:bg-emerald-500/20' },
  { status: 'Maybe',    label: 'Maybe',     icon: HelpCircle,   color: 'text-amber-400 border-amber-500/30 bg-amber-500/10 hover:bg-amber-500/20' },
  { status: 'NotGoing', label: 'Not Going', icon: XCircle,      color: 'text-red-400 border-red-500/30 bg-red-500/10 hover:bg-red-500/20' },
];

const submitRsvp = async (event: EventDto, status: 'Going' | 'Maybe' | 'NotGoing') => {
  rsvpLoading.value = event.id;
  try {
    await eventsService.rsvp(event.organizationUnitId, event.id, status);
    userRsvps.value[event.id] = status;
    toastStore.showToast(`RSVP updated: ${status === 'Going' ? 'Going!' : status === 'NotGoing' ? 'Not going.' : 'Maybe.'}`, 'success');
  } catch (err: any) {
    toastStore.showToast(err.response?.data?.error || 'Failed to update RSVP.', 'error');
  } finally {
    rsvpLoading.value = null;
  }
};

const toggleExpand = async (event: EventDto) => {
  if (expandedEventId.value === event.id) {
    expandedEventId.value = null;
    return;
  }
  expandedEventId.value = event.id;
  loadingExpanded.value = event.id;

  try {
    // Always load RSVP summary (visible to everyone)
    if (!rsvpSummaries.value[event.id]) {
      rsvpSummaries.value[event.id] = await eventsService.getRsvpSummary(event.organizationUnitId, event.id);
    }
    // Load attendance report for leaders on past events
    if (isLeaderOfEvent(event.organizationUnitId) && isPastEvent(event.startDate) && !attendanceReports.value[event.id]) {
      attendanceReports.value[event.id] = await eventsService.getAttendanceReport(event.organizationUnitId, event.id);
    }
  } catch {
  } finally {
    loadingExpanded.value = null;
  }
};

const confirmAttendanceAsLeader = async (event: EventDto, userId: string, currentStatus: string) => {
  let nextStatus = 'Present';
  if (currentStatus === 'Present') nextStatus = 'Absent';
  if (currentStatus === 'Absent') nextStatus = 'Excused';
  if (currentStatus === 'Excused') nextStatus = 'Unmarked';
  if (currentStatus === 'Unmarked') nextStatus = 'Present';

  try {
    await eventsService.confirmAttendance(event.organizationUnitId, event.id, userId, nextStatus);
    const report = attendanceReports.value[event.id];
    const item = report.find(r => r.userId === userId);
    if (item) item.attendance = nextStatus;
    toastStore.showToast('Attendance updated', 'success');
  } catch (err: any) {
    toastStore.showToast(err.response?.data?.error || 'Failed to update attendance', 'error');
  }
};

const rsvpBadge = (rsvp: string) => {
  if (rsvp === 'Going') return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
  if (rsvp === 'NotGoing') return 'bg-red-500/10 text-red-400 border-red-500/20';
  if (rsvp === 'Maybe') return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
  return 'bg-gray-500/10 text-text-muted border-gray-500/20';
};

const rsvpLabel = (rsvp: string) => {
  if (rsvp === 'Going') return 'Going';
  if (rsvp === 'NotGoing') return 'Not Going';
  if (rsvp === 'Maybe') return 'Maybe';
  return 'No Response';
};

const attendanceBadge = (attendance: string) => {
  if (attendance === 'Present') return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
  if (attendance === 'Absent') return 'bg-red-500/10 text-red-400 border-red-500/20';
  if (attendance === 'Excused') return 'bg-blue-500/10 text-blue-400 border-blue-500/20';
  return 'bg-gray-500/10 text-text-muted border-gray-500/20';
};

const attendanceLabel = (attendance: string) => {
  if (attendance === 'Present') return 'Present';
  if (attendance === 'Absent') return 'Absent';
  if (attendance === 'Excused') return 'Excused';
  return 'Unmarked';
};
</script>

<template>
  <div class="space-y-6 max-w-5xl mx-auto py-8 px-6 lg:px-12">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h2 class="text-2xl font-bold text-text-strong tracking-tight">My Events</h2>
        <p class="text-text-muted mt-1">
          Stay on top of upcoming meetings and activities across your units.
        </p>
      </div>
      <div class="flex items-center gap-4 bg-bg border border-border rounded-xl px-4 py-2">
        <div class="flex items-center gap-2">
          <CalendarDays class="w-4 h-4 text-emerald-400" />
          <span class="text-sm font-medium text-text-strong">{{ upcomingEvents.length }} upcoming</span>
        </div>
        <div class="w-px h-4 bg-border"></div>
        <div class="flex items-center gap-2">
          <Calendar class="w-4 h-4 text-text-muted" />
          <span class="text-sm font-medium text-text-muted">{{ pastEvents.length }} past</span>
        </div>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex justify-center py-16">
      <Loader2 class="w-8 h-8 text-emerald-500 animate-spin" />
    </div>

    <template v-else>
      <!-- Empty state -->
      <div v-if="events.length === 0" class="flex flex-col items-center justify-center py-32 text-center bg-surface border border-border rounded-2xl">
        <div class="w-16 h-16 bg-bg rounded-2xl flex items-center justify-center mb-4 ring-1 ring-dark-border shadow-lg">
          <CalendarDays class="w-8 h-8 text-text-muted" />
        </div>
        <p class="text-text-strong font-medium text-lg">No events scheduled</p>
        <p class="text-text-muted mt-2 max-w-sm">
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
              class="bg-surface border rounded-xl overflow-hidden transition-all hover:shadow-lg"
              :class="isToday(event.startDate)
                ? 'border-emerald-500/40 shadow-emerald-500/5'
                : 'border-border hover:border-border/80'"
            >
              <!-- Event Card Header -->
              <div class="p-5">
                <div class="flex items-start justify-between gap-4">
                  <div class="flex items-start gap-4">
                    <div class="flex-shrink-0 w-14 h-14 rounded-xl bg-bg border border-border flex flex-col items-center justify-center shadow-inner">
                      <div class="text-[10px] font-bold uppercase text-text-muted tracking-wider">
                        {{ new Date(event.startDate).toLocaleString('en-GB', { month: 'short' }) }}
                      </div>
                      <div class="text-2xl font-black leading-none mt-0.5"
                        :class="isToday(event.startDate) ? 'text-emerald-400' : 'text-text-strong'">
                        {{ new Date(event.startDate).getDate() }}
                      </div>
                    </div>

                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2 mb-1.5 flex-wrap">
                        <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-bg text-text-muted border border-border flex items-center gap-1">
                          <Network class="w-3 h-3 text-text-muted" />
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
                      <h4 class="text-text-strong font-semibold text-base leading-tight">{{ event.title }}</h4>
                      <p v-if="event.description" class="text-sm text-text-muted mt-1 line-clamp-2">{{ event.description }}</p>
                      <div class="flex items-center gap-4 mt-3">
                        <span class="flex items-center gap-1.5 text-xs text-text-muted font-medium">
                          <Clock class="w-3.5 h-3.5 text-text-muted" />
                          {{ formatTime(event.startDate) }} – {{ formatTime(event.endDate) }}
                        </span>
                        <span class="flex items-center gap-1.5 text-xs text-text-muted font-medium">
                          <AlarmClock class="w-3.5 h-3.5 text-text-muted" />
                          {{ formatDuration(event.startDate, event.endDate) }}
                        </span>
                      </div>
                    </div>
                  </div>

                  <button @click="toggleExpand(event)" class="w-8 h-8 rounded-lg bg-bg border border-border flex items-center justify-center text-text-muted hover:text-text-strong hover:border-gray-600 transition-all flex-shrink-0 mt-1">
                    <ChevronDown v-if="expandedEventId !== event.id" class="w-4 h-4" />
                    <ChevronUp v-else class="w-4 h-4" />
                  </button>
                </div>

                <!-- RSVP Buttons -->
                <div class="flex items-center gap-3 mt-5 pt-4 border-t border-border/50">
                  <span class="text-xs font-medium uppercase tracking-wider text-text-muted">Your RSVP</span>
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
                  <Loader2 v-if="rsvpLoading === event.id" class="w-4 h-4 text-text-muted animate-spin ml-auto" />
                </div>
              </div>

              <!-- Expanded: RSVP Summary (visible to ALL) -->
              <div v-if="expandedEventId === event.id" class="border-t border-border bg-bg p-5">
                <div v-if="loadingExpanded === event.id" class="flex justify-center py-6">
                  <Loader2 class="w-6 h-6 animate-spin text-text-muted" />
                </div>
                <template v-else-if="rsvpSummaries[event.id]">
                  <div class="flex items-center justify-between mb-4">
                    <h5 class="text-xs font-semibold text-text-strong uppercase tracking-widest flex items-center gap-2">
                      <Users class="w-4 h-4 text-emerald-500" />
                      RSVP Summary
                      <span class="text-text-muted font-normal normal-case tracking-normal">({{ rsvpSummaries[event.id].length }} invited)</span>
                    </h5>
                  </div>
                  <div v-if="rsvpSummaries[event.id].length === 0" class="text-sm text-text-muted text-center py-4 bg-surface rounded-xl border border-border">
                    No members found in this unit tree.
                  </div>
                  <div v-else class="grid grid-cols-1 sm:grid-cols-2 gap-2">
                    <div v-for="item in rsvpSummaries[event.id]" :key="item.userId"
                      class="flex items-center justify-between bg-surface border border-border rounded-lg p-2.5 transition-colors hover:border-gray-700">
                      <div class="flex items-center gap-3">
                        <div class="w-7 h-7 rounded-full bg-emerald-500/10 text-emerald-400 border border-emerald-500/20 flex items-center justify-center text-[10px] font-bold">
                          {{ item.userName.substring(0, 2).toUpperCase() }}
                        </div>
                        <span class="text-sm font-medium text-text">{{ item.userName }}</span>
                      </div>
                      <span class="px-2.5 py-1 rounded-md text-[10px] font-bold uppercase tracking-wider border"
                        :class="rsvpBadge(item.rsvp)">
                        {{ rsvpLabel(item.rsvp) }}
                      </span>
                    </div>
                  </div>
                </template>
              </div>
            </div>
          </div>
        </section>

        <!-- Past Events -->
        <section v-if="pastEvents.length > 0" class="mt-10">
          <h3 class="text-sm font-semibold uppercase tracking-widest text-text-muted mb-4 flex items-center gap-2">
            <Calendar class="w-4 h-4" /> Past Events
          </h3>
          <div class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
            <div
              v-for="event in pastEvents"
              :key="event.id"
              class="bg-surface border border-border rounded-xl overflow-hidden opacity-75 hover:opacity-100 transition-all group"
            >
              <div class="p-4">
                <div class="flex items-start justify-between">
                  <div class="flex-1 min-w-0 pr-4">
                    <p class="text-sm font-medium text-text-muted group-hover:text-text-strong transition-colors truncate">{{ event.title }}</p>
                    <div class="flex items-center gap-2 mt-1.5 flex-wrap">
                      <span class="text-xs text-text-muted">{{ formatDate(event.startDate) }}</span>
                      <span class="w-1 h-1 rounded-full bg-border"></span>
                      <span class="text-[10px] font-medium text-text-muted truncate">{{ getUnitName(event.organizationUnitId) }}</span>
                    </div>
                  </div>
                  <button @click="toggleExpand(event)" class="w-8 h-8 rounded-lg bg-bg border border-border flex items-center justify-center text-gray-600 hover:text-text-strong transition-colors">
                    <ChevronDown v-if="expandedEventId !== event.id" class="w-4 h-4" />
                    <ChevronUp v-else class="w-4 h-4" />
                  </button>
                </div>
              </div>

              <!-- Expanded: RSVP Summary + Attendance -->
              <div v-if="expandedEventId === event.id" class="border-t border-border bg-bg p-4 space-y-4">
                <div v-if="loadingExpanded === event.id" class="flex justify-center py-4">
                  <Loader2 class="w-5 h-5 animate-spin text-text-muted" />
                </div>
                <template v-else>
                  <!-- RSVP Summary (visible to ALL) -->
                  <div v-if="rsvpSummaries[event.id]">
                    <h5 class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-3 flex items-center gap-2">
                      <Users class="w-3.5 h-3.5" /> RSVP Summary
                    </h5>
                    <div v-if="rsvpSummaries[event.id].length === 0" class="text-sm text-text-muted text-center py-2">No members found.</div>
                    <div v-else class="grid grid-cols-1 gap-1.5">
                      <div v-for="item in rsvpSummaries[event.id]" :key="'rsvp-'+item.userId"
                        class="flex items-center justify-between bg-surface border border-border rounded-lg p-2">
                        <div class="flex items-center gap-2">
                          <div class="w-6 h-6 rounded-full bg-emerald-500/10 text-emerald-400 flex items-center justify-center text-[10px] font-bold">
                            {{ item.userName.substring(0, 2).toUpperCase() }}
                          </div>
                          <span class="text-sm text-text-muted">{{ item.userName }}</span>
                        </div>
                        <span class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border" :class="rsvpBadge(item.rsvp)">
                          {{ rsvpLabel(item.rsvp) }}
                        </span>
                      </div>
                    </div>
                  </div>

                  <!-- Attendance Panel (leaders only) -->
                  <div v-if="isLeaderOfEvent(event.organizationUnitId) && attendanceReports[event.id]">
                    <h5 class="text-xs font-semibold text-emerald-400 uppercase tracking-wider mb-3 flex items-center justify-between">
                      <span class="flex items-center gap-2"><ClipboardCheck class="w-3.5 h-3.5" /> Confirm Attendance</span>
                      <span class="text-[10px] normal-case text-text-muted font-normal">Click to cycle</span>
                    </h5>
                    <div class="grid grid-cols-1 gap-1.5">
                      <div v-for="item in attendanceReports[event.id]" :key="'att-'+item.userId"
                        class="flex items-center justify-between bg-surface border border-border rounded-lg p-2.5">
                        <div class="flex items-center gap-2">
                          <div class="w-6 h-6 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center text-[10px] font-bold">
                            {{ item.userName.substring(0, 2).toUpperCase() }}
                          </div>
                          <div class="flex flex-col">
                            <span class="text-sm text-text font-medium">{{ item.userName }}</span>
                            <span class="text-[10px] text-text-muted">RSVP: {{ rsvpLabel(item.rsvp) }}</span>
                          </div>
                        </div>
                        <button @click="confirmAttendanceAsLeader(event, item.userId, item.attendance)"
                          class="px-2.5 py-1 rounded-md text-[10px] font-bold uppercase tracking-wider border hover:scale-105 transition-transform"
                          :class="attendanceBadge(item.attendance)">
                          {{ attendanceLabel(item.attendance) }}
                        </button>
                      </div>
                    </div>
                  </div>
                  <div v-else-if="!isLeaderOfEvent(event.organizationUnitId)" class="text-xs text-text-muted text-center py-2">
                    Attendance confirmation is managed by your team leader.
                  </div>
                </template>
              </div>
            </div>
          </div>
        </section>
      </template>
    </template>

  </div>
</template>
