<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { eventsService, type AttendanceReportItem } from '../../api/services/events.service';
import type { EventDto } from '../../types/unit';
import { useOrgStore } from '../../stores/orgStore';
import { useToastStore } from '../../stores/toastStore';
import { organizationService } from '../../api/services/organization.service';
import {
  Clock, Users, ChevronDown, ChevronUp, CalendarDays, Plus, AlarmClock, Repeat, Trash2,
  CheckCircle2, XCircle, HelpCircle, Loader2, Calendar, X, Pencil
} from 'lucide-vue-next';

const props = defineProps<{
  unitId: string;
  isLeader: boolean;
}>();

const orgStore = useOrgStore();
const toastStore = useToastStore();

// --- Core state ---
const events = ref<EventDto[]>([]);
const isLoading = ref(true);
const expandedEventId = ref<string | null>(null);
const attendanceReports = ref<Record<string, AttendanceReportItem[]>>({});
const loadingAttendance = ref<string | null>(null);
const rsvpLoading = ref<string | null>(null);
const userRsvps = ref<Record<string, string>>({});

// --- Delete confirmation state (inline, no browser alert) ---
const confirmingDeleteId = ref<string | null>(null);

// --- Modal state ---
const isEventModalOpen = ref(false);
const isEditingEvent = ref(false);
const editingEventId = ref<string | null>(null);
const isCreating = ref(false);
const createError = ref('');
const eventForm = ref({
  id: undefined as string | undefined,
  title: '',
  description: '',
  startDate: '',
  endDate: '',
  isRecurring: false,
  recurrencePattern: null as string | null,
  invitedUnitIds: [] as string[],
  invitedUserIds: [] as string[],
});

// --- Invite users search ---
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

const toggleUserInvite = (user: {id: string, firstName: string, lastName: string, email: string}) => {
  const index = selectedUsers.value.findIndex(u => u.id === user.id);
  if (index === -1) {
    selectedUsers.value.push(user);
    eventForm.value.invitedUserIds.push(user.id);
  } else {
    selectedUsers.value.splice(index, 1);
    eventForm.value.invitedUserIds = eventForm.value.invitedUserIds.filter(id => id !== user.id);
  }
};

const isUserSelected = (userId: string) => eventForm.value.invitedUserIds.includes(userId);

// --- Invite units ---
const allUnits = computed(() => {
  const units: {id: string, name: string}[] = [];
  const traverse = (node: any, depth = 0) => {
    units.push({ id: node.id, name: depth > 0 ? `${'—'.repeat(depth)} ${node.name}` : node.name });
    if (node.children) {
      node.children.forEach((child: any) => traverse(child, depth + 1));
    }
  };
  orgStore.tree.forEach((root: any) => traverse(root, 0));
  return units;
});
const isUnitsDropdownOpen = ref(false);
const toggleUnitInvite = (unitId: string) => {
  const index = eventForm.value.invitedUnitIds.indexOf(unitId);
  if (index === -1) {
    eventForm.value.invitedUnitIds.push(unitId);
  } else {
    eventForm.value.invitedUnitIds.splice(index, 1);
  }
};
const isUnitSelected = (unitId: string) => eventForm.value.invitedUnitIds.includes(unitId);

// --- Data fetching ---
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

// --- RSVP ---
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

// --- Expand / Attendance ---
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
    const item = report.find((r: any) => r.userId === userId);
    if (item) item.status = nextStatus;
    toastStore.showToast('Attendance updated', 'success');
  } catch (err: any) {
    toastStore.showToast(err.response?.data?.error || 'Failed to update attendance', 'error');
  }
};

const statusBadge = (status: string) => {
  if (status === 'Present') return 'bg-emerald-500/10 text-emerald-400 border-emerald-500/20';
  if (status === 'Absent')  return 'bg-red-500/10 text-red-400 border-red-500/20';
  if (status === 'NotResponded') return 'bg-gray-500/10 text-text-muted border-gray-500/20';
  return 'bg-amber-500/10 text-amber-400 border-amber-500/20';
};

// --- Create / Edit modal ---
const openCreateModal = () => {
  createError.value = '';
  isEditingEvent.value = false;
  editingEventId.value = null;
  eventForm.value = {
    id: undefined,
    title: '', description: '',
    startDate: '', endDate: '',
    isRecurring: false, recurrencePattern: null,
    invitedUnitIds: [], invitedUserIds: [],
  };
  userSearchQuery.value = '';
  searchedUsers.value = [];
  selectedUsers.value = [];
  isUnitsDropdownOpen.value = false;
  isEventModalOpen.value = true;
};

const openEditModal = (event: EventDto) => {
  createError.value = '';
  isEditingEvent.value = true;
  editingEventId.value = event.id;
  eventForm.value = {
    id: event.id,
    title: event.title,
    description: event.description,
    startDate: new Date(event.startDate).toISOString().slice(0, 16),
    endDate: new Date(event.endDate).toISOString().slice(0, 16),
    isRecurring: event.isRecurring,
    recurrencePattern: event.recurrencePattern || null,
    invitedUnitIds: event.invitedUnitIds ? [...event.invitedUnitIds] : [],
    invitedUserIds: event.invitedUserIds ? [...event.invitedUserIds] : [],
  };
  userSearchQuery.value = '';
  searchedUsers.value = [];
  selectedUsers.value = [];
  isUnitsDropdownOpen.value = false;
  isEventModalOpen.value = true;
};

const submitEvent = async () => {
  if (!eventForm.value.title || !eventForm.value.startDate || !eventForm.value.endDate) {
    createError.value = 'Title, start date and end date are required.';
    return;
  }
  if (new Date(eventForm.value.endDate) <= new Date(eventForm.value.startDate)) {
    createError.value = 'End date must be after start date.';
    return;
  }

  createError.value = '';
  isCreating.value = true;
  try {
    const payload = {
      title: eventForm.value.title,
      description: eventForm.value.description,
      startDate: new Date(eventForm.value.startDate).toISOString(),
      endDate: new Date(eventForm.value.endDate).toISOString(),
      isRecurring: eventForm.value.isRecurring,
      recurrencePattern: eventForm.value.isRecurring ? eventForm.value.recurrencePattern : null,
      externalCalendarId: null,
      invitedUnitIds: eventForm.value.invitedUnitIds,
      invitedUserIds: eventForm.value.invitedUserIds,
    };

    if (isEditingEvent.value && editingEventId.value) {
      const updatedEvent = await eventsService.updateEvent(props.unitId, editingEventId.value, payload);
      const index = events.value.findIndex(e => e.id === editingEventId.value);
      if (index !== -1) events.value[index] = updatedEvent;
      toastStore.showToast('Event updated successfully!', 'success');
    } else {
      const newEvent = await eventsService.createEvent(props.unitId, payload);
      events.value.push(newEvent);
      toastStore.showToast('Event created successfully!', 'success');
    }
    isEventModalOpen.value = false;
  } catch (err: any) {
    createError.value = err.response?.data?.error || `Failed to ${isEditingEvent.value ? 'update' : 'create'} event.`;
  } finally {
    isCreating.value = false;
  }
};

// --- Delete (no browser confirm — inline) ---
const handleDeleteEvent = async (event: EventDto) => {
  try {
    await eventsService.deleteEvent(props.unitId, event.id);
    events.value = events.value.filter(e => e.id !== event.id);
    confirmingDeleteId.value = null;
    toastStore.showToast('Event deleted successfully.', 'success');
  } catch (err: any) {
    toastStore.showToast(err.response?.data?.error || 'Failed to delete event.', 'error');
  }
};
</script>

<template>
  <div class="space-y-6">

    <!-- Header -->
    <div class="flex items-center justify-between">
      <div>
        <h2 class="text-lg font-semibold text-text-strong">Events & Meetings</h2>
        <p class="text-sm text-text-muted mt-0.5">
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
        <div class="w-16 h-16 bg-border rounded-2xl flex items-center justify-center mb-4">
          <CalendarDays class="w-8 h-8 text-gray-600" />
        </div>
        <p class="text-text-muted font-medium">No events scheduled</p>
        <p class="text-gray-600 text-sm mt-1">
          {{ isLeader ? 'Create the first event for your team.' : 'Your leader has not scheduled any events yet.' }}
        </p>
      </div>

      <template v-else>
        <!-- Upcoming Events -->
        <section v-if="upcomingEvents.length > 0">
          <h3 class="text-xs font-semibold uppercase tracking-widest text-text-muted mb-3">Upcoming</h3>
          <div class="space-y-3">
            <div
              v-for="event in upcomingEvents"
              :key="event.id"
              class="bg-bg border rounded-xl overflow-hidden transition-all"
              :class="isToday(event.startDate)
                ? 'border-emerald-500/40 shadow-lg shadow-emerald-500/10'
                : 'border-border hover:border-border/80'"
            >
              <!-- Event Card Header -->
              <div class="p-4">
                <div class="flex items-start justify-between gap-4">
                  <!-- Left: Date Block -->
                  <div class="flex items-start gap-4">
                    <div class="flex-shrink-0 w-12 text-center">
                      <div class="text-xs font-medium uppercase text-text-muted">
                        {{ new Date(event.startDate).toLocaleString('en-GB', { month: 'short' }) }}
                      </div>
                      <div class="text-2xl font-black leading-tight"
                        :class="isToday(event.startDate) ? 'text-emerald-400' : 'text-text-strong'"
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
                      </div>
                      <h4 class="text-text-strong font-semibold text-sm leading-tight">{{ event.title }}</h4>
                      <p v-if="event.description" class="text-xs text-text-muted mt-0.5 line-clamp-1">{{ event.description }}</p>
                      <div class="flex items-center gap-3 mt-2">
                        <span class="flex items-center gap-1 text-xs text-text-muted">
                          <Clock class="w-3 h-3" />
                          {{ formatTime(event.startDate) }} – {{ formatTime(event.endDate) }}
                        </span>
                        <span class="flex items-center gap-1 text-xs text-text-muted">
                          <AlarmClock class="w-3 h-3" />
                          {{ formatDuration(event.startDate, event.endDate) }}
                        </span>
                      </div>
                    </div>
                  </div>

                  <!-- Right: actions & expand -->
                  <div class="flex items-center gap-1 flex-shrink-0">
                    <button v-if="isLeader" @click="openEditModal(event)" class="p-1.5 text-text-muted hover:text-emerald-400 transition-colors rounded-lg hover:bg-emerald-500/10" title="Edit Event">
                      <Pencil class="w-4 h-4" />
                    </button>
                    <!-- Delete with inline confirmation -->
                    <template v-if="isLeader">
                      <button v-if="confirmingDeleteId !== event.id" @click="confirmingDeleteId = event.id" class="p-1.5 text-text-muted hover:text-red-400 transition-colors rounded-lg hover:bg-red-500/10" title="Delete Event">
                        <Trash2 class="w-4 h-4" />
                      </button>
                      <div v-else class="flex items-center gap-1">
                        <button @click="handleDeleteEvent(event)" class="px-2 py-1 text-[10px] font-bold uppercase bg-red-500/20 text-red-400 border border-red-500/30 rounded-md hover:bg-red-500/30 transition-colors">
                          Delete
                        </button>
                        <button @click="confirmingDeleteId = null" class="px-2 py-1 text-[10px] font-bold uppercase text-text-muted border border-border rounded-md hover:bg-surface-hover transition-colors">
                          Cancel
                        </button>
                      </div>
                    </template>
                    <button @click="toggleExpand(event.id)" class="p-1.5 text-text-muted hover:text-text-strong transition-colors rounded-lg hover:bg-surface-hover">
                      <ChevronDown v-if="expandedEventId !== event.id" class="w-4 h-4" />
                      <ChevronUp v-else class="w-4 h-4" />
                    </button>
                  </div>
                </div>

                <!-- RSVP Buttons -->
                <div class="flex items-center gap-2 mt-4 pt-4 border-t border-border/50">
                  <span class="text-xs text-text-muted mr-1">Your RSVP:</span>
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
                  <Loader2 v-if="rsvpLoading === event.id" class="w-4 h-4 text-text-muted animate-spin ml-1" />
                </div>
              </div>

              <!-- Expanded: Attendance report (leaders only) -->
              <div v-if="expandedEventId === event.id" class="border-t border-border bg-surface/50 p-4">
                <div v-if="loadingAttendance === event.id" class="flex justify-center py-4">
                  <Loader2 class="w-5 h-5 animate-spin text-text-muted" />
                </div>
                <template v-else-if="isLeader && attendanceReports[event.id]">
                  <h5 class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-3 flex items-center justify-between">
                    <span class="flex items-center gap-2"><Users class="w-3.5 h-3.5" /> Attendance Report</span>
                    <span class="text-[10px] normal-case text-text-muted">Click status to change</span>
                  </h5>
                  <div v-if="attendanceReports[event.id].length === 0" class="text-sm text-text-muted text-center py-2">
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
                        <span class="text-sm text-text-muted">{{ item.userName }}</span>
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
                <div v-else-if="!isLeader" class="text-sm text-text-muted text-center py-2">
                  <p class="text-text-muted font-medium">{{ event.title }}</p>
                  <p class="text-xs mt-1">{{ event.description }}</p>
                </div>
              </div>
            </div>
          </div>
        </section>

        <!-- Past Events -->
        <section v-if="pastEvents.length > 0" class="mt-6">
          <h3 class="text-xs font-semibold uppercase tracking-widest text-text-muted mb-3">Past Events</h3>
          <div class="space-y-2">
            <div
              v-for="event in pastEvents"
              :key="event.id"
              class="bg-bg border border-border rounded-xl p-4 opacity-60 hover:opacity-80 transition-opacity"
            >
              <div class="flex items-center justify-between">
                <div class="flex items-center gap-3">
                  <Calendar class="w-4 h-4 text-text-muted flex-shrink-0" />
                  <div>
                    <p class="text-sm font-medium text-text-muted">{{ event.title }}</p>
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

  <!-- ==================== Event Modal (Create / Edit) ==================== -->
  <Teleport to="body">
    <div v-if="isEventModalOpen"
      class="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/60 backdrop-blur-sm"
      @click.self="isEventModalOpen = false"
    >
      <div class="bg-surface border border-border rounded-2xl shadow-2xl w-full max-w-lg flex flex-col max-h-[90vh] overflow-hidden">
        <!-- Modal Header -->
        <div class="flex items-center justify-between p-6 border-b border-border">
          <div class="flex items-center gap-3">
            <div class="w-9 h-9 bg-emerald-500/10 rounded-xl flex items-center justify-center">
              <CalendarDays class="w-5 h-5 text-emerald-400" />
            </div>
            <div>
              <h3 class="text-text-strong font-semibold">{{ isEditingEvent ? 'Edit Event' : 'Create Event' }}</h3>
              <p class="text-xs text-text-muted">{{ isEditingEvent ? 'Update event details and invitations' : 'Schedule a meeting or activity for your team' }}</p>
            </div>
          </div>
          <button @click="isEventModalOpen = false" class="text-text-muted hover:text-text-strong transition-colors">
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
            <label class="block text-xs font-medium text-text-muted mb-1.5">Event Title *</label>
            <input
              v-model="eventForm.title"
              placeholder="e.g., Weekly Team Sync"
              class="w-full bg-bg border border-border rounded-lg px-3 py-2.5 text-sm text-text-strong placeholder-gray-600 focus:border-emerald-500 outline-none transition-colors"
            />
          </div>

          <!-- Description -->
          <div>
            <label class="block text-xs font-medium text-text-muted mb-1.5">Description</label>
            <textarea
              v-model="eventForm.description"
              placeholder="What's this event about?"
              rows="2"
              class="w-full bg-bg border border-border rounded-lg px-3 py-2.5 text-sm text-text-strong placeholder-gray-600 focus:border-emerald-500 outline-none transition-colors resize-none"
            />
          </div>

          <!-- Date/Time row -->
          <div class="grid grid-cols-2 gap-3">
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1.5">Start *</label>
              <input
                v-model="eventForm.startDate"
                type="datetime-local"
                class="w-full bg-bg border border-border rounded-lg px-3 py-2.5 text-sm text-text-strong focus:border-emerald-500 outline-none transition-colors"
                style="color-scheme: dark;"
              />
            </div>
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1.5">End *</label>
              <input
                v-model="eventForm.endDate"
                type="datetime-local"
                class="w-full bg-bg border border-border rounded-lg px-3 py-2.5 text-sm text-text-strong focus:border-emerald-500 outline-none transition-colors"
                style="color-scheme: dark;"
              />
            </div>
          </div>

          <!-- Invited Units -->
          <div class="relative">
            <label class="block text-xs font-medium text-text-muted mb-1.5">Invite Entire Units / Teams</label>
            <div 
              @click="isUnitsDropdownOpen = !isUnitsDropdownOpen"
              class="w-full bg-bg border border-border rounded-lg px-3 py-2 text-sm text-text-strong cursor-pointer flex justify-between items-center"
            >
              <span class="text-text-muted" v-if="eventForm.invitedUnitIds.length === 0">Select units...</span>
              <span v-else>{{ eventForm.invitedUnitIds.length }} unit(s) selected</span>
              <ChevronDown class="w-4 h-4 text-text-muted" />
            </div>
            
            <div v-if="isUnitsDropdownOpen" class="absolute z-10 mt-1 w-full bg-bg border border-border rounded-lg max-h-40 overflow-y-auto shadow-xl">
              <div
                v-for="unit in allUnits"
                :key="unit.id"
                @click="toggleUnitInvite(unit.id)"
                class="px-3 py-2 flex items-center justify-between cursor-pointer hover:bg-surface-hover transition-colors"
              >
                <span class="text-sm text-text-strong whitespace-pre">{{ unit.name }}</span>
                <CheckCircle2 v-if="isUnitSelected(unit.id)" class="w-4 h-4 text-emerald-500" />
              </div>
            </div>
          </div>

          <!-- Invited Users -->
          <div>
            <label class="block text-xs font-medium text-text-muted mb-1.5">Invite Specific Members</label>
            <div class="relative">
              <input
                v-model="userSearchQuery"
                @input="onUserSearch"
                placeholder="Search by name or email..."
                class="w-full bg-bg border border-border rounded-lg px-3 py-2 text-sm text-text-strong placeholder-gray-600 focus:border-emerald-500 outline-none transition-colors"
              />
              <Loader2 v-if="isSearchingUsers" class="absolute right-3 top-2.5 w-4 h-4 text-emerald-500 animate-spin" />
            </div>
            
            <!-- Search Results Dropdown -->
            <div v-if="searchedUsers.length > 0" class="mt-1 bg-bg border border-border rounded-lg max-h-40 overflow-y-auto">
              <div
                v-for="user in searchedUsers"
                :key="user.id"
                @click="toggleUserInvite(user)"
                class="px-3 py-2 flex items-center justify-between cursor-pointer hover:bg-surface-hover"
              >
                <div>
                  <p class="text-sm text-text-strong">{{ user.firstName }} {{ user.lastName }}</p>
                  <p class="text-xs text-text-muted">{{ user.email }}</p>
                </div>
                <CheckCircle2 v-if="isUserSelected(user.id)" class="w-4 h-4 text-emerald-500" />
              </div>
            </div>

            <!-- Selected Users Badges -->
            <div v-if="selectedUsers.length > 0" class="flex flex-wrap gap-2 mt-2">
              <span
                v-for="user in selectedUsers"
                :key="user.id"
                class="inline-flex items-center gap-1.5 px-2 py-1 rounded bg-emerald-500/10 border border-emerald-500/20 text-xs text-emerald-400"
              >
                {{ user.firstName }} {{ user.lastName }}
                <button @click="toggleUserInvite(user)" class="hover:text-red-400">
                  <X class="w-3 h-3" />
                </button>
              </span>
            </div>
          </div>

          <!-- Recurring toggle — styled as a clean toggle row -->
          <div class="flex items-center gap-3 p-3 bg-bg rounded-lg border border-border">
            <Repeat class="w-4 h-4 text-purple-400 flex-shrink-0" />
            <div class="flex-1">
              <p class="text-sm text-text-strong font-medium">Recurring Event</p>
              <p class="text-xs text-text-muted">Repeat this event on a schedule</p>
            </div>
            <button
              @click="eventForm.isRecurring = !eventForm.isRecurring"
              class="relative w-10 h-5 rounded-full transition-colors flex-shrink-0 focus:outline-none"
              :class="eventForm.isRecurring ? 'bg-purple-500' : 'bg-border'"
            >
              <span class="absolute top-0.5 left-0.5 w-4 h-4 rounded-full bg-white transition-transform shadow-sm"
                :class="eventForm.isRecurring ? 'translate-x-5' : 'translate-x-0'" />
            </button>
          </div>

          <!-- Recurrence Pattern (shown only if recurring) -->
          <div v-if="eventForm.isRecurring">
            <label class="block text-xs font-medium text-text-muted mb-1.5">Recurrence Pattern</label>
            <select
              v-model="eventForm.recurrencePattern"
              class="w-full bg-bg border border-border rounded-lg px-3 py-2.5 text-sm text-text-strong focus:border-emerald-500 outline-none transition-colors"
            >
              <option value="WEEKLY">Weekly</option>
              <option value="BIWEEKLY">Bi-weekly</option>
              <option value="MONTHLY">Monthly</option>
            </select>
          </div>
        </div>

        <!-- Modal Footer -->
        <div class="flex items-center justify-end gap-3 p-6 border-t border-border">
          <button
            @click="isEventModalOpen = false"
            class="px-4 py-2 text-sm text-text-muted hover:text-text-strong transition-colors"
          >
            Cancel
          </button>
          <button
            @click="submitEvent"
            :disabled="isCreating"
            class="flex items-center gap-2 px-5 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-medium rounded-lg transition-all disabled:opacity-50 shadow-lg shadow-emerald-500/20"
          >
            <Loader2 v-if="isCreating" class="w-4 h-4 animate-spin" />
            {{ isEditingEvent ? 'Save Changes' : 'Create Event' }}
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
