<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { eventsService, type EventDto, type AttendanceReportItem } from '../../api/services/events.service';
import { useAuthStore } from '../../stores/authStore';
import { useOrgStore } from '../../stores/orgStore';
import { useToastStore } from '../../stores/toastStore';
import { organizationService } from '../../api/services/organization.service';
import {
  CalendarDays, Plus, Clock, MapPin, Users, CheckCircle2,
  XCircle, HelpCircle, ChevronDown, ChevronUp, X, Loader2,
  Calendar, Repeat, AlarmClock
} from 'lucide-vue-next';

const props = defineProps<{
  unitId: string;
  isLeader: boolean;
}>();

const authStore = useAuthStore();
const orgStore = useOrgStore();
const toastStore = useToastStore();

const events = ref<EventDto[]>([]);
const isLoading = ref(true);
const expandedEventId = ref<string | null>(null);
const attendanceReports = ref<Record<string, AttendanceReportItem[]>>({});
const loadingAttendance = ref<string | null>(null);
const rsvpLoading = ref<string | null>(null);
const userRsvps = ref<Record<string, string>>({});
const isCreateModalOpen = ref(false);
const isCreating = ref(false);
const createError = ref('');
const newEventForm = ref({
  title: '',
  description: '',
  startDate: '',
  endDate: '',
  isRecurring: false,
  recurrencePattern: null as string | null,
  invitedUnitIds: [] as string[],
  invitedUserIds: [] as string[],
});
const userSearchQuery = ref('');
const isSearchingUsers = ref(false);
const searchedUsers = ref<Array<{id: string, firstName: string, lastName: string, email: string}>>([]);
const selectedUsers = ref<Array<{id: string, firstName: string, lastName: string, email: string}>>([]);

let searchTimeout: any = null;
const onUserSearch = () => {
  if (searchTimeout) clearTimeout(searchTimeout);
  if (userSearchQuery.value.length < 2) {
    searchedUsers.value = [];
    return;
  }
  searchTimeout = setTimeout(async () => {
    isSearchingUsers.value = true;
    try {
      searchedUsers.value = await organizationService.searchMembers(userSearchQuery.value);
    } catch {
      searchedUsers.value = [];
    } finally {
      isSearchingUsers.value = false;
    }
  }, 300);
};

const toggleUserInvite = (user: any) => {
  const index = selectedUsers.value.findIndex(u => u.id === user.id);
  if (index === -1) {
    selectedUsers.value.push(user);
    newEventForm.value.invitedUserIds.push(user.id);
  } else {
    selectedUsers.value.splice(index, 1);
    newEventForm.value.invitedUserIds = newEventForm.value.invitedUserIds.filter(id => id !== user.id);
  }
};

const isUserSelected = (userId: string) => newEventForm.value.invitedUserIds.includes(userId);
const allUnits = computed(() => {
  const allowed = new Set<string>();
  const userRoles = authStore.user?.unitRoles || [];
  const leaderUnitIds = userRoles
    .filter(r => r.role.name.includes('President') || r.role.name.includes('Leader'))
    .map(r => r.organizationUnitId);
  const collectDescendants = (node: any, isAllowed: boolean) => {
    const currentAllowed = isAllowed || leaderUnitIds.includes(node.id);
    if (currentAllowed) {
      allowed.add(node.id);
    }
    if (node.children) {
      node.children.forEach((child: any) => collectDescendants(child, currentAllowed));
    }
  };
  orgStore.tree.forEach(root => collectDescendants(root, false));

  const units: {id: string, name: string}[] = [];
  const traverse = (node: any, depth = 0) => {
    if (allowed.has(node.id)) {
      units.push({ id: node.id, name: depth > 0 ? `${'-'.repeat(depth)} ${node.name}` : node.name });
    }
    if (node.children) {
      node.children.forEach((child: any) => traverse(child, depth + 1));
    }
  };
  orgStore.tree.forEach(root => traverse(root, 0));
  return units;
});
const isUnitsDropdownOpen = ref(false);
const toggleUnitInvite = (unitId: string) => {
  const index = newEventForm.value.invitedUnitIds.indexOf(unitId);
  if (index === -1) {
    newEventForm.value.invitedUnitIds.push(unitId);
  } else {
    newEventForm.value.invitedUnitIds.splice(index, 1);
  }
};
const isUnitSelected = (unitId: string) => newEventForm.value.invitedUnitIds.includes(unitId);
const fetchEvents = async () => {
  isLoading.value = true;
  try {
    events.value = await eventsService.getEvents(props.unitId);
    events.value.forEach(e => {
      if (e.currentUserRsvp && e.currentUserRsvp !== 'NotResponded') {
        userRsvps.value[e.id] = e.currentUserRsvp;
      }
    });
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

const dateLabel = (iso: string) => {
  if (isToday(iso)) return 'Today';
  if (isTomorrow(iso)) return 'Tomorrow';
  return formatDate(iso);
};
const rsvpOptions = [
  { status: 'Present', label: 'Going', icon: CheckCircle2, color: 'text-emerald-400 border-emerald-500/30 bg-emerald-500/10 hover:bg-emerald-500/20' },
  { status: 'Maybe',   label: 'Maybe',  icon: HelpCircle,   color: 'text-amber-400 border-amber-500/30 bg-amber-500/10 hover:bg-amber-500/20' },
  { status: 'Absent',  label: 'Not Going', icon: XCircle,   color: 'text-red-400 border-red-500/30 bg-red-500/10 hover:bg-red-500/20' },
];

const submitRsvp = async (eventId: string, status: 'Present' | 'Absent' | 'Maybe') => {
  rsvpLoading.value = eventId;
  try {
    await eventsService.rsvp(props.unitId, eventId, status);
    userRsvps.value[eventId] = status;
    toastStore.showToast(`RSVP updated: ${status === 'Present' ? 'Going!' : status === 'Absent' ? 'Not going.' : 'Maybe.'}`, 'success');
  } catch (err: any) {
    toastStore.showToast(err.response?.data?.error || 'Failed to update RSVP.', 'error');
  } finally {
    rsvpLoading.value = null;
  }
};
const toggleExpand = async (eventId: string) => {
  if (expandedEventId.value === eventId) {
    expandedEventId.value = null;
    return;
  }
  expandedEventId.value = eventId;
  if (props.isLeader && !attendanceReports.value[eventId]) {
    loadingAttendance.value = eventId;
    try {
      attendanceReports.value[eventId] = await eventsService.getAttendanceReport(props.unitId, eventId);
    } catch {
    } finally {
      loadingAttendance.value = null;
    }
  }
};

const confirmAttendanceAsLeader = async (eventId: string, userId: string, currentStatus: string) => {
  let nextStatus = 'Present';
  if (currentStatus === 'Present') nextStatus = 'Absent';
  if (currentStatus === 'Absent') nextStatus = 'Maybe';
  if (currentStatus === 'Maybe') nextStatus = 'Present';

  try {
    await eventsService.confirmAttendance(props.unitId, eventId, userId, nextStatus);
    const report = attendanceReports.value[eventId];
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
const openCreateModal = () => {
  createError.value = '';
  newEventForm.value = {
    title: '', description: '',
    startDate: '', endDate: '',
    isRecurring: false, recurrencePattern: null,
    invitedUnitIds: [],
    invitedUserIds: [],
  };
  userSearchQuery.value = '';
  searchedUsers.value = [];
  selectedUsers.value = [];
  isCreateModalOpen.value = true;
};

const submitCreateEvent = async () => {
  if (!newEventForm.value.title || !newEventForm.value.startDate || !newEventForm.value.endDate) {
    createError.value = 'Title, start date and end date are required.';
    return;
  }
  if (new Date(newEventForm.value.endDate) <= new Date(newEventForm.value.startDate)) {
    createError.value = 'End date must be after start date.';
    return;
  }

  createError.value = '';
  isCreating.value = true;
  try {
    const newEvent = await eventsService.createEvent(props.unitId, {
      title: newEventForm.value.title,
      description: newEventForm.value.description,
      startDate: new Date(newEventForm.value.startDate).toISOString(),
      endDate: new Date(newEventForm.value.endDate).toISOString(),
      isRecurring: newEventForm.value.isRecurring,
      recurrencePattern: newEventForm.value.isRecurring ? newEventForm.value.recurrencePattern : null,
      externalCalendarId: null, // Reserved for Google Calendar integration
      invitedUnitIds: newEventForm.value.invitedUnitIds,
      invitedUserIds: newEventForm.value.invitedUserIds,
    });
    events.value.push(newEvent);
    isCreateModalOpen.value = false;
    toastStore.showToast('Event created successfully!', 'success');
  } catch (err: any) {
    createError.value = err.response?.data?.error || 'Failed to create event.';
  } finally {
    isCreating.value = false;
  }
};
</script>

<template>
  <div class="space-y-6">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h2 class="text-lg font-semibold text-white">Events & Meetings</h2>
        <p class="text-sm text-gray-500 mt-0.5">
          {{ upcomingEvents.length }} upcoming · {{ pastEvents.length }} past
        </p>
      </div>
      <button
        v-if="isLeader"
        @click="openCreateModal"
        class="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-medium rounded-lg transition-all shadow-lg shadow-emerald-500/20"
      >
        <Plus class="w-4 h-4" />
        New Event
      </button>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex justify-center py-16">
      <Loader2 class="w-8 h-8 text-emerald-500 animate-spin" />
    </div>

    <template v-else>
      <!-- Empty state -->
      <div v-if="events.length === 0" class="flex flex-col items-center justify-center py-20 text-center">
        <div class="w-16 h-16 bg-dark-border rounded-2xl flex items-center justify-center mb-4">
          <CalendarDays class="w-8 h-8 text-gray-600" />
        </div>
        <p class="text-gray-400 font-medium">No events scheduled</p>
        <p class="text-gray-600 text-sm mt-1">
          {{ isLeader ? 'Create the first event for your team.' : 'Your leader has not scheduled any events yet.' }}
        </p>
      </div>

      <template v-else>
        <!-- Upcoming Events -->
        <section v-if="upcomingEvents.length > 0">
          <h3 class="text-xs font-semibold uppercase tracking-widest text-gray-500 mb-3">Upcoming</h3>
          <div class="space-y-3">
            <div
              v-for="event in upcomingEvents"
              :key="event.id"
              class="bg-dark-bg border rounded-xl overflow-hidden transition-all"
              :class="isToday(event.startDate)
                ? 'border-emerald-500/40 shadow-lg shadow-emerald-500/10'
                : 'border-dark-border hover:border-dark-border/80'"
            >
              <!-- Event Card Header -->
              <div class="p-4">
                <div class="flex items-start justify-between gap-4">
                  <!-- Left: Date Block -->
                  <div class="flex items-start gap-4">
                    <div class="flex-shrink-0 w-12 text-center">
                      <div class="text-xs font-medium uppercase text-gray-500">
                        {{ new Date(event.startDate).toLocaleString('en-GB', { month: 'short' }) }}
                      </div>
                      <div class="text-2xl font-black leading-tight"
                        :class="isToday(event.startDate) ? 'text-emerald-400' : 'text-white'"
                      >
                        {{ new Date(event.startDate).getDate() }}
                      </div>
                    </div>

                    <!-- Content -->
                    <div class="flex-1 min-w-0">
                      <div class="flex items-center gap-2 mb-1 flex-wrap">
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
                        <span v-if="event.targetAudience === 1"
                          class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-orange-500/15 text-orange-400 border border-orange-500/20">
                          Leadership
                        </span>
                        <span v-if="event.targetAudience === 2"
                          class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider bg-red-500/15 text-red-400 border border-red-500/20">
                          EB Only
                        </span>
                      </div>
                      <h4 class="text-white font-semibold text-sm leading-tight">{{ event.title }}</h4>
                      <p v-if="event.description" class="text-xs text-gray-500 mt-0.5 line-clamp-1">{{ event.description }}</p>
                      <div class="flex items-center gap-3 mt-2">
                        <span class="flex items-center gap-1 text-xs text-gray-500">
                          <Clock class="w-3 h-3" />
                          {{ formatTime(event.startDate) }} – {{ formatTime(event.endDate) }}
                        </span>
                        <span class="flex items-center gap-1 text-xs text-gray-500">
                          <AlarmClock class="w-3 h-3" />
                          {{ formatDuration(event.startDate, event.endDate) }}
                        </span>
                      </div>
                    </div>
                  </div>

                  <!-- Right: expand -->
                  <button @click="toggleExpand(event.id)" class="text-gray-500 hover:text-white transition-colors flex-shrink-0 mt-1">
                    <ChevronDown v-if="expandedEventId !== event.id" class="w-4 h-4" />
                    <ChevronUp v-else class="w-4 h-4" />
                  </button>
                </div>

                <!-- RSVP Buttons -->
                <div class="flex items-center gap-2 mt-4 pt-4 border-t border-dark-border/50">
                  <span class="text-xs text-gray-500 mr-1">Your RSVP:</span>
                  <button
                    v-for="opt in rsvpOptions"
                    :key="opt.status"
                    @click="submitRsvp(event.id, opt.status as any)"
                    :disabled="rsvpLoading === event.id"
                    class="flex items-center gap-1.5 px-3 py-1.5 rounded-lg text-xs font-medium border transition-all disabled:opacity-50"
                    :class="[opt.color, userRsvps[event.id] === opt.status ? 'ring-2 ring-offset-1 ring-offset-dark-bg ring-current' : '']"
                  >
                    <component :is="opt.icon" class="w-3.5 h-3.5" />
                    {{ opt.label }}
                  </button>
                  <Loader2 v-if="rsvpLoading === event.id" class="w-4 h-4 text-gray-400 animate-spin ml-1" />
                </div>
              </div>

              <!-- Expanded: Attendance report (leaders only) -->
              <div v-if="expandedEventId === event.id" class="border-t border-dark-border bg-dark-surface/50 p-4">
                <div v-if="loadingAttendance === event.id" class="flex justify-center py-4">
                  <Loader2 class="w-5 h-5 animate-spin text-gray-500" />
                </div>
                <template v-else-if="isLeader && attendanceReports[event.id]">
                  <h5 class="text-xs font-semibold text-gray-400 uppercase tracking-wider mb-3 flex items-center justify-between">
                    <span class="flex items-center gap-2"><Users class="w-3.5 h-3.5" /> Attendance Report</span>
                    <span class="text-[10px] normal-case text-gray-500">Click status to change</span>
                  </h5>
                  <div v-if="attendanceReports[event.id].length === 0" class="text-sm text-gray-500 text-center py-2">
                    No members found in this unit.
                  </div>
                  <div v-else class="space-y-2">
                    <div
                      v-for="item in attendanceReports[event.id]"
                      :key="item.userId"
                      class="flex items-center justify-between"
                    >
                      <div class="flex items-center gap-2">
                        <div class="w-6 h-6 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center text-[10px] font-bold">
                          {{ item.userName.substring(0, 2).toUpperCase() }}
                        </div>
                        <span class="text-sm text-gray-300">{{ item.userName }}</span>
                      </div>
                      <button 
                        @click="confirmAttendanceAsLeader(event.id, item.userId, item.status)"
                        class="px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border hover:scale-105 transition-transform"
                        :class="statusBadge(item.status)">
                        {{ item.status === 'NotResponded' ? 'No RSVP' : item.status }}
                      </button>
                    </div>
                  </div>
                </template>
                <div v-else-if="!isLeader" class="text-sm text-gray-500 text-center py-2">
                  <p class="text-gray-400 font-medium">{{ event.title }}</p>
                  <p class="text-xs mt-1">{{ event.description }}</p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- Past Events -->
        <section v-if="pastEvents.length > 0" class="mt-6">
          <h3 class="text-xs font-semibold uppercase tracking-widest text-gray-500 mb-3">Past Events</h3>
          <div class="space-y-2">
            <div
              v-for="event in pastEvents"
              :key="event.id"
              class="bg-dark-bg border border-dark-border rounded-xl p-4 opacity-60 hover:opacity-80 transition-opacity"
            >
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <Calendar class="w-4 h-4 text-gray-500 flex-shrink-0" />
                  <div>
                    <p class="text-sm font-medium text-gray-300">{{ event.title }}</p>
                    <p class="text-xs text-gray-600">{{ formatDate(event.startDate) }} · {{ formatDuration(event.startDate, event.endDate) }}</p>
                  </div>
                </div>
                <span class="text-[10px] font-bold uppercase text-gray-600 border border-gray-700 px-2 py-0.5 rounded">Past</span>
              </div>
            </div>
          </div>
        </section>
      </template>
    </template>

  </div>

  <!-- ==================== Create Event Modal ==================== -->
  <Teleport to="body">
    <div v-if="isCreateModalOpen"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
      @click.self="isCreateModalOpen = false"
    >
      <div class="bg-dark-surface border border-dark-border rounded-2xl shadow-2xl w-full max-w-lg flex flex-col max-h-[90vh] overflow-hidden">
        <!-- Modal Header -->
        <div class="flex items-center justify-between p-6 border-b border-dark-border">
          <div class="flex items-center gap-3">
            <div class="w-9 h-9 bg-blue-500/10 rounded-xl flex items-center justify-center">
              <CalendarDays class="w-5 h-5 text-blue-400" />
            </div>
            <div>
              <h3 class="text-white font-semibold">Create Event</h3>
              <p class="text-xs text-gray-500">Schedule a meeting or activity for your team</p>
            </div>
          </div>
          <button @click="isCreateModalOpen = false" class="text-gray-500 hover:text-white transition-colors">
            <X class="w-5 h-5" />
          </button>
        </div>

        <!-- Modal Body -->
        <div class="p-6 space-y-4 flex-1 overflow-y-auto custom-scrollbar">
          <!-- Error -->
          <div v-if="createError" class="flex items-center gap-2 p-3 bg-red-500/10 border border-red-500/20 rounded-lg">
            <XCircle class="w-4 h-4 text-red-400 flex-shrink-0" />
            <p class="text-sm text-red-400">{{ createError }}</p>
          </div>

          <!-- Title -->
          <div>
            <label class="block text-xs font-medium text-gray-400 mb-1.5">Event Title *</label>
            <input
              v-model="newEventForm.title"
              placeholder="e.g., Weekly Team Sync"
              class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2.5 text-sm text-white placeholder-gray-600 focus:border-blue-500 outline-none transition-colors"
            />
          </div>

          <!-- Description -->
          <div>
            <label class="block text-xs font-medium text-gray-400 mb-1.5">Description</label>
            <textarea
              v-model="newEventForm.description"
              placeholder="What's this event about?"
              rows="2"
              class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2.5 text-sm text-white placeholder-gray-600 focus:border-blue-500 outline-none transition-colors resize-none"
            />
          </div>

          <!-- Date/Time row -->
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium text-gray-400 mb-1.5">Start *</label>
              <input
                v-model="newEventForm.startDate"
                type="datetime-local"
                class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2.5 text-sm text-white focus:border-blue-500 outline-none transition-colors"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-gray-400 mb-1.5">End *</label>
              <input
                v-model="newEventForm.endDate"
                type="datetime-local"
                class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2.5 text-sm text-white focus:border-blue-500 outline-none transition-colors"
              />
            </div>
          </div>

          <!-- Invited Units -->
          <div class="relative">
            <label class="block text-xs font-medium text-gray-400 mb-1.5">Invite Entire Units / Teams</label>
            <div 
              @click="isUnitsDropdownOpen = !isUnitsDropdownOpen"
              class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2 text-sm text-white cursor-pointer flex justify-between items-center"
            >
              <span class="text-gray-400" v-if="newEventForm.invitedUnitIds.length === 0">Select units...</span>
              <span v-else>{{ newEventForm.invitedUnitIds.length }} unit(s) selected</span>
              <ChevronDown class="w-4 h-4 text-gray-500" />
            </div>
            
            <div v-if="isUnitsDropdownOpen" class="absolute z-10 mt-1 w-full bg-dark-bg border border-dark-border rounded-lg max-h-40 overflow-y-auto shadow-xl">
              <div
                v-for="unit in allUnits"
                :key="unit.id"
                @click="toggleUnitInvite(unit.id)"
                class="px-3 py-2 flex items-center justify-between cursor-pointer hover:bg-dark-border/50 transition-colors"
              >
                <span class="text-sm text-white whitespace-pre">{{ unit.name }}</span>
                <CheckCircle2 v-if="isUnitSelected(unit.id)" class="w-4 h-4 text-emerald-500" />
              </div>
            </div>
          </div>

          <!-- Invited Users -->
          <div>
            <label class="block text-xs font-medium text-gray-400 mb-1.5">Invite Specific Members</label>
            <div class="relative">
              <input
                v-model="userSearchQuery"
                @input="onUserSearch"
                placeholder="Search by name or email..."
                class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:border-blue-500 outline-none transition-colors"
              />
              <Loader2 v-if="isSearchingUsers" class="absolute right-3 top-2.5 w-4 h-4 text-blue-500 animate-spin" />
            </div>
            
            <!-- Search Results Dropdown -->
            <div v-if="searchedUsers.length > 0" class="mt-1 bg-dark-bg border border-dark-border rounded-lg max-h-40 overflow-y-auto">
              <div
                v-for="user in searchedUsers"
                :key="user.id"
                @click="toggleUserInvite(user)"
                class="px-3 py-2 flex items-center justify-between cursor-pointer hover:bg-dark-border/50"
              >
                <div>
                  <p class="text-sm text-white">{{ user.firstName }} {{ user.lastName }}</p>
                  <p class="text-xs text-gray-500">{{ user.email }}</p>
                </div>
                <CheckCircle2 v-if="isUserSelected(user.id)" class="w-4 h-4 text-emerald-500" />
              </div>
            </div>

            <!-- Selected Users Badges -->
            <div v-if="selectedUsers.length > 0" class="flex flex-wrap gap-2 mt-2">
              <span
                v-for="user in selectedUsers"
                :key="user.id"
                class="inline-flex items-center gap-1.5 px-2 py-1 rounded bg-blue-500/10 border border-blue-500/20 text-xs text-blue-400"
              >
                {{ user.firstName }} {{ user.lastName }}
                <button @click="toggleUserInvite(user)" class="hover:text-red-400">
                  <X class="w-3 h-3" />
                </button>
              </span>
            </div>
          </div>

          <!-- Recurring toggle -->
          <div class="flex items-center gap-3 p-3 bg-dark-bg rounded-lg border border-dark-border">
            <Repeat class="w-4 h-4 text-purple-400 flex-shrink-0" />
            <div class="flex-1">
              <p class="text-sm text-white font-medium">Recurring Event</p>
              <p class="text-xs text-gray-500">Repeat this event on a schedule</p>
            </div>
            <button
              @click="newEventForm.isRecurring = !newEventForm.isRecurring"
              class="relative w-10 h-5.5 rounded-full transition-colors flex-shrink-0 focus:outline-none"
              :class="newEventForm.isRecurring ? 'bg-purple-500' : 'bg-dark-border'"
            >
              <span class="absolute top-0.5 left-0.5 w-4 h-4 rounded-full bg-white transition-transform shadow-sm"
                :class="newEventForm.isRecurring ? 'translate-x-4.5' : 'translate-x-0'" />
            </button>
          </div>

          <!-- Recurrence Pattern (shown only if recurring) -->
          <div v-if="newEventForm.isRecurring">
            <label class="block text-xs font-medium text-gray-400 mb-1.5">Recurrence Pattern</label>
            <select
              v-model="newEventForm.recurrencePattern"
              class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2.5 text-sm text-white focus:border-blue-500 outline-none transition-colors"
            >
              <option value="WEEKLY">Weekly</option>
              <option value="BIWEEKLY">Bi-weekly</option>
              <option value="MONTHLY">Monthly</option>
            </select>
          </div>

          <!-- Google Calendar note -->
          <div class="flex items-start gap-2 p-3 bg-blue-500/5 border border-blue-500/15 rounded-lg">
            <CalendarDays class="w-4 h-4 text-blue-400 flex-shrink-0 mt-0.5" />
            <p class="text-xs text-gray-400">
              <span class="text-blue-400 font-medium">Google Calendar sync</span> — coming soon! 
              Events will be exportable directly to your Google Calendar.
            </p>
          </div>
        </div>

        <!-- Modal Footer -->
        <div class="flex items-center justify-end gap-3 p-6 border-t border-dark-border">
          <button
            @click="isCreateModalOpen = false"
            class="px-4 py-2 text-sm text-gray-400 hover:text-white transition-colors"
          >
            Cancel
          </button>
          <button
            @click="submitCreateEvent"
            :disabled="isCreating"
            class="flex items-center gap-2 px-5 py-2 bg-blue-600 hover:bg-blue-500 text-white text-sm font-medium rounded-lg transition-all disabled:opacity-50 shadow-lg shadow-blue-500/20"
          >
            <Loader2 v-if="isCreating" class="w-4 h-4 animate-spin" />
            <CalendarDays v-else class="w-4 h-4" />
            {{ isCreating ? 'Creating...' : 'Create Event' }}
          </button>
        </div>
      </div>
    </div>
  </Teleport>
</template>

<style scoped>
/* Custom Scrollbar */
.custom-scrollbar::-webkit-scrollbar {
  width: 4px;
  height: 6px;
}
.custom-scrollbar::-webkit-scrollbar-track {
  background: transparent;
}
.custom-scrollbar::-webkit-scrollbar-thumb {
  background-color: #374151;
  border-radius: 10px;
}
.custom-scrollbar:hover::-webkit-scrollbar-thumb {
  background-color: #4b5563;
}
</style>
