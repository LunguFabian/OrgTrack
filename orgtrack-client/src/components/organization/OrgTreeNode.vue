<script setup lang="ts">
import { ref, computed } from 'vue';
import type { OrganizationUnitDto } from '../../types/organization';
import { ChevronRight, ChevronDown, Plus, Users, Globe, Building2, Layers, Shield, ArrowRight } from 'lucide-vue-next';

const props = defineProps<{
  node: OrganizationUnitDto;
  level: number;
}>();

const emit = defineEmits<{
  (e: 'add-child', payload: { id: string; type: string }): void
}>();

import { useRouter } from 'vue-router';
const router = useRouter();

const isExpanded = ref(props.level < 2);

const toggleExpand = () => {
  if (props.node.children && props.node.children.length > 0) {
    isExpanded.value = !isExpanded.value;
  }
};

const handleAddChild = (e: Event) => {
  e.stopPropagation();
  emit('add-child', { id: props.node.id, type: props.node.type });
};

const goToDashboard = (e: Event) => {
  e.stopPropagation();
  router.push(`/units/${props.node.id}`);
};
const nodeStyling = computed(() => {
  switch (props.node.type) {
    case 'National': return { icon: Globe, color: 'text-blue-400', bg: 'bg-blue-500/10', border: 'border-blue-500/20' };
    case 'Committee': return { icon: Building2, color: 'text-emerald-400', bg: 'bg-emerald-500/10', border: 'border-emerald-500/20' };
    case 'Department': return { icon: Layers, color: 'text-purple-400', bg: 'bg-purple-500/10', border: 'border-purple-500/20' };
    case 'Team': return { icon: Shield, color: 'text-orange-400', bg: 'bg-orange-500/10', border: 'border-orange-500/20' };
    default: return { icon: Users, color: 'text-gray-400', bg: 'bg-gray-500/10', border: 'border-gray-500/20' };
  }
});
</script>

<template>
  <div class="relative">
    <!-- Vertical connecting line (using absolute positioning) -->
    <div v-if="level > 0" class="absolute left-[-1.5rem] top-6 bottom-[-0.5rem] w-px bg-dark-border z-0"></div>
    <div v-if="level > 0" class="absolute left-[-1.5rem] top-6 w-6 h-px bg-dark-border z-0"></div>

    <!-- Node Card -->
    <div 
      @click="toggleExpand"
      class="relative z-10 flex items-center justify-between p-3 mb-2 rounded-xl border bg-dark-surface shadow-sm cursor-pointer transition-all hover:border-gray-600 group"
      :class="nodeStyling.border"
      :style="{ marginLeft: level === 0 ? '0' : '0.5rem' }"
    >
      <div class="flex items-center gap-3">
        <!-- Expand Icon -->
        <div class="w-5 flex justify-center text-gray-500">
          <ChevronDown v-if="node.children.length && isExpanded" class="w-4 h-4" />
          <ChevronRight v-else-if="node.children.length && !isExpanded" class="w-4 h-4" />
        </div>

        <!-- Node Type Icon -->
        <div :class="['p-2 rounded-lg', nodeStyling.bg, nodeStyling.color]">
          <component :is="nodeStyling.icon" class="w-4 h-4" />
        </div>

        <!-- Info -->
        <div>
          <h3 class="font-medium text-gray-200">{{ node.name }}</h3>
          <p class="text-xs text-gray-500 flex items-center gap-1">
            {{ node.type }}
            <span v-if="node.childrenCount > 0" class="px-1.5 py-0.5 rounded bg-dark-border text-[10px]">
              {{ node.childrenCount }} sub-units
            </span>
          </p>
        </div>
      </div>

      <!-- Action Buttons (Visible on hover) -->
      <div class="opacity-0 group-hover:opacity-100 transition-opacity pr-2 flex items-center gap-1">
        <button 
          @click="goToDashboard"
          class="p-1.5 rounded-md hover:bg-gray-500/20 text-gray-400 hover:text-white transition-colors"
          title="Open Dashboard"
        >
          <ArrowRight class="w-4 h-4" />
        </button>
        <button 
          v-if="node.type !== 'Team'"
          @click="handleAddChild"
          class="p-1.5 rounded-md hover:bg-emerald-500/20 text-emerald-400 transition-colors"
          title="Add Sub-unit"
        >
          <Plus class="w-4 h-4" />
        </button>
      </div>
    </div>

    <!-- Children (Recursive rendering) -->
    <div v-show="isExpanded" v-if="node.children && node.children.length > 0" class="pl-6 relative">
      <OrgTreeNode 
        v-for="child in node.children" 
        :key="child.id" 
        :node="child" 
        :level="level + 1"
        @add-child="(payload) => emit('add-child', payload)"
      />
    </div>
  </div>
</template>
