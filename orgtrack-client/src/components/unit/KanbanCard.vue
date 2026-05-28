<script setup lang="ts">
import { computed } from 'vue';
import { Clock, AlertCircle, Trash2, Pencil, ListChecks } from 'lucide-vue-next';
import type { TaskDto } from '../../types/unit';

const props = defineProps<{
  task: TaskDto;
  subTasks?: TaskDto[];
  confirmingDelete?: boolean;
}>();

defineEmits(['request-delete', 'confirm-delete', 'edit', 'cancel-delete']);

const priorityColors: Record<string, string> = {
  Low: 'bg-gray-500/10 text-text-muted border-gray-500/20',
  Medium: 'bg-blue-500/10 text-blue-400 border-blue-500/20',
  High: 'bg-orange-500/10 text-orange-400 border-orange-500/20',
  Critical: 'bg-red-500/10 text-red-400 border-red-500/20'
};

const formatDate = (iso?: string) => {
  if (!iso) return null;
  return new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short' });
};

const isOverdue = computed(() => {
  if (!props.task.deadline || props.task.status === 'Done') return false;
  return new Date(props.task.deadline) < new Date();
});

const subTasksProgress = computed(() => {
  if (!props.subTasks || props.subTasks.length === 0) return null;
  const doneCount = props.subTasks.filter(t => t.status === 'Done').length;
  return { done: doneCount, total: props.subTasks.length };
});
</script>

<template>
  <div
    class="bg-bg border border-border rounded-xl p-4 shadow-sm hover:border-emerald-500/30 transition-colors cursor-grab active:cursor-grabbing group"
    draggable="true"
  >
    <!-- Priority Badge & Actions -->
    <div class="flex items-start justify-between mb-2">
      <span :class="['px-2 py-0.5 rounded text-[10px] font-bold uppercase tracking-wider border', priorityColors[task.priority]]">
        {{ task.priority }}
      </span>
      
      <div class="flex items-center gap-1">
        <!-- Inline delete confirmation -->
        <template v-if="confirmingDelete">
          <button @click.stop="$emit('confirm-delete', task)" class="px-2 py-1 text-[10px] font-bold uppercase bg-red-500/20 text-red-400 border border-red-500/30 rounded-md hover:bg-red-500/30 transition-colors">
            Delete
          </button>
          <button @click.stop="$emit('cancel-delete')" class="px-2 py-1 text-[10px] font-bold uppercase text-text-muted border border-border rounded-md hover:bg-surface-hover transition-colors">
            Cancel
          </button>
        </template>
        <!-- Normal actions (shown on hover) -->
        <template v-else>
          <div class="flex items-center gap-2 opacity-0 group-hover:opacity-100 transition-all">
            <button @click.stop="$emit('edit', task)" class="p-1 text-text-muted hover:text-emerald-400 transition-colors rounded-lg hover:bg-emerald-500/10" title="Edit Task">
              <Pencil class="w-3.5 h-3.5" />
            </button>
            <button @click.stop="$emit('request-delete', task)" class="p-1 text-text-muted hover:text-red-400 transition-colors rounded-lg hover:bg-red-500/10" title="Delete Task">
              <Trash2 class="w-3.5 h-3.5" />
            </button>
          </div>
        </template>
      </div>
    </div>

    <!-- Title -->
    <h4 class="text-text-strong font-medium text-sm leading-snug mb-1.5">{{ task.title }}</h4>
    
    <!-- Description Preview -->
    <p v-if="task.description" class="text-xs text-text-muted line-clamp-2 mb-4 leading-relaxed">
      {{ task.description }}
    </p>

    <div v-if="subTasksProgress" class="flex items-center gap-1.5 mb-4 text-xs font-medium" :class="subTasksProgress.done === subTasksProgress.total ? 'text-emerald-400' : 'text-text-muted'">
      <ListChecks class="w-3.5 h-3.5" />
      <span>{{ subTasksProgress.done }}/{{ subTasksProgress.total }} subtasks</span>
    </div>

    <!-- Footer: Deadline & Assignee -->
    <div class="flex items-center justify-between mt-3 pt-3 border-t border-border/50">
      <div class="flex items-center gap-1.5">
        <Clock :class="['w-3.5 h-3.5', isOverdue ? 'text-red-400' : 'text-text-muted']" />
        <span :class="['text-xs font-medium', isOverdue ? 'text-red-400' : 'text-text-muted']">
          {{ formatDate(task.deadline) || 'No date' }}
        </span>
        <AlertCircle v-if="isOverdue" class="w-3.5 h-3.5 text-red-400 ml-0.5" title="Overdue!" />
      </div>

      <!-- Assignee Avatar -->
      <div v-if="task.assigneeName" class="relative group/avatar">
        <div class="w-6 h-6 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center text-[10px] font-bold ring-2 ring-bg" title="Assigned to">
          {{ task.assigneeName.substring(0, 2).toUpperCase() }}
        </div>
        <!-- Tooltip -->
        <div class="absolute bottom-full right-0 mb-2 whitespace-nowrap opacity-0 group-hover/avatar:opacity-100 transition-opacity pointer-events-none z-10">
          <div class="bg-gray-800 text-xs text-text-strong px-2 py-1 rounded shadow-xl">
            {{ task.assigneeName }}
          </div>
          <div class="w-2 h-2 bg-gray-800 rotate-45 absolute -bottom-1 right-2"></div>
        </div>
      </div>
      <div v-else class="w-6 h-6 rounded-full border border-dashed border-gray-600 flex items-center justify-center" title="Unassigned">
        <span class="text-[10px] text-gray-600">?</span>
      </div>
    </div>
  </div>
</template>
