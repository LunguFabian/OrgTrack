<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { Plus, ListTodo, Loader2, ClipboardCheck, CheckCircle2 } from 'lucide-vue-next';
import { tasksService } from '../../api/services/tasks.service';
import { organizationService } from '../../api/services/organization.service';
import { useToastStore } from '../../stores/toastStore';
import { useAuthStore } from '../../stores/authStore';
import type { TaskDto, UnitMemberDto } from '../../types/unit';
import type { OrganizationUnitDto } from '../../types/organization';
import KanbanCard from './KanbanCard.vue';

const toastStore = useToastStore();
const authStore = useAuthStore();

const props = withDefaults(defineProps<{
  unitId?: string;
  members?: UnitMemberDto[];
  mode?: 'unit' | 'me';
}>(), {
  mode: 'unit',
  members: () => []
});

const tasks = ref<TaskDto[]>([]);
const myUnits = ref<OrganizationUnitDto[]>([]);
const isLoading = ref(true);
const error = ref('');
const dragOverColumn = ref<string | null>(null);

const columns = [
  { id: 'ToDo', name: 'To Do', icon: ListTodo, color: 'text-gray-400', border: 'border-gray-500/20', bg: 'bg-gray-500/5' },
  { id: 'InProgress', name: 'In Progress', icon: Loader2, color: 'text-blue-400', border: 'border-blue-500/20', bg: 'bg-blue-500/5' },
  { id: 'WaitingForApproval', name: 'Review', icon: ClipboardCheck, color: 'text-orange-400', border: 'border-orange-500/20', bg: 'bg-orange-500/5' },
  { id: 'Done', name: 'Done', icon: CheckCircle2, color: 'text-emerald-400', border: 'border-emerald-500/20', bg: 'bg-emerald-500/5' }
];

const fetchTasks = async () => {
  try {
    isLoading.value = true;
    if (props.mode === 'me') {
      const [tasksData, unitsData] = await Promise.all([
        tasksService.getMyTasks(),
        organizationService.getMyUnits()
      ]);
      tasks.value = tasksData;
      myUnits.value = unitsData;
    } else if (props.unitId) {
      tasks.value = await tasksService.getTasksByUnit(props.unitId);
    }
  } catch (err: any) {
    error.value = err.response?.data?.error || 'Failed to load tasks.';
  } finally {
    isLoading.value = false;
  }
};

onMounted(fetchTasks);

const tasksByColumn = computed(() => {
  const grouped: Record<string, TaskDto[]> = {
    ToDo: [],
    InProgress: [],
    WaitingForApproval: [],
    Done: []
  };
  
  tasks.value.forEach(task => {
    if (grouped[task.status]) {
      grouped[task.status].push(task);
    }
  });
  
  return grouped;
});
const onDragStart = (e: DragEvent, task: TaskDto, fromColumn: string) => {
  if (task.status === 'Done') {
    e.preventDefault();
    return;
  }
  if (e.dataTransfer) {
    e.dataTransfer.setData('taskId', task.id);
    e.dataTransfer.setData('fromColumn', fromColumn);
    e.dataTransfer.effectAllowed = 'move';
  }
};

const onDragEnter = (columnId: string) => {
  dragOverColumn.value = columnId;
};

const onDragLeave = (columnId: string) => {
  if (dragOverColumn.value === columnId) {
    dragOverColumn.value = null;
  }
};

const onDrop = async (event: DragEvent, newStatus: string) => {
  dragOverColumn.value = null;
  const taskId = event.dataTransfer?.getData('taskId');
  
  if (!taskId) return;
  
  const taskIndex = tasks.value.findIndex(t => t.id === taskId);
  if (taskIndex === -1) return;
  
  const task = tasks.value[taskIndex];
  if (task.status === newStatus) return; // Dropped in same column
  const oldStatus = task.status;
  task.status = newStatus as any;
  
  try {
    await tasksService.updateTaskStatus(task.organizationUnitId, taskId, newStatus);
  } catch (err: any) {
    console.error('Failed to update task status', err);
    task.status = oldStatus as any;
    toastStore.showToast(err.response?.data?.error || 'Failed to update task status.', 'error');
  }
};

const handleDeleteTask = async (task: TaskDto) => {
  if (!confirm(`Are you sure you want to delete the task "${task.title}"?`)) return;

  try {
    await tasksService.deleteTask(task.organizationUnitId, task.id);
    tasks.value = tasks.value.filter(t => t.id !== task.id);
    toastStore.showToast('Task deleted successfully.', 'success');
  } catch (err: any) {
    console.error('Failed to delete task', err);
    toastStore.showToast(err.response?.data?.error || 'Failed to delete task.', 'error');
  }
};
const isCreateModalOpen = ref(false);
const isCreating = ref(false);
const createError = ref('');
const newTaskForm = ref({
  title: '',
  description: '',
  priority: 'Medium',
  assigneeId: '' as string | null,
  deadline: '' as string | null,
  targetUnitId: ''
});

const minDateTime = ref('');

const openCreateModal = () => {
  createError.value = '';
  const now = new Date();
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
  minDateTime.value = now.toISOString().slice(0, 16);

  newTaskForm.value = {
    title: '',
    description: '',
    priority: 'Medium',
    assigneeId: props.mode === 'me' ? (authStore.user?.id || '') : '',
    deadline: '',
    targetUnitId: props.mode === 'unit' ? props.unitId! : ''
  };
  isCreateModalOpen.value = true;
};

const submitCreateTask = async () => {
  if (!newTaskForm.value.title || !newTaskForm.value.description) {
    createError.value = 'Title and description are required.';
    return;
  }

  if (props.mode === 'me' && !newTaskForm.value.targetUnitId) {
    createError.value = 'Please select a unit for this task.';
    return;
  }
  
  if (newTaskForm.value.deadline) {
    const selectedDate = new Date(newTaskForm.value.deadline);
    if (selectedDate < new Date()) {
      createError.value = 'The deadline cannot be set in the past.';
      return;
    }
  }
  
  createError.value = '';
  isCreating.value = true;
  
  try {
    const payload = {
      title: newTaskForm.value.title,
      description: newTaskForm.value.description,
      priority: newTaskForm.value.priority,
      assigneeId: newTaskForm.value.assigneeId || null,
      deadline: newTaskForm.value.deadline ? new Date(newTaskForm.value.deadline).toISOString() : null
    };
    
    const unitToCreateIn = props.mode === 'me' ? newTaskForm.value.targetUnitId : props.unitId!;
    const newTask = await tasksService.createTask(unitToCreateIn, payload);
    tasks.value.push(newTask);
    isCreateModalOpen.value = false;
    toastStore.showToast('Task created successfully.', 'success');
  } catch (err: any) {
    createError.value = err.response?.data?.error || 'Failed to create task.';
    console.error(err);
  } finally {
    isCreating.value = false;
  }
};

</script>

<template>
  <div class="h-[750px] flex flex-col">
    <!-- Header Controls -->
    <div class="flex items-center justify-between mb-6 flex-shrink-0">
      <div class="flex items-center gap-2">
        <h3 class="text-white font-bold text-lg">Task Board</h3>
        <span class="px-2 py-0.5 rounded-full bg-dark-bg border border-dark-border text-xs text-gray-400 font-medium">
          {{ tasks.length }} tasks
        </span>
      </div>
      
      <button @click="openCreateModal" class="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-medium rounded-lg transition-all shadow-lg shadow-emerald-500/20">
        <Plus class="w-4 h-4" />
        New Task
      </button>
    </div>

    <!-- Error State -->
    <div v-if="error" class="mb-4 p-4 bg-red-500/10 border border-red-500/20 rounded-xl text-red-400 text-sm">
      {{ error }}
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="flex-1 flex items-center justify-center">
      <div class="flex flex-col items-center gap-3">
        <div class="w-8 h-8 border-4 border-emerald-500 border-t-transparent rounded-full animate-spin"></div>
        <span class="text-gray-400 text-sm font-medium">Loading board...</span>
      </div>
    </div>

    <!-- Kanban Columns -->
    <div v-else class="flex gap-6 h-full overflow-x-auto pb-4 custom-scrollbar">
      <div
        v-for="column in columns"
        :key="column.id"
        class="flex-shrink-0 w-[300px] flex flex-col rounded-2xl border transition-colors duration-200"
        :class="[
          dragOverColumn === column.id ? `border-${column.color.split('-')[1]}-500/50 ${column.bg}` : 'border-dark-border bg-dark-surface/50'
        ]"
        @dragover.prevent
        @dragenter.prevent="onDragEnter(column.id)"
        @dragleave.prevent="onDragLeave(column.id)"
        @drop="onDrop($event, column.id)"
      >
        <!-- Column Header -->
        <div class="p-4 border-b border-dark-border/50 flex items-center justify-between group">
          <div class="flex items-center gap-2.5">
            <component :is="column.icon" :class="['w-4 h-4', column.color]" />
            <h4 class="text-white font-semibold text-sm">{{ column.name }}</h4>
            <span class="text-xs text-gray-500 font-medium ml-1">{{ tasksByColumn[column.id].length }}</span>
          </div>
          <button class="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-dark-bg text-gray-400 transition-all">
            <Plus class="w-3.5 h-3.5" />
          </button>
        </div>

        <!-- Cards Container -->
        <div class="p-3 flex-1 overflow-y-auto custom-scrollbar">
          <!-- Tasks -->
          <div class="space-y-3">
            <KanbanCard
              v-for="task in tasksByColumn[column.id]"
              :key="task.id"
              :task="task"
              @dragstart="onDragStart($event, task, column.id)"
              @delete="handleDeleteTask"
              :class="task.status === 'Done' ? 'opacity-70 cursor-not-allowed' : ''"
              :draggable="task.status !== 'Done'"
            />
          </div>
          
          <div
            v-if="tasksByColumn[column.id].length === 0"
            class="h-24 border-2 border-dashed border-dark-border rounded-xl flex items-center justify-center"
          >
            <span class="text-xs text-gray-600 font-medium">Drop tasks here</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Create Task Modal -->
    <div v-if="isCreateModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div class="bg-dark-surface border border-dark-border rounded-2xl w-full max-w-md overflow-hidden shadow-2xl flex flex-col max-h-[90vh]">
        <div class="p-6 border-b border-dark-border flex items-center justify-between flex-shrink-0">
          <h3 class="text-lg font-bold text-white">Create New Task</h3>
          <button @click="isCreateModalOpen = false" class="text-gray-400 hover:text-white transition-colors">✕</button>
        </div>
        
        <div class="p-6 overflow-y-auto custom-scrollbar">
          <div v-if="createError" class="mb-4 p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm">
            {{ createError }}
          </div>
          
          <div class="space-y-4">
            <div>
              <label class="block text-xs font-medium text-gray-400 mb-1.5">Task Title *</label>
              <input v-model="newTaskForm.title" type="text" placeholder="E.g. Prepare marketing materials" class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:border-emerald-500 outline-none transition-colors" />
            </div>
            
            <div>
              <label class="block text-xs font-medium text-gray-400 mb-1.5">Description *</label>
              <textarea v-model="newTaskForm.description" rows="3" placeholder="Add more details about what needs to be done..." class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2 text-sm text-white placeholder-gray-600 focus:border-emerald-500 outline-none transition-colors resize-none"></textarea>
            </div>
            
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-gray-400 mb-1.5">Priority</label>
                <select v-model="newTaskForm.priority" class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2 text-sm text-white focus:border-emerald-500 outline-none transition-colors">
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>
              
              <div>
                <label class="block text-xs font-medium text-gray-400 mb-1.5">Deadline (Optional)</label>
                <input v-model="newTaskForm.deadline" type="datetime-local" :min="minDateTime" class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2 text-sm text-white focus:border-emerald-500 outline-none transition-colors" style="color-scheme: dark;" />
              </div>
            </div>
            
            <div>
              <label class="block text-xs font-medium text-gray-400 mb-1.5">Assignee (Optional)</label>
              <select v-model="newTaskForm.assigneeId" :disabled="props.mode === 'me'" class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2 text-sm text-white focus:border-emerald-500 outline-none transition-colors disabled:opacity-50">
                <option value="">Unassigned</option>
                <option v-for="member in props.members" :key="member.userId" :value="member.userId">
                  {{ member.firstName }} {{ member.lastName }} ({{ member.roleName }})
                </option>
                <option v-if="props.mode === 'me'" :value="authStore.user?.id">Me ({{ authStore.user?.firstName }})</option>
              </select>
              <p v-if="props.mode === 'me'" class="text-[10px] text-gray-500 mt-1">In "My Tasks" mode, tasks are automatically assigned to you.</p>
            </div>
            
            <!-- Unit selection only in 'me' mode -->
            <div v-if="props.mode === 'me'">
              <label class="block text-xs font-medium text-gray-400 mb-1.5">Target Unit *</label>
              <select v-model="newTaskForm.targetUnitId" class="w-full bg-dark-bg border border-dark-border rounded-lg px-3 py-2 text-sm text-white focus:border-emerald-500 outline-none transition-colors">
                <option value="" disabled>Select the unit for this task...</option>
                <option v-for="unit in myUnits" :key="unit.id" :value="unit.id">
                  {{ unit.name }}
                </option>
              </select>
            </div>
          </div>
        </div>
        
        <div class="p-6 border-t border-dark-border flex justify-end gap-3 flex-shrink-0 bg-dark-surface/50">
          <button @click="isCreateModalOpen = false" :disabled="isCreating" class="px-4 py-2 rounded-lg text-sm font-medium text-gray-400 hover:text-white transition-colors disabled:opacity-50">Cancel</button>
          <button @click="submitCreateTask" :disabled="isCreating || !newTaskForm.title || !newTaskForm.description" class="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-medium rounded-lg transition-all disabled:opacity-50 disabled:hover:bg-emerald-600 flex items-center gap-2">
            <span v-if="isCreating" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
            Create Task
          </button>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
/* Custom Scrollbar for columns */
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
