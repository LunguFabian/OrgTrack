<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { organizationService } from '../api/services/organization.service';
import type { OrganizationUnitDto } from '../types/organization';
import type { UnitMemberDto } from '../types/unit';
import UnitMembersTab from '../components/unit/UnitMembersTab.vue';
import KanbanBoard from '../components/unit/KanbanBoard.vue';
import EventsTab from '../components/unit/EventsTab.vue';
import AnalyticsTab from '../components/unit/AnalyticsTab.vue';
import { Users, KanbanSquare, CalendarDays, BarChart, ArrowLeft, Settings, Globe, Building2, Layers, Shield, RefreshCw } from 'lucide-vue-next';
import { useAuthStore } from '../stores/authStore';
import { api } from '../api/axios';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const unitId = route.params.id as string;
const currentTab = ref('members');
const unitData = ref<OrganizationUnitDto | null>(null);
const isLoading = ref(true);
const isRefreshing = ref(false);
const error = ref('');
const currentUserRole = computed<string | null>(() => {
  if (!unitData.value || !authStore.user) return null;
  const currentUserId = authStore.user.id;
  const me = unitData.value.members?.find((m: any) => m.userId === currentUserId);
  return me?.roleName ?? null;
});
const isLeader = computed(() => {
  const role = currentUserRole.value;
  if (role === null) return true;
  return role !== 'Member';
});
const tabs = computed(() => {
  const allTabs = [
    { id: 'members', name: 'Members', icon: Users },
    { id: 'kanban', name: 'Task Board', icon: KanbanSquare },
    { id: 'events', name: 'Events', icon: CalendarDays },
    { id: 'analytics', name: 'Analytics', icon: BarChart }
  ];
  if (currentUserRole.value === 'Member') {
    return allTabs.filter(t => t.id === 'members' || t.id === 'kanban');
  }
  return allTabs;
});

const fetchUnit = async (silent = false) => {
  if (!silent) isLoading.value = true;
  else isRefreshing.value = true;
  error.value = '';
  try {
    unitData.value = await organizationService.getUnitById(unitId);
    if (!tabs.value.find(t => t.id === currentTab.value)) {
      currentTab.value = 'members';
    }
  } catch (err: any) {
    error.value = err.response?.data?.error || 'Failed to load unit details';
  } finally {
    isLoading.value = false;
    isRefreshing.value = false;
  }
};

onMounted(() => fetchUnit());
const unitStyling = computed(() => {
  switch (unitData.value?.type) {
    case 'National': return { icon: Globe, color: 'text-blue-400', bg: 'bg-blue-500/10', border: 'border-blue-500/30' };
    case 'Committee': return { icon: Building2, color: 'text-emerald-400', bg: 'bg-emerald-500/10', border: 'border-emerald-500/30' };
    case 'Department': return { icon: Layers, color: 'text-purple-400', bg: 'bg-purple-500/10', border: 'border-purple-500/30' };
    case 'Team': return { icon: Shield, color: 'text-orange-400', bg: 'bg-orange-500/10', border: 'border-orange-500/30' };
    default: return { icon: Globe, color: 'text-text-muted', bg: 'bg-gray-500/10', border: 'border-gray-500/30' };
  }
});
const members = computed<UnitMemberDto[]>(() => (unitData.value?.members ?? []) as UnitMemberDto[]);

const handleMemberRemoved = async (userId: string) => {
  try {
    await api.delete(`/organization/units/${unitId}/members/${userId}`);
    await fetchUnit(true);
  } catch (err: any) {
    console.error('Failed to remove member:', err.response?.data?.error);
  }
};

const handleRoleUpdated = () => {
  fetchUnit(true);
};
</script>

<template>
  <div class="space-y-6 max-w-7xl mx-auto pb-20">
    <!-- Back Button -->
    <button
      @click="router.push('/organization')"
      class="flex items-center gap-2 text-sm text-text-muted hover:text-text-strong transition-colors group"
    >
      <ArrowLeft class="w-4 h-4 group-hover:-translate-x-0.5 transition-transform" />
      Back to Organization
    </button>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-4">
        <div class="w-10 h-10 border-4 border-emerald-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-text-muted">Loading dashboard...</p>
      </div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="bg-red-500/10 border border-red-500/20 p-6 rounded-2xl text-center">
      <p class="text-red-400">{{ error }}</p>
      <button @click="router.push('/organization')" class="mt-4 text-emerald-400 hover:underline text-sm">Go back</button>
    </div>

    <!-- Main Content -->
    <template v-else-if="unitData">
      <!-- Header Card -->
      <div class="bg-surface p-6 rounded-2xl border border-border shadow-sm">
        <div class="flex items-start justify-between">
          <div class="flex items-start gap-4">
            <div :class="['p-3.5 rounded-xl', unitStyling.bg, unitStyling.border, 'border']">
              <component :is="unitStyling.icon" :class="['w-7 h-7', unitStyling.color]" />
            </div>
            <div>
              <div class="flex items-center gap-2 mb-1">
                <span :class="['px-2.5 py-0.5 rounded-md text-xs font-medium border', unitStyling.bg, unitStyling.color, unitStyling.border]">
                  {{ unitData.type }}
                </span>
                <span v-if="unitData.departmentType && unitData.departmentType !== 'None'"
                  class="px-2.5 py-0.5 rounded-md text-xs font-medium bg-border text-text-muted">
                  {{ unitData.departmentType }}
                </span>
                <!-- Current user role badge -->
                <span v-if="currentUserRole"
                  class="px-2.5 py-0.5 rounded-md text-xs font-medium bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                  You: {{ currentUserRole }}
                </span>
              </div>
              <h1 class="text-3xl font-bold text-text-strong tracking-tight">{{ unitData.name }}</h1>
              <p class="text-text-muted text-sm mt-1 max-w-xl">{{ unitData.description || 'No description provided.' }}</p>
            </div>
          </div>

          <button class="p-2 rounded-lg text-text-muted hover:text-text-strong hover:bg-border transition-colors" title="Unit Settings">
            <Settings class="w-5 h-5" />
          </button>
          <button
            @click="fetchUnit(true)"
            :class="['p-2 rounded-lg text-text-muted hover:text-text-strong hover:bg-border transition-colors', isRefreshing ? 'animate-spin' : '']"
            title="Refresh members"
          >
            <RefreshCw class="w-5 h-5" />
          </button>
        </div>

        <!-- Stats Row -->
        <div class="flex items-center gap-6 mt-6 pt-6 border-t border-border">
          <div class="flex items-center gap-2 text-sm">
            <Users class="w-4 h-4 text-text-muted" />
            <span class="text-text-strong font-semibold">{{ members.length }}</span>
            <span class="text-text-muted">member{{ members.length !== 1 ? 's' : '' }}</span>
          </div>
          <div class="flex items-center gap-2 text-sm">
            <KanbanSquare class="w-4 h-4 text-text-muted" />
            <span class="text-text-strong font-semibold">{{ unitData.children?.length ?? 0 }}</span>
            <span class="text-text-muted">sub-unit{{ (unitData.children?.length ?? 0) !== 1 ? 's' : '' }}</span>
          </div>
        </div>

        <!-- Navigation Tabs -->
        <div class="flex items-center gap-6 mt-6 border-b border-border">
          <button
            v-for="tab in tabs"
            :key="tab.id"
            @click="currentTab = tab.id"
            class="flex items-center gap-2 pb-4 text-sm font-medium transition-colors relative"
            :class="currentTab === tab.id ? 'text-emerald-400' : 'text-text-muted hover:text-text'"
          >
            <component :is="tab.icon" class="w-4 h-4" />
            {{ tab.name }}
            <div v-if="currentTab === tab.id"
              class="absolute bottom-0 left-0 right-0 h-0.5 bg-emerald-500 rounded-t-full"></div>
          </button>
        </div>
      </div>

      <!-- Tab Content -->
      <div>
        <!-- Members Tab -->
        <div v-if="currentTab === 'members'" class="bg-surface border border-border rounded-2xl p-6">
          <UnitMembersTab
            :unit-id="unitId"
            :members="members"
            :is-loading="isRefreshing"
            @member-removed="handleMemberRemoved"
            @role-updated="handleRoleUpdated"
          />
        </div>

        <!-- Kanban Tab -->
        <div v-else-if="currentTab === 'kanban'" class="bg-surface border border-border rounded-2xl p-6">
          <KanbanBoard :unit-id="unitId" :members="members" />
        </div>

        <!-- Events Tab -->
        <div v-else-if="currentTab === 'events'" class="bg-surface border border-border rounded-2xl p-6">
          <EventsTab :unit-id="unitId" :is-leader="isLeader" />
        </div>

        <!-- Analytics Tab -->
        <div v-else-if="currentTab === 'analytics'" class="bg-surface border border-border rounded-2xl p-6">
          <AnalyticsTab :unit-id="unitId" :members="members" :unit-type="unitData.type" />
        </div>
      </div>
    </template>
  </div>
</template>
