<script setup lang="ts">
import { ref, onMounted, computed } from 'vue';
import { analyticsService, type UnitActivitySummaryDto } from '../api/services/analytics.service';
import { useOrgStore } from '../stores/orgStore';
import { useToastStore } from '../stores/toastStore';
import { 
  BarChart3, 
  Users, 
  CheckCircle2, 
  CalendarDays,
  TrendingUp,
  Activity
} from 'lucide-vue-next';
import SkeletonLoader from '../components/common/SkeletonLoader.vue';
import { Bar, Doughnut } from 'vue-chartjs';
import { 
  Chart as ChartJS, 
  Title, 
  Tooltip, 
  Legend, 
  BarElement, 
  CategoryScale, 
  LinearScale,
  ArcElement
} from 'chart.js';

ChartJS.register(CategoryScale, LinearScale, BarElement, Title, Tooltip, Legend, ArcElement);

const orgStore = useOrgStore();
const toastStore = useToastStore();

const isLoading = ref(true);
const summaries = ref<UnitActivitySummaryDto[]>([]);
const nationalUnit = ref<any>(null);

onMounted(async () => {
  isLoading.value = true;
  try {
    if (orgStore.tree.length === 0) {
      await orgStore.fetchTree();
    }
    
    // Find the National unit (root)
    nationalUnit.value = orgStore.tree.find(u => u.type === 'National');
    
    if (nationalUnit.value) {
      summaries.value = await analyticsService.getNationalDashboard(nationalUnit.value.id);
    } else {
      toastStore.showToast('National unit not found.', 'error');
    }
  } catch (err: any) {
    toastStore.showToast(err.response?.data?.error || 'Failed to load national dashboard', 'error');
  } finally {
    isLoading.value = false;
  }
});

// Aggregate stats
const totalTasks = computed(() => summaries.value.reduce((acc, curr) => acc + curr.tasksDone, 0));
const totalEvents = computed(() => summaries.value.reduce((acc, curr) => acc + curr.eventsHeld, 0));
const totalMembers = computed(() => summaries.value.reduce((acc, curr) => acc + curr.membersActive, 0));

// Chart configuration
const chartOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'bottom' as const,
      labels: {
        color: '#9ca3af' // text-muted
      }
    }
  },
  scales: {
    y: {
      beginAtZero: true,
      grid: {
        color: '#374151' // dark border
      },
      ticks: {
        color: '#9ca3af'
      }
    },
    x: {
      grid: {
        display: false
      },
      ticks: {
        color: '#9ca3af'
      }
    }
  }
};

const doughnutOptions = {
  responsive: true,
  maintainAspectRatio: false,
  plugins: {
    legend: {
      position: 'right' as const,
      labels: {
        color: '#9ca3af'
      }
    }
  }
};

// Chart Data: Tasks & Events Comparison
const performanceChartData = computed(() => {
  return {
    labels: summaries.value.map(s => s.unitName),
    datasets: [
      {
        label: 'Tasks Done',
        backgroundColor: '#34d399', // emerald-400
        data: summaries.value.map(s => s.tasksDone)
      },
      {
        label: 'Events Held',
        backgroundColor: '#60a5fa', // blue-400
        data: summaries.value.map(s => s.eventsHeld)
      }
    ]
  };
});

// Chart Data: Members Distribution
const membersChartData = computed(() => {
  const colors = ['#34d399', '#60a5fa', '#fbbf24', '#a78bfa', '#fb923c', '#f472b6'];
  return {
    labels: summaries.value.map(s => s.unitName),
    datasets: [
      {
        backgroundColor: summaries.value.map((_, i) => colors[i % colors.length]),
        data: summaries.value.map(s => s.membersActive),
        borderWidth: 0
      }
    ]
  };
});

</script>

<template>
  <div class="space-y-8 max-w-7xl mx-auto pb-20 px-4 sm:px-6">
    <!-- Header -->
    <div class="flex items-center gap-4">
      <div class="w-12 h-12 rounded-xl bg-blue-500/10 border border-blue-500/20 flex items-center justify-center">
        <TrendingUp class="w-6 h-6 text-blue-400" />
      </div>
      <div>
        <h1 class="text-2xl font-bold text-text-strong tracking-tight">National Dashboard</h1>
        <p class="text-sm text-text-muted mt-1">High-level overview of committee performance</p>
      </div>
    </div>

    <!-- Skeleton Loading State -->
    <div v-if="isLoading" class="space-y-6">
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div v-for="i in 3" :key="'skel-stat-'+i" class="bg-surface p-6 rounded-2xl border border-border shadow-sm flex items-center gap-5">
          <SkeletonLoader type="circular" width="56px" height="56px" class="rounded-xl" />
          <div class="space-y-2 flex-1">
            <SkeletonLoader width="120px" height="12px" />
            <SkeletonLoader width="60px" height="32px" />
          </div>
        </div>
      </div>
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <div class="lg:col-span-2 bg-surface p-6 rounded-2xl border border-border">
          <SkeletonLoader width="200px" height="20px" class="mb-6" />
          <SkeletonLoader width="100%" height="320px" />
        </div>
        <div class="bg-surface p-6 rounded-2xl border border-border">
          <SkeletonLoader width="180px" height="20px" class="mb-6" />
          <SkeletonLoader type="circular" width="250px" height="250px" class="mx-auto" />
        </div>
      </div>
    </div>
    
    <div v-else-if="!nationalUnit" class="bg-surface p-10 rounded-2xl border border-border text-center">
      <Activity class="w-10 h-10 text-gray-500 mx-auto mb-4" />
      <p class="text-text-strong font-medium text-lg">No National Unit Found</p>
      <p class="text-sm text-text-muted mt-1">Your organization structure does not have a National root unit.</p>
    </div>

    <template v-else>
      <!-- Top Stats -->
      <div class="grid grid-cols-1 md:grid-cols-3 gap-4">
        <div class="bg-surface p-6 rounded-2xl border border-border shadow-sm flex items-center gap-5">
          <div class="p-4 rounded-xl bg-emerald-500/10 text-emerald-400">
            <CheckCircle2 class="w-6 h-6" />
          </div>
          <div>
            <p class="text-sm font-medium text-text-muted uppercase tracking-wider">Total Tasks Done</p>
            <p class="text-3xl font-bold text-text-strong mt-1">{{ totalTasks }}</p>
          </div>
        </div>

        <div class="bg-surface p-6 rounded-2xl border border-border shadow-sm flex items-center gap-5">
          <div class="p-4 rounded-xl bg-blue-500/10 text-blue-400">
            <CalendarDays class="w-6 h-6" />
          </div>
          <div>
            <p class="text-sm font-medium text-text-muted uppercase tracking-wider">Total Events Held</p>
            <p class="text-3xl font-bold text-text-strong mt-1">{{ totalEvents }}</p>
          </div>
        </div>

        <div class="bg-surface p-6 rounded-2xl border border-border shadow-sm flex items-center gap-5">
          <div class="p-4 rounded-xl bg-purple-500/10 text-purple-400">
            <Users class="w-6 h-6" />
          </div>
          <div>
            <p class="text-sm font-medium text-text-muted uppercase tracking-wider">Active Members</p>
            <p class="text-3xl font-bold text-text-strong mt-1">{{ totalMembers }}</p>
          </div>
        </div>
      </div>

      <!-- Charts -->
      <div class="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <!-- Main Bar Chart -->
        <div class="lg:col-span-2 bg-surface p-6 rounded-2xl border border-border">
          <h3 class="text-lg font-semibold text-text-strong mb-6 flex items-center gap-2">
            <BarChart3 class="w-5 h-5 text-blue-400" />
            Committee Performance
          </h3>
          <div class="h-80">
            <Bar
              v-if="summaries.length > 0"
              :data="performanceChartData"
              :options="chartOptions"
            />
            <div v-else class="h-full flex items-center justify-center text-text-muted">
              No data available
            </div>
          </div>
        </div>

        <!-- Donut Chart -->
        <div class="bg-surface p-6 rounded-2xl border border-border">
          <h3 class="text-lg font-semibold text-text-strong mb-6 flex items-center gap-2">
            <Users class="w-5 h-5 text-purple-400" />
            Member Distribution
          </h3>
          <div class="h-80">
            <Doughnut
              v-if="summaries.length > 0"
              :data="membersChartData"
              :options="doughnutOptions"
            />
            <div v-else class="h-full flex items-center justify-center text-text-muted">
              No data available
            </div>
          </div>
        </div>
      </div>
      
      <!-- Data Table -->
      <div class="bg-surface rounded-2xl border border-border overflow-hidden">
        <div class="px-6 py-4 border-b border-border">
          <h3 class="text-lg font-semibold text-text-strong">Detailed Breakdown</h3>
        </div>
        <div class="overflow-x-auto">
          <table class="w-full text-left text-sm">
            <thead class="bg-bg text-text-muted font-medium uppercase tracking-wider text-xs border-b border-border">
              <tr>
                <th class="px-6 py-3">Committee</th>
                <th class="px-6 py-3">Active Members</th>
                <th class="px-6 py-3">Tasks Done</th>
                <th class="px-6 py-3">Events Held</th>
                <th class="px-6 py-3">Activity Score</th>
              </tr>
            </thead>
            <tbody class="divide-y divide-border text-text-strong">
              <tr v-for="unit in summaries" :key="unit.unitId" class="hover:bg-surface-hover transition-colors">
                <td class="px-6 py-4 font-medium">{{ unit.unitName }}</td>
                <td class="px-6 py-4">{{ unit.membersActive }}</td>
                <td class="px-6 py-4 text-emerald-400 font-medium">{{ unit.tasksDone }}</td>
                <td class="px-6 py-4 text-blue-400 font-medium">{{ unit.eventsHeld }}</td>
                <td class="px-6 py-4 text-purple-400 font-medium">
                  {{ (unit.tasksDone * 10) + (unit.eventsHeld * 20) + unit.membersActive }}
                </td>
              </tr>
              <tr v-if="summaries.length === 0">
                <td colspan="5" class="px-6 py-8 text-center text-text-muted">
                  No committees found to analyze.
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </div>
    </template>
  </div>
</template>
