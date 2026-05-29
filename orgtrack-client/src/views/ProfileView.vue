<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAuthStore } from '../stores/authStore';
import { organizationService } from '../api/services/organization.service';
import { analyticsService, type MemberActivityScoreDto } from '../api/services/analytics.service';
import type { OrganizationUnitDto } from '../types/organization';
import { Activity, CheckSquare, CalendarCheck, Shield, Building2, Mail, Loader2, ArrowLeft, Calendar } from 'lucide-vue-next';
import { useRouter } from 'vue-router';
import { api } from '../api/axios';
import { useToastStore } from '../stores/toastStore';

const authStore = useAuthStore();
const router = useRouter();
const toastStore = useToastStore();

const isLoading = ref(true);
const isConnectingCalendar = ref(false);
const score = ref<MemberActivityScoreDto | null>(null);
const myUnits = ref<OrganizationUnitDto[]>([]);

onMounted(async () => {
  if (!authStore.user) return;
  isLoading.value = true;
  try {
    const [scoreData, unitsData] = await Promise.all([
      analyticsService.getMemberScore(authStore.user.id),
      organizationService.getMyUnits()
    ]);
    score.value = scoreData;
    myUnits.value = unitsData;
  } catch (err) {
    console.error('Failed to load profile data:', err);
  } finally {
    isLoading.value = false;
  }
});

const connectGoogleCalendar = async () => {
  const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID;
  if (!GOOGLE_CLIENT_ID) {
    toastStore.showToast('Google Client ID is not configured.', 'error');
    return;
  }

  isConnectingCalendar.value = true;

  const initClient = () => {
    const client = (window as any).google.accounts.oauth2.initCodeClient({
      client_id: GOOGLE_CLIENT_ID,
      scope: 'https://www.googleapis.com/auth/calendar.events',
      ux_mode: 'popup',
      callback: async (response: any) => {
        if (response.code) {
          try {
            await api.post('/auth/google-calendar', {
              authorizationCode: response.code,
              redirectUri: window.location.origin
            });
            
            if (authStore.user) {
              authStore.user.isGoogleCalendarConnected = true;
              localStorage.setItem('user', JSON.stringify(authStore.user));
            }
            
            toastStore.showToast('Google Calendar connected successfully!', 'success');
          } catch (err) {
            toastStore.showToast('Failed to connect Google Calendar.', 'error');
          } finally {
            isConnectingCalendar.value = false;
          }
        } else {
          isConnectingCalendar.value = false;
        }
      },
      error_callback: () => {
        toastStore.showToast('Google Authorization failed.', 'error');
        isConnectingCalendar.value = false;
      }
    });

    client.requestCode();
  };

  if (!(window as any).google) {
    const script = document.createElement('script');
    script.src = 'https://accounts.google.com/gsi/client';
    script.async = true;
    script.defer = true;
    script.onload = initClient;
    script.onerror = () => {
      toastStore.showToast('Failed to load Google script.', 'error');
      isConnectingCalendar.value = false;
    };
    document.head.appendChild(script);
  } else {
    initClient();
  }
};
</script>

<template>
  <div class="max-w-5xl mx-auto space-y-8 pb-20">
    <!-- Header -->
    <div class="flex items-center gap-4 mb-8">
      <button @click="router.back()" class="p-2 rounded-xl border border-border bg-surface hover:bg-bg text-text-muted hover:text-text-strong transition-colors">
        <ArrowLeft class="w-5 h-5" />
      </button>
      <div>
        <h1 class="text-3xl font-bold text-text-strong tracking-tight">My Profile</h1>
        <p class="text-sm text-text-muted">View your activity score and organizational roles</p>
      </div>
    </div>

    <div v-if="isLoading" class="flex justify-center py-20">
      <div class="flex flex-col items-center gap-4">
        <Loader2 class="w-10 h-10 text-emerald-500 animate-spin" />
        <p class="text-text-muted text-sm">Loading profile data...</p>
      </div>
    </div>

    <div v-else class="grid grid-cols-1 lg:grid-cols-3 gap-8">
      
      <!-- Left Column: User Card -->
      <div class="lg:col-span-1 space-y-6">
        <div class="bg-surface border border-border rounded-2xl p-6 text-center shadow-lg">
          <img 
            v-if="authStore.user?.pictureUrl" 
            :src="authStore.user.pictureUrl" 
            alt="Profile Avatar" 
            class="w-24 h-24 mx-auto rounded-full object-cover shadow-xl mb-4 border-4 border-bg"
          />
          <div v-else class="w-24 h-24 mx-auto bg-gradient-to-tr from-emerald-500 to-blue-500 rounded-full flex items-center justify-center text-3xl font-bold text-white shadow-xl mb-4 border-4 border-bg">
            {{ authStore.user?.firstName?.[0] }}{{ authStore.user?.lastName?.[0] }}
          </div>
          <h2 class="text-xl font-bold text-text-strong">{{ authStore.user?.firstName }} {{ authStore.user?.lastName }}</h2>
          <div class="flex items-center justify-center gap-2 mt-2 text-text-muted text-sm">
            <Mail class="w-4 h-4" />
            {{ authStore.user?.email }}
          </div>
          
          <div class="mt-6 pt-6 border-t border-border text-left">
            <h3 class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Total Activity Score</h3>
            <div class="flex items-center gap-4">
              <div class="p-3 bg-yellow-500/10 border border-yellow-500/20 rounded-xl">
                <Activity class="w-6 h-6 text-yellow-500" />
              </div>
              <div>
                <span class="text-3xl font-bold text-text-strong">{{ score?.totalScore || 0 }}</span>
                <span class="text-sm text-text-muted ml-1">pts</span>
              </div>
            </div>
          </div>

          <div class="mt-6 pt-6 border-t border-border text-left">
            <h3 class="text-xs font-semibold text-text-muted uppercase tracking-wider mb-4">Integrations</h3>
            
            <div v-if="authStore.user?.isGoogleCalendarConnected" class="p-4 bg-emerald-500/10 border border-emerald-500/20 rounded-xl flex items-center justify-between">
              <div class="flex items-center gap-3">
                <Calendar class="w-5 h-5 text-emerald-400" />
                <span class="text-sm font-medium text-emerald-400">Google Calendar</span>
              </div>
              <span class="text-xs px-2 py-1 bg-emerald-500/20 text-emerald-300 rounded-md">Connected ✓</span>
            </div>
            
            <button 
              v-else 
              @click="connectGoogleCalendar" 
              :disabled="isConnectingCalendar"
              class="w-full flex items-center justify-center gap-2 px-4 py-2.5 bg-blue-600 hover:bg-blue-700 text-white text-sm font-medium rounded-xl transition-colors disabled:opacity-50"
            >
              <Loader2 v-if="isConnectingCalendar" class="w-4 h-4 animate-spin" />
              <Calendar v-else class="w-4 h-4" />
              {{ isConnectingCalendar ? 'Connecting...' : 'Connect Google Calendar' }}
            </button>
            <p class="text-xs text-text-muted mt-2 text-center">
              Automatically sync your OrgTrack events to Google Calendar.
            </p>
          </div>
        </div>
      </div>

      <!-- Right Column: Stats & Roles -->
      <div class="lg:col-span-2 space-y-6">
        
        <!-- Detailed Stats -->
        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div class="bg-surface border border-border p-5 rounded-2xl flex items-start gap-4">
            <div class="p-3 bg-emerald-500/10 border border-emerald-500/20 rounded-xl">
              <CheckSquare class="w-5 h-5 text-emerald-400" />
            </div>
            <div>
              <p class="text-sm text-text-muted">Tasks Completed</p>
              <p class="text-2xl font-bold text-text-strong">{{ score?.tasksDone || 0 }}</p>
              <p class="text-xs text-text-muted mt-1">x3 points per task</p>
            </div>
          </div>

          <div class="bg-surface border border-border p-5 rounded-2xl flex items-start gap-4">
            <div class="p-3 bg-blue-500/10 border border-blue-500/20 rounded-xl">
              <CalendarCheck class="w-5 h-5 text-blue-400" />
            </div>
            <div>
              <p class="text-sm text-text-muted">Events Attended</p>
              <p class="text-2xl font-bold text-text-strong">{{ score?.eventsAttended || 0 }}</p>
              <p class="text-xs text-text-muted mt-1">x1 point per event</p>
            </div>
          </div>
        </div>

        <!-- My Units -->
        <div class="bg-surface border border-border rounded-2xl overflow-hidden">
          <div class="p-5 border-b border-border flex items-center gap-2">
            <Building2 class="w-5 h-5 text-text-muted" />
            <h3 class="text-sm font-semibold text-text-strong">My Roles & Teams</h3>
          </div>
          
          <div v-if="myUnits.length === 0" class="p-8 text-center text-text-muted text-sm">
            You are not part of any unit yet. Ask your leader for an invite link.
          </div>
          
          <div class="divide-y divide-dark-border">
            <div v-for="unit in myUnits" :key="unit.id" class="p-5 hover:bg-bg/50 transition-colors flex items-center justify-between">
              <div>
                <p class="text-sm font-semibold text-text-strong">{{ unit.name }}</p>
                <div class="flex items-center gap-2 mt-1">
                  <span class="text-xs text-text-muted">{{ unit.type }}</span>
                  <span v-if="unit.departmentType && unit.departmentType !== 'None'" class="w-1 h-1 rounded-full bg-gray-600"></span>
                  <span v-if="unit.departmentType && unit.departmentType !== 'None'" class="text-xs text-text-muted">{{ unit.departmentType }}</span>
                </div>
              </div>
              <div class="px-3 py-1 bg-emerald-500/10 border border-emerald-500/20 rounded-lg flex items-center gap-2 text-sm text-emerald-400">
                <Shield class="w-3.5 h-3.5" />
                {{ unit.members?.find(m => m.userId === authStore.user?.id)?.roleName || 'Member' }}
              </div>
            </div>
          </div>
        </div>

      </div>
    </div>
  </div>
</template>
