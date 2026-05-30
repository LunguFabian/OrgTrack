<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted } from 'vue';
import { Plus, ListTodo, Loader2, ClipboardCheck, CheckCircle2 } from 'lucide-vue-next';
import { tasksService } from '../../api/services/tasks.service';
import { organizationService } from '../../api/services/organization.service';
import { signalrService } from '../../api/services/signalr.service';
import { useToastStore } from '../../stores/toastStore';
import { useAuthStore } from '../../stores/authStore';
import type { TaskDto, UnitMemberDto, WorkloadScoreDto } from '../../types/unit';
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
const workloadScores = ref<WorkloadScoreDto[]>([]);
const isLoadingWorkload = ref(false);

const isLoading = ref(true);
const error = ref('');
const dragOverColumn = ref<string | null>(null);

const columns = [
  { id: 'ToDo', name: 'To Do', icon: ListTodo, color: 'text-text-muted', border: 'border-gray-500/20', bg: 'bg-gray-500/5' },
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

onMounted(async () => {
  await fetchTasks();

  if (props.mode === 'unit' && props.unitId) {
    await signalrService.joinUnitGroup(props.unitId);
  } else if (props.mode === 'me' && myUnits.value.length > 0) {
    for (const unit of myUnits.value) {
      await signalrService.joinUnitGroup(unit.id);
    }
  }

  // Attach real-time listeners
  const mapTask = (raw: any): TaskDto => ({
    id: raw.id || raw.Id,
    title: raw.title || raw.Title,
    description: raw.description || raw.Description,
    status: raw.status || raw.Status,
    priority: raw.priority || raw.Priority,
    deadline: raw.deadline || raw.Deadline,
    organizationUnitId: raw.organizationUnitId || raw.OrganizationUnitId,
    assigneeName: raw.assigneeName || raw.AssigneeName,
    assigneeId: raw.assigneeId || raw.AssigneeId,
    assigneeProfilePictureUrl: raw.assigneeProfilePictureUrl || raw.AssigneeProfilePictureUrl,
    creatorName: raw.creatorName || raw.CreatorName,
    createdAt: raw.createdAt || raw.CreatedAt,
    parentTaskId: raw.parentTaskId || raw.ParentTaskId
  });

  signalrService.on('TaskCreated', (rawTask: any) => {
    const task = mapTask(rawTask);
    if (props.mode === 'unit' || (props.mode === 'me' && task.assigneeId === authStore.user?.id)) {
      if (!tasks.value.some(t => t.id === task.id)) {
        tasks.value.push(task);
      }
    }
  });

  signalrService.on('TaskUpdated', (rawTask: any) => {
    const task = mapTask(rawTask);
    const index = tasks.value.findIndex(t => t.id === task.id);
    if (index !== -1) {
      tasks.value[index] = task;
    } else if (props.mode === 'me' && task.assigneeId === authStore.user?.id) {
      tasks.value.push(task);
    }
  });

  signalrService.on('TaskDeleted', (data: any) => {
    const id = data.id || data.Id;
    tasks.value = tasks.value.filter(t => t.id !== id && t.parentTaskId !== id);
  });
});

onUnmounted(() => {
  if (props.mode === 'unit' && props.unitId) {
    signalrService.leaveUnitGroup(props.unitId);
  } else if (props.mode === 'me' && myUnits.value.length > 0) {
    for (const unit of myUnits.value) {
      signalrService.leaveUnitGroup(unit.id);
    }
  }
  signalrService.off('TaskCreated');
  signalrService.off('TaskUpdated');
  signalrService.off('TaskDeleted');
});

const tasksByColumn = computed(() => {
  const grouped: Record<string, TaskDto[]> = {
    ToDo: [],
    InProgress: [],
    WaitingForApproval: [],
    Done: []
  };
  
  tasks.value.forEach(task => {
    // Only show top-level tasks on the board
    if (!task.parentTaskId && grouped[task.status]) {
      grouped[task.status].push(task);
    }
  });
  
  return grouped;
});

const getSubTasks = (parentId: string) => {
  return tasks.value.filter(t => t.parentTaskId === parentId);
};
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

  if (!task.assigneeId && newStatus !== 'ToDo') {
    toastStore.showToast('Unassigned tasks cannot be moved out of "To Do". Please assign someone first.', 'error');
    return;
  }
  
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

const confirmingDeleteTaskId = ref<string | null>(null);

const handleDeleteTask = async (task: TaskDto) => {
  try {
    await tasksService.deleteTask(task.organizationUnitId, task.id);
    tasks.value = tasks.value.filter(t => t.id !== task.id);
    confirmingDeleteTaskId.value = null;
    toastStore.showToast('Task deleted successfully.', 'success');
  } catch (err: any) {
    console.error('Failed to delete task', err);
    toastStore.showToast(err.response?.data?.error || 'Failed to delete task.', 'error');
  }
};
const isTaskModalOpen = ref(false);
const isEditingTask = ref(false);
const editingTaskId = ref<string | null>(null);

const isCreating = ref(false);
const createError = ref('');
const taskForm = ref({
  title: '',
  description: '',
  priority: 'Medium',
  assigneeId: '' as string | null,
  deadline: '' as string | null,
  targetUnitId: '',
  parentTaskId: null as string | null,
  status: 'ToDo'
});

const minDateTime = ref('');

const openCreateModal = async (parentTaskId: string | null = null, defaultStatus: string = 'ToDo') => {
  createError.value = '';
  isEditingTask.value = false;
  editingTaskId.value = null;
  const now = new Date();
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
  minDateTime.value = now.toISOString().slice(0, 16);

  taskForm.value = {
    title: '',
    description: '',
    priority: 'Medium',
    assigneeId: props.mode === 'me' ? (authStore.user?.id || '') : '',
    deadline: '',
    targetUnitId: props.mode === 'unit' ? props.unitId! : '',
    parentTaskId: parentTaskId,
    status: defaultStatus
  };
  isTaskModalOpen.value = true;
  
  if (props.mode === 'unit' && props.unitId) {
    await fetchWorkload(props.unitId);
  }
};

const openEditModal = async (task: TaskDto) => {
  createError.value = '';
  isEditingTask.value = true;
  editingTaskId.value = task.id;
  const now = new Date();
  now.setMinutes(now.getMinutes() - now.getTimezoneOffset());
  minDateTime.value = now.toISOString().slice(0, 16);

  taskForm.value = {
    title: task.title,
    description: task.description || '',
    priority: task.priority,
    assigneeId: task.assigneeId || null,
    deadline: task.deadline ? new Date(task.deadline).toISOString().slice(0, 16) : null,
    targetUnitId: task.organizationUnitId,
    parentTaskId: task.parentTaskId || null,
    status: task.status
  };
  isTaskModalOpen.value = true;
  
  if (props.mode === 'unit') {
    await fetchWorkload(task.organizationUnitId);
  }
};

const fetchWorkload = async (unitId: string) => {
  isLoadingWorkload.value = true;
  try {
    workloadScores.value = await tasksService.getUnitWorkload(unitId);
  } catch (err) {
    console.error('Failed to fetch workload', err);
  } finally {
    isLoadingWorkload.value = false;
  }
};

const sortedMembers = computed(() => {
  if (workloadScores.value.length === 0) return props.members;
  
  // Sort members so that the order matches the workloadScores order
  const sorted = [...(props.members || [])].sort((a, b) => {
    const scoreA = workloadScores.value.findIndex(ws => ws.userId === a.userId);
    const scoreB = workloadScores.value.findIndex(ws => ws.userId === b.userId);
    // If not found in scores, put at the end
    const idxA = scoreA === -1 ? 999 : scoreA;
    const idxB = scoreB === -1 ? 999 : scoreB;
    return idxA - idxB;
  });
  return sorted;
});

const isRecommended = (userId: string) => {
  if (workloadScores.value.length === 0) return false;
  const minScore = workloadScores.value[0]?.finalScore;
  const userScore = workloadScores.value.find(w => w.userId === userId)?.finalScore;
  return userScore === minScore;
};

const getWorkloadTooltip = (userId: string) => {
  const ws = workloadScores.value.find(w => w.userId === userId);
  if (!ws) return '';
  return `Load: ${ws.currentWorkloadRaw} | Velocity: ${ws.velocityDaysRaw.toFixed(1)}d | Affinity: ${ws.affinityRaw} | Idle: ${ws.daysSinceLastAssignmentRaw}d | Subtasks: ${ws.subtasksComplexityRaw}`;
};

const handleSubtaskStatusChange = async (subTask: TaskDto, newStatus: string) => {
  if (!subTask.assigneeId && newStatus !== 'ToDo') {
    toastStore.showToast('Unassigned sub-tasks cannot be moved out of "To Do".', 'error');
    return;
  }

  try {
    const updated = await tasksService.updateTaskStatus(subTask.organizationUnitId, subTask.id, newStatus);
    const index = tasks.value.findIndex(t => t.id === subTask.id);
    if (index !== -1) tasks.value[index] = updated;
  } catch (err: any) {
    toastStore.showToast(err.response?.data?.error || 'Failed to update subtask status.', 'error');
  }
};

const submitTask = async () => {
  if (!taskForm.value.title || !taskForm.value.description) {
    createError.value = 'Title and description are required.';
    return;
  }

  if (props.mode === 'me' && !taskForm.value.targetUnitId && !isEditingTask.value) {
    createError.value = 'Please select a unit for this task.';
    return;
  }
  
  if (taskForm.value.deadline) {
    const selectedDate = new Date(taskForm.value.deadline);
    if (selectedDate < new Date() && !isEditingTask.value) {
      createError.value = 'The deadline cannot be set in the past.';
      return;
    }
  }

  if (taskForm.value.status !== 'ToDo' && !taskForm.value.assigneeId) {
    createError.value = 'Tasks outside "To Do" must have an assignee.';
    return;
  }
  
  createError.value = '';
  isCreating.value = true;
  
  try {
    const payload = {
      title: taskForm.value.title,
      description: taskForm.value.description,
      priority: taskForm.value.priority,
      assigneeId: taskForm.value.assigneeId || null,
      deadline: taskForm.value.deadline ? new Date(taskForm.value.deadline).toISOString() : null,
      parentTaskId: taskForm.value.parentTaskId,
      status: taskForm.value.status
    };
    
    if (isEditingTask.value && editingTaskId.value) {
      const updatedTask = await tasksService.updateTask(taskForm.value.targetUnitId, editingTaskId.value, payload);
      const index = tasks.value.findIndex(t => t.id === editingTaskId.value);
      if (index !== -1) tasks.value[index] = updatedTask;
      toastStore.showToast('Task updated successfully.', 'success');
    } else {
      const unitToCreateIn = props.mode === 'me' ? taskForm.value.targetUnitId : props.unitId!;
      const newTask = await tasksService.createTask(unitToCreateIn, payload);
      // Avoid duplication if SignalR already pushed it
      if (!tasks.value.some(t => t.id === newTask.id)) {
        tasks.value.push(newTask);
      }
      toastStore.showToast('Task created successfully.', 'success');
    }
    
    isTaskModalOpen.value = false;
  } catch (err: any) {
    createError.value = err.response?.data?.error || `Failed to ${isEditingTask.value ? 'update' : 'create'} task.`;
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
        <h3 class="text-text-strong font-bold text-lg">Task Board</h3>
        <span class="px-2 py-0.5 rounded-full bg-bg border border-border text-xs text-text-muted font-medium">
          {{ tasks.length }} tasks
        </span>
      </div>
      
      <button @click="openCreateModal(null)" class="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white text-sm font-medium rounded-lg transition-all shadow-lg shadow-emerald-500/20">
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
        <span class="text-text-muted text-sm font-medium">Loading board...</span>
      </div>
    </div>

    <!-- Kanban Columns -->
    <div v-else class="flex gap-6 h-full overflow-x-auto pb-4 custom-scrollbar">
      <div
        v-for="column in columns"
        :key="column.id"
        class="flex-shrink-0 w-[300px] flex flex-col rounded-2xl border transition-colors duration-200"
        :class="[
          dragOverColumn === column.id ? `border-${column.color.split('-')[1]}-500/50 ${column.bg}` : 'border-border bg-surface/50'
        ]"
        @dragover.prevent
        @dragenter.prevent="onDragEnter(column.id)"
        @dragleave.prevent="onDragLeave(column.id)"
        @drop="onDrop($event, column.id)"
      >
        <!-- Column Header -->
        <div class="p-4 border-b border-border/50 flex items-center justify-between group">
          <div class="flex items-center gap-2.5">
            <component :is="column.icon" :class="['w-4 h-4', column.color]" />
            <h4 class="text-text-strong font-semibold text-sm">{{ column.name }}</h4>
            <span class="text-xs text-text-muted font-medium ml-1">{{ tasksByColumn[column.id].length }}</span>
          </div>
          <button @click="openCreateModal(null, column.id)" class="opacity-0 group-hover:opacity-100 p-1 rounded hover:bg-bg text-text-muted hover:text-emerald-400 transition-all">
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
              :sub-tasks="getSubTasks(task.id)"
              :confirmingDelete="confirmingDeleteTaskId === task.id"
              @dragstart="onDragStart($event, task, column.id)"
              @request-delete="confirmingDeleteTaskId = task.id"
              @confirm-delete="handleDeleteTask"
              @cancel-delete="confirmingDeleteTaskId = null"
              @edit="openEditModal"
              :class="task.status === 'Done' ? 'opacity-70 cursor-not-allowed' : ''"
              :draggable="task.status !== 'Done'"
            />
          </div>
          
          <div
            v-if="tasksByColumn[column.id].length === 0"
            class="h-24 border-2 border-dashed border-border rounded-xl flex items-center justify-center"
          >
            <span class="text-xs text-gray-600 font-medium">Drop tasks here</span>
          </div>
        </div>
      </div>
    </div>

    <!-- Task Modal -->
    <div v-if="isTaskModalOpen" class="fixed inset-0 bg-black/60 backdrop-blur-sm z-50 flex items-center justify-center p-4">
      <div class="bg-surface border border-border rounded-2xl w-full max-w-md overflow-hidden shadow-2xl flex flex-col max-h-[90vh]">
        <div class="p-6 border-b border-border flex items-center justify-between flex-shrink-0">
          <h3 class="text-lg font-bold text-text-strong">{{ isEditingTask ? 'Edit Task' : 'Create New Task' }}</h3>
          <button @click="isTaskModalOpen = false" class="text-text-muted hover:text-text-strong transition-colors">✕</button>
        </div>
        
        <div class="p-6 overflow-y-auto custom-scrollbar">
          <div v-if="createError" class="mb-4 p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm">
            {{ createError }}
          </div>
          
          <div class="space-y-4">
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1.5">Task Title *</label>
              <input v-model="taskForm.title" type="text" placeholder="E.g. Prepare marketing materials" class="w-full bg-bg border border-border rounded-lg px-3 py-2 text-sm text-text-strong placeholder-gray-600 focus:border-emerald-500 outline-none transition-colors" />
            </div>
            
            <div>
              <label class="block text-xs font-medium text-text-muted mb-1.5">Description *</label>
              <textarea v-model="taskForm.description" rows="3" placeholder="Add more details about what needs to be done..." class="w-full bg-bg border border-border rounded-lg px-3 py-2 text-sm text-text-strong placeholder-gray-600 focus:border-emerald-500 outline-none transition-colors resize-none"></textarea>
            </div>
            
            <div class="grid grid-cols-2 gap-4">
              <div>
                <label class="block text-xs font-medium text-text-muted mb-1.5">Priority</label>
                <select v-model="taskForm.priority" class="w-full bg-bg border border-border rounded-lg px-3 py-2 text-sm text-text-strong focus:border-emerald-500 outline-none transition-colors">
                  <option value="Low">Low</option>
                  <option value="Medium">Medium</option>
                  <option value="High">High</option>
                  <option value="Critical">Critical</option>
                </select>
              </div>
              
              <div>
                <label class="block text-xs font-medium text-text-muted mb-1.5">Deadline (Optional)</label>
                <input v-model="taskForm.deadline" type="datetime-local" :min="minDateTime" class="w-full bg-bg border border-border rounded-lg px-3 py-2 text-sm text-text-strong focus:border-emerald-500 outline-none transition-colors" style="color-scheme: dark;" />
              </div>
            </div>
            
            <div>
              <div class="flex items-center justify-between mb-1.5">
                <label class="block text-xs font-medium text-text-muted">Assignee (Optional)</label>
                <span v-if="isLoadingWorkload" class="text-[10px] text-emerald-400 flex items-center gap-1">
                  <Loader2 class="w-3 h-3 animate-spin" /> Analyzing workload...
                </span>
              </div>
              <select v-model="taskForm.assigneeId" :disabled="props.mode === 'me'" class="w-full bg-bg border border-border rounded-lg px-3 py-2 text-sm text-text-strong focus:border-emerald-500 outline-none transition-colors disabled:opacity-50">
                <option value="">Unassigned</option>
                <option 
                  v-for="member in sortedMembers" 
                  :key="member.userId" 
                  :value="member.userId"
                  :title="getWorkloadTooltip(member.userId)"
                >
                  {{ isRecommended(member.userId) ? '⭐ ' : '' }}{{ member.firstName }} {{ member.lastName }}
                </option>
                <option v-if="props.mode === 'me'" :value="authStore.user?.id">Me ({{ authStore.user?.firstName }})</option>
              </select>
              <p v-if="props.mode === 'me'" class="text-[10px] text-text-muted mt-1">In "My Tasks" mode, tasks are automatically assigned to you.</p>
              <p v-else-if="workloadScores.length > 0" class="text-[10px] text-text-muted mt-1">
                ⭐ means the user is recommended
              </p>
            </div>
            
            <!-- Unit selection only in 'me' mode -->
            <div v-if="props.mode === 'me'">
              <label class="block text-xs font-medium text-text-muted mb-1.5">Target Unit *</label>
              <select v-model="taskForm.targetUnitId" :disabled="isEditingTask" class="w-full bg-bg border border-border rounded-lg px-3 py-2 text-sm text-text-strong focus:border-emerald-500 outline-none transition-colors disabled:opacity-50">
                <option value="" disabled>Select the unit for this task...</option>
                <option v-for="unit in myUnits" :key="unit.id" :value="unit.id">
                  {{ unit.name }}
                </option>
              </select>
            </div>
            
            <!-- SubTasks Section in Edit Mode -->
            <div v-if="isEditingTask && !taskForm.parentTaskId" class="mt-6 pt-6 border-t border-border">
              <div class="flex items-center justify-between mb-3">
                <label class="block text-xs font-semibold text-text-strong uppercase tracking-wider">Sub-tasks</label>
                <button @click="openCreateModal(editingTaskId)" class="text-xs text-emerald-400 hover:text-emerald-300 font-medium flex items-center gap-1">
                  <Plus class="w-3 h-3" /> Add sub-task
                </button>
              </div>
              
              <div class="space-y-2">
                <div v-for="subTask in getSubTasks(editingTaskId!)" :key="subTask.id" class="flex items-center justify-between p-3 bg-bg border border-border rounded-lg group">
                  <div class="flex items-center gap-3">
                    <button 
                      @click="handleSubtaskStatusChange(subTask, subTask.status === 'Done' ? 'ToDo' : 'Done')"
                      class="w-5 h-5 rounded-full border flex items-center justify-center transition-colors"
                      :class="subTask.status === 'Done' ? 'bg-emerald-500 border-emerald-500 text-white' : 'border-gray-500 hover:border-emerald-400'"
                    >
                      <CheckCircle2 v-if="subTask.status === 'Done'" class="w-3.5 h-3.5" />
                    </button>
                    <span :class="['text-sm font-medium', subTask.status === 'Done' ? 'text-text-muted line-through' : 'text-text-strong']">
                      {{ subTask.title }}
                    </span>
                  </div>
                  <button @click="openEditModal(subTask)" class="opacity-0 group-hover:opacity-100 p-1 text-text-muted hover:text-emerald-400 transition-opacity">
                    Edit
                  </button>
                </div>
                
                <div v-if="getSubTasks(editingTaskId!).length === 0" class="text-center p-4 text-xs text-text-muted border border-dashed border-border rounded-lg">
                  No sub-tasks. Break down your task into smaller steps.
                </div>
              </div>
            </div>
          </div>
        </div>
        
        <div class="p-6 border-t border-border flex items-center justify-end gap-3 flex-shrink-0 bg-surface">
          <button @click="isTaskModalOpen = false" class="px-4 py-2 text-sm font-medium text-text-muted hover:text-text-strong transition-colors">
            Cancel
          </button>
          <button 
            @click="submitTask" 
            :disabled="isCreating"
            class="px-4 py-2 bg-emerald-600 hover:bg-emerald-500 disabled:bg-emerald-600/50 text-white text-sm font-medium rounded-lg transition-colors shadow-lg shadow-emerald-500/20 flex items-center gap-2"
          >
            <span v-if="isCreating" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
            {{ isEditingTask ? 'Save Changes' : 'Create Task' }}
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
