<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';
import { organizationService } from '../api/services/organization.service';
import { analyticsService, type MemberActivityScoreDto } from '../api/services/analytics.service';
import { Mail, Shield, Building2, Loader2, ArrowLeft, Activity, CheckSquare, CalendarCheck, MessageSquare, Globe, Layers } from 'lucide-vue-next';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const userId = computed(() => route.params.userId as string);
const isOwnProfile = computed(() => userId.value === authStore.user?.id);

const isLoading = ref(true);
const error = ref('');
const profile = ref<{
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  pictureUrl?: string;
  units: Array<{ id: string; name: string; type: string; departmentType: string; roleName: string }>;
} | null>(null);
const score = ref<MemberActivityScoreDto | null>(null);

const unitIcon = (type: string) => {
  switch (type) {
    case 'National': return Globe;
    case 'Committee': return Building2;
    case 'Department': return Layers;
    default: return Shield;
  }
};

const unitColor = (type: string) => {
  switch (type) {
    case 'National': return 'text-blue-400 bg-blue-500/10 border-blue-500/20';
    case 'Committee': return 'text-emerald-400 bg-emerald-500/10 border-emerald-500/20';
    case 'Department': return 'text-purple-400 bg-purple-500/10 border-purple-500/20';
    default: return 'text-orange-400 bg-orange-500/10 border-orange-500/20';
  }
};

onMounted(async () => {
  isLoading.value = true;
  error.value = '';
  try {
    const [profileData, scoreData] = await Promise.all([
      organizationService.getUserProfile(userId.value),
      analyticsService.getMemberScore(userId.value).catch(() => null)
    ]);
    profile.value = profileData;
    score.value = scoreData;
  } catch (err: any) {
    error.value = err.response?.data?.error || 'Failed to load user profile.';
  } finally {
    isLoading.value = false;
  }
});
</script>

<template>
  <div class="max-w-5xl mx-auto space-y-8 pb-20">
    <!-- Header -->
    <div class="flex items-center gap-4 mb-8">
      <button @click="router.back()" class="p-2 rounded-xl border border-border bg-surface hover:bg-bg text-text-muted hover:text-text-strong transition-colors">
        <ArrowLeft class="w-5 h-5" />
      </button>
      <div>
        <h1 class="text-3xl font-bold text-text-strong tracking-tight">{{ isOwnProfile ? 'My Profile' : profile ? `${profile.firstName} ${profile.lastName}'s Profile` : 'Member Profile' }}</h1>
        <p class="text-sm text-text-muted">{{ isOwnProfile ? 'Your activity and roles' : 'View member details and activity' }}</p>
      </div>
    </div>

    <!-- Loading -->
    <div v-if="isLoading" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-4">
        <Loader2 class="w-10 h-10 text-emerald-500 animate-spin" />
        <p class="text-text-muted text-sm">Loading profile...</p>
      </div>
    </div>

    <!-- Error -->
    <div v-else-if="error" class="bg-red-500/10 border border-red-500/20 p-6 rounded-2xl text-center">
      <p class="text-red-400">{{ error }}</p>
      <button @click="router.back()" class="mt-4 text-emerald-400 hover:underline text-sm">Go back</button>
    </div>

    <!-- Profile Content -->
    <div v-else-if="profile" class="grid grid-cols-1 lg:grid-cols-3 gap-8">
      
      <!-- Left Column: User Card -->
      <div class="lg:col-span-1 space-y-6">
        <div class="bg-surface border border-border rounded-2xl p-6 text-center shadow-lg">
          <img 
            v-if="profile.pictureUrl" 
            :src="profile.pictureUrl" 
            alt="Profile Avatar" 
            class="w-24 h-24 mx-auto rounded-full object-cover shadow-xl mb-4 border-4 border-bg"
          />
          <div v-else class="w-24 h-24 mx-auto bg-gradient-to-tr from-emerald-500 to-blue-500 rounded-full flex items-center justify-center text-3xl font-bold text-white shadow-xl mb-4 border-4 border-bg">
            {{ profile.firstName[0] }}{{ profile.lastName[0] }}
          </div>
          <h2 class="text-xl font-bold text-text-strong">{{ profile.firstName }} {{ profile.lastName }}</h2>
          <div class="flex items-center justify-center gap-2 mt-2 text-text-muted text-sm">
            <Mail class="w-4 h-4" />
            {{ profile.email }}
          </div>

          <!-- Send Message Button (disabled for now) -->
          <div v-if="!isOwnProfile" class="mt-5">
            <button 
              disabled
              class="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-xl text-sm font-medium bg-emerald-600/30 text-emerald-400/50 cursor-not-allowed border border-emerald-500/10"
              title="Messaging is coming soon!"
            >
              <MessageSquare class="w-4 h-4" />
              Send Message
              <span class="text-[10px] uppercase tracking-wider bg-emerald-500/10 px-1.5 py-0.5 rounded-md ml-1">Soon</span>
            </button>
          </div>

          <!-- Activity Score -->
          <div v-if="score" class="mt-6 pt-6 border-t border-border text-left">
            <h3 class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Activity Score</h3>
            <div class="flex items-center gap-4">
              <div class="p-3 bg-yellow-500/10 border border-yellow-500/20 rounded-xl">
                <Activity class="w-6 h-6 text-yellow-500" />
              </div>
              <div>
                <span class="text-3xl font-bold text-text-strong">{{ score.totalScore }}</span>
                <span class="text-sm text-text-muted ml-1">pts</span>
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Right Column: Stats & Roles -->
      <div class="lg:col-span-2 space-y-6">
        
        <!-- Detailed Stats -->
        <div v-if="score" class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div class="bg-surface border border-border p-5 rounded-2xl flex items-start gap-4">
            <div class="p-3 bg-emerald-500/10 border border-emerald-500/20 rounded-xl">
              <CheckSquare class="w-5 h-5 text-emerald-400" />
            </div>
            <div>
              <p class="text-sm text-text-muted">Tasks Completed</p>
              <p class="text-2xl font-bold text-text-strong">{{ score.tasksDone }}</p>
              <p class="text-xs text-text-muted mt-1">x3 points per task</p>
            </div>
          </div>

          <div class="bg-surface border border-border p-5 rounded-2xl flex items-start gap-4">
            <div class="p-3 bg-blue-500/10 border border-blue-500/20 rounded-xl">
              <CalendarCheck class="w-5 h-5 text-blue-400" />
            </div>
            <div>
              <p class="text-sm text-text-muted">Events Attended</p>
              <p class="text-2xl font-bold text-text-strong">{{ score.eventsAttended }}</p>
              <p class="text-xs text-text-muted mt-1">x1 point per event</p>
            </div>
          </div>
        </div>

        <!-- Units / Roles -->
        <div class="bg-surface border border-border rounded-2xl overflow-hidden">
          <div class="p-5 border-b border-border flex items-center gap-2">
            <Building2 class="w-5 h-5 text-text-muted" />
            <h3 class="text-sm font-semibold text-text-strong">{{ isOwnProfile ? 'My Roles & Teams' : 'Roles & Teams' }}</h3>
          </div>
          
          <div v-if="profile.units.length === 0" class="p-8 text-center text-text-muted text-sm">
            {{ isOwnProfile ? 'You are not part of any unit yet.' : 'This member is not part of any unit yet.' }}
          </div>
          
          <div class="divide-y divide-dark-border">
            <router-link 
              v-for="unit in profile.units" 
              :key="unit.id" 
              :to="`/units/${unit.id}`"
              class="p-5 hover:bg-bg/50 transition-colors flex items-center justify-between cursor-pointer block"
            >
              <div class="flex items-center gap-3">
                <div :class="['p-2 rounded-lg border', unitColor(unit.type)]">
                  <component :is="unitIcon(unit.type)" class="w-4 h-4" />
                </div>
                <div>
                  <p class="text-sm font-semibold text-text-strong">{{ unit.name }}</p>
                  <div class="flex items-center gap-2 mt-0.5">
                    <span class="text-xs text-text-muted">{{ unit.type }}</span>
                    <span v-if="unit.departmentType && unit.departmentType !== 'None'" class="w-1 h-1 rounded-full bg-gray-600"></span>
                    <span v-if="unit.departmentType && unit.departmentType !== 'None'" class="text-xs text-text-muted">{{ unit.departmentType }}</span>
                  </div>
                </div>
              </div>
              <div class="px-3 py-1 bg-emerald-500/10 border border-emerald-500/20 rounded-lg flex items-center gap-2 text-sm text-emerald-400">
                <Shield class="w-3.5 h-3.5" />
                {{ unit.roleName }}
              </div>
            </router-link>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>
