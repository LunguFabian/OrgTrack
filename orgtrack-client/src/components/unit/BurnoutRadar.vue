<script setup lang="ts">
import { ref, onMounted, watch } from 'vue';
import { analyticsService, type BurnoutRiskDto } from '../../api/services/analytics.service';
import { 
  HeartPulse, 
  AlertTriangle, 
  AlertCircle, 
  Info,
  TrendingUp,
  Activity,
  Flame
} from 'lucide-vue-next';

const props = defineProps<{
  unitId: string;
}>();

const risks = ref<BurnoutRiskDto[]>([]);
const loading = ref(true);
const error = ref('');

const fetchRisks = async () => {
  loading.value = true;
  error.value = '';
  try {
    risks.value = await analyticsService.getBurnoutRisks(props.unitId);
  } catch (err: any) {
    if (err.response?.status === 403) {
      error.value = 'You do not have leadership permissions to view burnout analytics for this unit.';
    } else {
      error.value = 'Failed to load burnout analysis.';
    }
  } finally {
    loading.value = false;
  }
};

onMounted(() => {
  fetchRisks();
});

watch(() => props.unitId, () => {
  fetchRisks();
});

const getRiskColor = (level: string) => {
  if (level === 'Critical') return 'text-red-500 bg-red-500/10 border-red-500/20';
  if (level === 'High') return 'text-orange-500 bg-orange-500/10 border-orange-500/20';
  if (level === 'Elevated') return 'text-yellow-500 bg-yellow-500/10 border-yellow-500/20';
  return 'text-green-500 bg-green-500/10 border-green-500/20';
};

const getRiskIcon = (level: string) => {
  if (level === 'Critical') return Flame;
  if (level === 'High') return AlertTriangle;
  return AlertCircle;
};
</script>

<template>
  <div class="space-y-6">
    <div class="flex items-center justify-between">
      <div>
        <h2 class="text-2xl font-bold text-white flex items-center gap-3">
          <HeartPulse class="w-7 h-7 text-rose-500" />
          Burnout Radar
        </h2>
        <p class="text-zinc-400 mt-1 text-sm">
          Predictive behavioral analysis for members in this unit.
        </p>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="loading" class="flex justify-center items-center py-20">
      <div class="animate-spin rounded-full h-10 w-10 border-t-2 border-b-2 border-rose-500"></div>
    </div>

    <!-- Error State -->
    <div v-else-if="error" class="p-4 bg-red-500/10 border border-red-500/20 rounded-xl text-red-500 flex items-center gap-2">
      <AlertTriangle class="w-5 h-5" />
      {{ error }}
    </div>

    <!-- Empty State (No risks!) -->
    <div v-else-if="risks.length === 0" class="p-10 text-center bg-zinc-900/50 border border-zinc-800 rounded-xl">
      <div class="inline-flex items-center justify-center w-14 h-14 rounded-full bg-emerald-500/10 mb-4">
        <Activity class="w-7 h-7 text-emerald-500" />
      </div>
      <h3 class="text-lg font-semibold text-white mb-2">This unit is healthy!</h3>
      <p class="text-zinc-400 text-sm">
        The predictive algorithm did not detect any elevated burnout risks here.
      </p>
    </div>

    <!-- Risks Grid -->
    <div v-else class="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
      <div 
        v-for="risk in risks" 
        :key="risk.userId"
        class="bg-zinc-900 border border-zinc-800 rounded-xl p-5 hover:border-zinc-700 transition-all flex flex-col"
      >
        <div class="flex items-start justify-between mb-4">
          <div class="flex items-center gap-3">
            <div class="w-12 h-12 rounded-full overflow-hidden bg-zinc-800 flex-shrink-0">
              <img v-if="risk.pictureUrl" :src="risk.pictureUrl" class="w-full h-full object-cover" />
              <div v-else class="w-full h-full flex items-center justify-center text-zinc-400 font-medium">
                {{ risk.userName.charAt(0) }}
              </div>
            </div>
            <div>
              <h3 class="font-medium text-white">{{ risk.userName }}</h3>
              <div 
                class="inline-flex items-center gap-1.5 px-2 py-0.5 rounded text-xs font-medium border mt-1"
                :class="getRiskColor(risk.riskLevel)"
              >
                <component :is="getRiskIcon(risk.riskLevel)" class="w-3.5 h-3.5" />
                {{ risk.riskLevel }} ({{ risk.burnoutScorePercentage }}%)
              </div>
            </div>
          </div>
        </div>

        <div class="flex-1">
          <h4 class="text-xs uppercase tracking-wider text-zinc-500 font-semibold mb-3 flex items-center gap-1.5">
            <TrendingUp class="w-3.5 h-3.5" />
            Detected Stress Factors
          </h4>
          <ul class="space-y-2">
            <li 
              v-for="(flag, index) in risk.warningFlags" 
              :key="index"
              class="text-xs text-zinc-300 flex items-start gap-2 bg-zinc-800/50 p-2.5 rounded-lg border border-zinc-800/50"
            >
              <Info class="w-4 h-4 text-rose-400 flex-shrink-0" />
              <span class="leading-relaxed">{{ flag }}</span>
            </li>
          </ul>
        </div>
        
        <div class="mt-5 pt-4 border-t border-zinc-800">
          <RouterLink 
            :to="`/profile/${risk.userId}`"
            class="text-xs text-indigo-400 hover:text-indigo-300 font-medium transition-colors"
          >
            View Profile &rarr;
          </RouterLink>
        </div>
      </div>
    </div>
  </div>
</template>
