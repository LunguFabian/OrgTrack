<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { analyticsService, type UnitActivitySummaryDto, type MemberActivityScoreDto } from '../../api/services/analytics.service';
import { BarChart, Activity, Users, CalendarCheck, CheckSquare, Trophy, AlertCircle, FileText, Table as TableIcon } from 'lucide-vue-next';
import SkeletonLoader from '../common/SkeletonLoader.vue';
import { Bar } from 'vue-chartjs';
import { Chart as ChartJS, Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale } from 'chart.js';

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale);

const props = defineProps<{
  unitId: string;
  members: UnitMemberDto[];
  unitType?: string;
}>();

const isLoading = ref(true);
const error = ref('');
type EnhancedScoreDto = MemberActivityScoreDto & { unitName: string };
const summary = ref<UnitActivitySummaryDto | null>(null);
const memberScores = ref<EnhancedScoreDto[]>([]);
const isDownloading = ref(false);

const fetchAnalytics = async () => {
  isLoading.value = true;
  error.value = '';
  try {
    // 1. Fetch unit summary
    summary.value = await analyticsService.getUnitSummary(props.unitId);

    // 2. Fetch the whole leaderboard from the new endpoint
    const scores = await analyticsService.getLeaderboard(props.unitId);
    
    // The backend already handles the logic for UnitName and Top 5 limiting
    memberScores.value = scores as EnhancedScoreDto[];
  } catch (err: any) {
    error.value = 'Failed to load analytics data. You might not have permission.';
    console.error(err);
  } finally {
    isLoading.value = false;
  }
};

onMounted(fetchAnalytics);
watch(() => props.unitId, fetchAnalytics);

const chartData = computed(() => {
  return {
    labels: ['Tasks Completed', 'Events Organized'],
    datasets: [
      {
        label: 'Unit Productivity',
        backgroundColor: ['#10b981', '#3b82f6'],
        borderRadius: 6,
        data: [summary.value?.tasksDone || 0, summary.value?.eventsHeld || 0]
      }
    ]
  };
});

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: { display: false }
  },
  scales: {
    y: {
      beginAtZero: true,
      grid: { color: '#374151' },
      ticks: { color: '#9ca3af', stepSize: 1 }
    },
    x: {
      grid: { display: false },
      ticks: { color: '#9ca3af' }
    }
  }
};

const downloadReport = async (format: 'pdf' | 'excel') => {
  if (!summary.value) return;
  
  isDownloading.value = true;
  try {
    const blob = await analyticsService.downloadReport(props.unitId, format);
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `OrgTrack_Report_${summary.value.unitName}_${new Date().toISOString().split('T')[0]}.${format === 'excel' ? 'xlsx' : 'pdf'}`;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  } catch (err) {
    console.error('Failed to download report', err);
    // You could show a toast here using useToastStore if it was imported
  } finally {
    isDownloading.value = false;
  }
};
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex flex-col sm:flex-row sm:items-center justify-between gap-4 mb-6">
      <div class="flex items-center gap-3">
        <div class="w-10 h-10 bg-purple-500/10 rounded-xl flex items-center justify-center border border-purple-500/20">
          <BarChart class="w-5 h-5 text-purple-400" />
        </div>
        <div>
          <h2 class="text-xl font-bold text-text-strong">Unit Analytics</h2>
          <p class="text-sm text-text-muted">Activity and performance metrics for this unit</p>
        </div>
      </div>
      
      <div class="flex items-center gap-2" v-if="summary">
        <button 
          @click="downloadReport('pdf')"
          :disabled="isDownloading"
          class="flex items-center gap-2 px-4 py-2 bg-purple-500/10 hover:bg-purple-500/20 text-purple-400 rounded-lg text-sm font-medium transition-colors border border-purple-500/20 disabled:opacity-50"
        >
          <FileText class="w-4 h-4" />
          {{ isDownloading ? '...' : 'Export PDF' }}
        </button>
        <button 
          @click="downloadReport('excel')"
          :disabled="isDownloading"
          class="flex items-center gap-2 px-4 py-2 bg-emerald-500/10 hover:bg-emerald-500/20 text-emerald-400 rounded-lg text-sm font-medium transition-colors border border-emerald-500/20 disabled:opacity-50"
        >
          <TableIcon class="w-4 h-4" />
          {{ isDownloading ? '...' : 'Export Excel' }}
        </button>
      </div>
    </div>

    <!-- Skeleton Loading State -->
    <div v-if="isLoading" class="space-y-6">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div v-for="i in 3" :key="'skel-stat-'+i" class="bg-surface border border-border p-5 rounded-2xl space-y-3">
          <SkeletonLoader width="120px" height="16px" />
          <SkeletonLoader width="40px" height="32px" />
        </div>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-1 bg-surface border border-border rounded-2xl p-5">
          <SkeletonLoader width="140px" height="16px" class="mb-4" />
          <SkeletonLoader width="100%" height="250px" class="rounded-xl" />
        </div>
        <div class="lg:col-span-2 bg-surface border border-border rounded-2xl p-5 space-y-4">
          <SkeletonLoader width="140px" height="16px" class="mb-4" />
          <div v-for="i in 4" :key="'skel-lead-'+i" class="flex items-center gap-4">
            <SkeletonLoader type="circular" width="32px" height="32px" />
            <SkeletonLoader width="200px" height="16px" class="flex-1" />
            <SkeletonLoader width="60px" height="16px" />
          </div>
        </div>
      </div>
    </div>

    <div v-else-if="error" class="flex flex-col items-center justify-center py-12 text-center bg-red-500/5 rounded-2xl border border-red-500/10">
      <AlertCircle class="w-12 h-12 text-red-400 mb-3" />
      <h3 class="text-lg font-semibold text-text-strong">Access Denied</h3>
      <p class="text-text-muted text-sm mt-1 max-w-sm">{{ error }}</p>
    </div>

    <template v-else-if="summary">
      <!-- Quick Stats -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="bg-surface border border-border p-5 rounded-2xl">
          <div class="flex items-center gap-3 mb-2">
            <CheckSquare class="w-4 h-4 text-emerald-400" />
            <h3 class="text-sm font-medium text-text-muted">Tasks Completed</h3>
          </div>
          <p class="text-3xl font-bold text-text-strong">{{ summary.tasksDone }}</p>
        </div>
        
        <div class="bg-surface border border-border p-5 rounded-2xl">
          <div class="flex items-center gap-3 mb-2">
            <CalendarCheck class="w-4 h-4 text-blue-400" />
            <h3 class="text-sm font-medium text-text-muted">Events Held</h3>
          </div>
          <p class="text-3xl font-bold text-text-strong">{{ summary.eventsHeld }}</p>
        </div>

        <div class="bg-surface border border-border p-5 rounded-2xl">
          <div class="flex items-center gap-3 mb-2">
            <Users class="w-4 h-4 text-amber-400" />
            <h3 class="text-sm font-medium text-text-muted">Active Members</h3>
          </div>
          <p class="text-3xl font-bold text-text-strong">{{ summary.membersActive }}</p>
        </div>
      </div>

      <!-- Charts & Leaderboard -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Chart -->
        <div class="lg:col-span-1 bg-surface border border-border rounded-2xl p-5 flex flex-col">
          <h3 class="text-sm font-medium text-text-muted mb-4 flex items-center gap-2">
            <Activity class="w-4 h-4 text-text-muted" />
            Performance Overview
          </h3>
          <div class="flex-1 min-h-[250px]">
            <Bar :data="chartData" :options="chartOptions" />
          </div>
        </div>

        <!-- Leaderboard -->
        <div class="lg:col-span-2 bg-surface border border-border rounded-2xl overflow-hidden flex flex-col">
          <div class="p-5 border-b border-border flex items-center gap-2">
            <Trophy class="w-4 h-4 text-yellow-500" />
            <h3 class="text-sm font-medium text-text-muted">Member Leaderboard</h3>
          </div>
          
          <div class="flex-1 overflow-y-auto max-h-[300px] custom-scrollbar">
            <div v-if="memberScores.length === 0" class="p-6 text-center text-text-muted text-sm">
              No active members in this unit.
            </div>
            <div class="divide-y divide-dark-border">
              <div 
                v-for="(score, index) in memberScores" 
                :key="score.userId"
                class="p-4 hover:bg-bg/50 transition-colors flex items-center justify-between"
              >
                <div class="flex items-center gap-4">
                  <div class="w-8 h-8 rounded-full bg-border flex items-center justify-center text-xs font-bold"
                       :class="index === 0 ? 'bg-gradient-to-r from-yellow-500/20 via-yellow-200/25 to-yellow-600/20 text-yellow-300 border border-yellow-300/40 shadow-[0_0_10px_rgba(253,224,71,0.25)]' :
                               index === 1 ? 'bg-gradient-to-r from-slate-400/20 via-slate-200/25 to-slate-500/20 text-slate-200 border border-slate-300/40 shadow-[0_0_10px_rgba(226,232,240,0.25)]' :
                               index === 2 ? 'bg-gradient-to-r from-amber-700/20 via-orange-300/25 to-amber-800/20 text-amber-400 border border-amber-500/40 shadow-[0_0_10px_rgba(251,146,60,0.22)]' :
                               'text-text-muted'">
                    #{{ index + 1 }}
                  </div>
                  <div>
                    <div class="flex items-center gap-2">
                      <p class="text-sm font-medium text-text-strong">{{ score.userName }}</p>
                      <span class="text-xs font-medium text-blue-400 bg-blue-400/10 px-1.5 py-0.5 rounded border border-blue-400/20 max-w-[80px] truncate" :title="score.roleName">{{ score.roleName }}</span>
                      <span v-if="unitType === 'Committee' || unitType === 'Department'" class="text-xs font-medium text-emerald-400 bg-emerald-400/10 px-1.5 py-0.5 rounded border border-emerald-400/20 max-w-[120px] truncate" :title="score.unitName">{{ score.unitName }}</span>
                    </div>
                    <p class="text-xs text-text-muted mt-1">{{ score.tasksDone }} tasks · {{ score.eventsAttended }} events</p>
                  </div>
                </div>
                <div class="text-right">
                  <span class="text-lg font-bold"
                        :class="index < 3 ? 'text-text-strong' : 'text-text-muted'">
                    {{ score.totalScore }}
                  </span>
                  <span class="text-xs text-text-muted ml-1">pts</span>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>
    </template>
  </div>
</template>

<style scoped>
.custom-scrollbar::-webkit-scrollbar {
  width: 4px;
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
