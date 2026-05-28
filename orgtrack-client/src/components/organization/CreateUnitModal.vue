<script setup lang="ts">
import { ref } from 'vue';
import { useOrgStore } from '../../stores/orgStore';
import { X, Network, Save } from 'lucide-vue-next';

const props = defineProps<{
  isOpen: boolean;
  parentId: string | null;
  parentType?: string;
}>();

const emit = defineEmits<{
  (e: 'close'): void;
  (e: 'created'): void;
}>();

const orgStore = useOrgStore();

const form = ref({
  name: '',
  description: '',
  type: 'Committee', // Default
  departmentType: ''
});
import { watch } from 'vue';
watch(() => props.isOpen, (newVal) => {
  if (newVal) {
    if (!props.parentId) form.value.type = 'Committee'; // New Root usually means Committee or National
    else if (props.parentType === 'National') form.value.type = 'Committee';
    else if (props.parentType === 'Committee') form.value.type = 'Department';
    else if (props.parentType === 'Department') form.value.type = 'Team';
    else form.value.type = 'Team'; // Fallback
  }
});

const isSubmitting = ref(false);
const error = ref('');

const handleSubmit = async () => {
  if (!form.value.name.trim()) {
    error.value = 'Name is required';
    return;
  }
  
  error.value = '';
  isSubmitting.value = true;
  
  try {
    await orgStore.createUnit({
      name: form.value.name,
      description: form.value.description || 'No description provided',
      type: form.value.type,
      departmentType: form.value.type === 'Department' ? form.value.departmentType : null,
      parentUnitId: props.parentId
    });
    
    emit('created');
    emit('close');
    form.value.name = '';
    form.value.description = '';
    form.value.type = props.parentId ? 'Committee' : 'National'; 
    form.value.departmentType = '';
  } catch (err: any) {
    error.value = err.response?.data?.error || 'Failed to create unit';
  } finally {
    isSubmitting.value = false;
  }
};

const unitTypes = [
  { value: 'National', label: 'National Board' },
  { value: 'Committee', label: 'Local Committee' },
  { value: 'Department', label: 'Department' },
  { value: 'Team', label: 'Team' }
];
</script>

<template>
  <div v-if="isOpen" class="fixed inset-0 z-50 flex items-center justify-center p-4">
    <!-- Backdrop Blur Overlay -->
    <div 
      class="absolute inset-0 bg-black/60 backdrop-blur-sm transition-opacity"
      @click="emit('close')"
    ></div>

    <!-- Modal Content -->
    <div class="relative bg-surface border border-border rounded-2xl shadow-2xl w-full max-w-md overflow-hidden transform transition-all">
      <!-- Header -->
      <div class="px-6 py-4 border-b border-border flex justify-between items-center bg-bg/50">
        <div class="flex items-center gap-3">
          <div class="p-2 bg-emerald-500/10 rounded-lg text-emerald-400">
            <Network class="w-5 h-5" />
          </div>
          <h3 class="text-lg font-bold text-text-strong">
            {{ parentId ? 'Add Sub-unit' : 'Create Root Unit' }}
          </h3>
        </div>
        <button 
          @click="emit('close')"
          class="text-text-muted hover:text-text-strong transition-colors p-1"
        >
          <X class="w-5 h-5" />
        </button>
      </div>

      <!-- Body -->
      <form @submit.prevent="handleSubmit" class="p-6 space-y-5">
        <div v-if="error" class="p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm">
          {{ error }}
        </div>

        <div>
          <label class="block text-sm font-medium text-text-muted mb-1.5">Unit Name</label>
          <input 
            v-model="form.name"
            type="text" 
            placeholder="e.g. Marketing Department" 
            class="w-full px-4 py-2.5 bg-bg border border-border rounded-xl text-text-strong placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/50 focus:border-emerald-500 transition-all"
            autofocus
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-text-muted mb-1.5">Description</label>
          <input 
            v-model="form.description"
            type="text" 
            placeholder="e.g. Focuses on marketing campaigns" 
            class="w-full px-4 py-2.5 bg-bg border border-border rounded-xl text-text-strong placeholder-gray-500 focus:outline-none focus:ring-2 focus:ring-emerald-500/50 focus:border-emerald-500 transition-all"
          />
        </div>

        <div>
          <label class="block text-sm font-medium text-text-muted mb-1.5">Unit Type</label>
          <select 
            v-model="form.type"
            class="w-full px-4 py-2.5 bg-bg border border-border rounded-xl text-text-strong focus:outline-none focus:ring-2 focus:ring-emerald-500/50 focus:border-emerald-500 transition-all appearance-none"
          >
            <option v-for="type in unitTypes" :key="type.value" :value="type.value">
              {{ type.label }}
            </option>
          </select>
        </div>

        <div v-if="form.type === 'Department'">
          <label class="block text-sm font-medium text-text-muted mb-1.5">Department Category</label>
          <select 
            v-model="form.departmentType"
            class="w-full px-4 py-2.5 bg-bg border border-border rounded-xl text-text-strong focus:outline-none focus:ring-2 focus:ring-emerald-500/50 focus:border-emerald-500 transition-all appearance-none"
          >
            <option value="HR">Human Resources (HR)</option>
            <option value="IT">Information Technology (IT)</option>
            <option value="Finance">Finance & Legal</option>
            <option value="Marketing">Marketing (B2C)</option>
            <option value="Sales">Sales (B2B)</option>
            <option value="Other">Other</option>
          </select>
        </div>

        <!-- Footer Buttons -->
        <div class="pt-4 flex items-center justify-end gap-3 border-t border-border mt-6">
          <button 
            type="button"
            @click="emit('close')"
            class="px-4 py-2 rounded-lg text-sm font-medium text-text-muted hover:text-text-strong hover:bg-border transition-all"
          >
            Cancel
          </button>
          <button 
            type="submit"
            :disabled="isSubmitting"
            class="flex items-center gap-2 px-5 py-2 rounded-lg text-sm font-medium bg-emerald-600 hover:bg-emerald-500 text-white shadow-lg shadow-emerald-500/20 transition-all disabled:opacity-50 disabled:cursor-not-allowed"
          >
            <span v-if="isSubmitting" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
            <Save v-else class="w-4 h-4" />
            {{ isSubmitting ? 'Saving...' : 'Create Unit' }}
          </button>
        </div>
      </form>
    </div>
  </div>
</template>
