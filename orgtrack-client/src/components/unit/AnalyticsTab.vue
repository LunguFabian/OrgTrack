<script setup lang="ts">
import { ref, onMounted, computed, watch } from 'vue';
import { analyticsService, type UnitActivitySummaryDto, type MemberActivityScoreDto } from '../../api/services/analytics.service';
import type { UnitMemberDto } from '../../types/unit';
import { BarChart, Activity, Users, CalendarCheck, CheckSquare, Trophy, AlertCircle } from 'lucide-vue-next';
import { Bar } from 'vue-chartjs';
import { Chart as ChartJS, Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale } from 'chart.js';

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale);

const props = defineProps<{
  unitId: string;
  members: UnitMemberDto[];
}>();

const isLoading = ref(true);
const error = ref('');
const summary = ref<UnitActivitySummaryDto | null>(null);
const memberScores = ref<MemberActivityScoreDto[]>([]);

const fetchAnalytics = async () => {
  isLoading.value = true;
  error.value = '';
  try {
    // 1. Fetch unit summary
    summary.value = await analyticsService.getUnitSummary(props.unitId);

    // 2. Fetch scores for all members in parallel
    const scorePromises = props.members.map(m => analyticsService.getMemberScore(m.userId).catch(() => null));
    const results = await Promise.all(scorePromises);
    
    // Filter out nulls (in case of errors for specific users) and sort by score descending
    memberScores.value = results.filter((r): r is MemberActivityScoreDto => r !== null)
                                .sort((a, b) => b.totalScore - a.totalScore);
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
</script>

<template>
  <div class="space-y-6">
    <!-- Header -->
    <div class="flex items-center gap-3 mb-6">
      <div class="w-10 h-10 bg-purple-500/10 rounded-xl flex items-center justify-center border border-purple-500/20">
        <BarChart class="w-5 h-5 text-purple-400" />
      </div>
      <div>
        <h2 class="text-xl font-bold text-white">Unit Analytics</h2>
        <p class="text-sm text-gray-400">Activity and performance metrics for this unit</p>
      </div>
    </div>

    <div v-if="isLoading" class="flex justify-center py-12">
      <div class="w-8 h-8 border-4 border-purple-500 border-t-transparent rounded-full animate-spin"></div>
    </div>

    <div v-else-if="error" class="flex flex-col items-center justify-center py-12 text-center bg-red-500/5 rounded-2xl border border-red-500/10">
      <AlertCircle class="w-12 h-12 text-red-400 mb-3" />
      <h3 class="text-lg font-semibold text-white">Access Denied</h3>
      <p class="text-gray-400 text-sm mt-1 max-w-sm">{{ error }}</p>
    </div>

    <template v-else-if="summary">
      <!-- Quick Stats -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="bg-dark-surface border border-dark-border p-5 rounded-2xl">
          <div class="flex items-center gap-3 mb-2">
            <CheckSquare class="w-4 h-4 text-emerald-400" />
            <h3 class="text-sm font-medium text-gray-400">Tasks Completed</h3>
          </div>
          <p class="text-3xl font-bold text-white">{{ summary.tasksDone }}</p>
        </div>
        
        <div class="bg-dark-surface border border-dark-border p-5 rounded-2xl">
          <div class="flex items-center gap-3 mb-2">
            <CalendarCheck class="w-4 h-4 text-blue-400" />
            <h3 class="text-sm font-medium text-gray-400">Events Held</h3>
          </div>
          <p class="text-3xl font-bold text-white">{{ summary.eventsHeld }}</p>
        </div>

        <div class="bg-dark-surface border border-dark-border p-5 rounded-2xl">
          <div class="flex items-center gap-3 mb-2">
            <Users class="w-4 h-4 text-amber-400" />
            <h3 class="text-sm font-medium text-gray-400">Active Members</h3>
          </div>
          <p class="text-3xl font-bold text-white">{{ summary.membersActive }}</p>
        </div>
      </div>

      <!-- Charts & Leaderboard -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Chart -->
        <div class="lg:col-span-1 bg-dark-surface border border-dark-border rounded-2xl p-5 flex flex-col">
          <h3 class="text-sm font-medium text-gray-300 mb-4 flex items-center gap-2">
            <Activity class="w-4 h-4 text-gray-400" />
            Performance Overview
          </h3>
          <div class="flex-1 min-h-[250px]">
            <Bar :data="chartData" :options="chartOptions" />
          </div>
        </div>

        <!-- Leaderboard -->
        <div class="lg:col-span-2 bg-dark-surface border border-dark-border rounded-2xl overflow-hidden flex flex-col">
          <div class="p-5 border-b border-dark-border flex items-center gap-2">
            <Trophy class="w-4 h-4 text-yellow-500" />
            <h3 class="text-sm font-medium text-gray-300">Member Leaderboard</h3>
          </div>
          
          <div class="flex-1 overflow-y-auto max-h-[300px] custom-scrollbar">
            <div v-if="memberScores.length === 0" class="p-6 text-center text-gray-500 text-sm">
              No active members in this unit.
            </div>
            <div class="divide-y divide-dark-border">
              <div 
                v-for="(score, index) in memberScores" 
                :key="score.userId"
                class="p-4 hover:bg-dark-bg/50 transition-colors flex items-center justify-between"
              >
                <div class="flex items-center gap-4">
                  <div class="w-8 h-8 rounded-full bg-dark-border flex items-center justify-center text-xs font-bold"
                       :class="index === 0 ? 'bg-yellow-500/20 text-yellow-500 border border-yellow-500/30' : 
                               index === 1 ? 'bg-gray-400/20 text-gray-400 border border-gray-400/30' : 
                               index === 2 ? 'bg-amber-700/20 text-amber-600 border border-amber-700/30' : 'text-gray-400'">
                    #{{ index + 1 }}
                  </div>
                  <div>
                    <p class="text-sm font-medium text-white">{{ score.userName }}</p>
                    <p class="text-xs text-gray-500">{{ score.tasksDone }} tasks · {{ score.eventsAttended }} events</p>
                  </div>
                </div>
                <div class="text-right">
                  <span class="text-lg font-bold"
                        :class="index < 3 ? 'text-white' : 'text-gray-400'">
                    {{ score.totalScore }}
                  </span>
                  <span class="text-xs text-gray-500 ml-1">pts</span>
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
