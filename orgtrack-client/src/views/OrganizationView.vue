<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useOrgStore } from '../stores/orgStore';
import OrgTreeNode from '../components/organization/OrgTreeNode.vue';
import CreateUnitModal from '../components/organization/CreateUnitModal.vue';
import { Network, Plus } from 'lucide-vue-next';

const orgStore = useOrgStore();
const isModalOpen = ref(false);
const selectedParentId = ref<string | null>(null);
const selectedParentType = ref<string | undefined>(undefined);

onMounted(() => {
  if (orgStore.tree.length === 0) {
    orgStore.fetchTree();
  }
});

const handleAddChild = (payload: { id: string | null; type?: string }) => {
  selectedParentId.value = payload.id;
  selectedParentType.value = payload.type;
  isModalOpen.value = true;
};
</script>

<template>
  <div class="space-y-6 max-w-4xl mx-auto pb-20">
    <!-- Header -->
    <div class="flex items-center justify-between bg-dark-surface p-6 rounded-2xl border border-dark-border shadow-sm">
      <div class="flex items-center gap-4">
        <div class="p-3 bg-emerald-500/10 rounded-xl text-emerald-400">
          <Network class="w-6 h-6" />
        </div>
        <div>
          <h1 class="text-2xl font-bold text-white tracking-tight">Organization Structure</h1>
          <p class="text-gray-400 text-sm mt-1">Manage all committees, departments, and teams.</p>
        </div>
      </div>
      <button 
        @click="handleAddChild({ id: null })"
        class="flex items-center gap-2 px-4 py-2 bg-emerald-600 hover:bg-emerald-500 text-white font-medium rounded-lg shadow-lg shadow-emerald-500/20 transition-all"
      >
        <Plus class="w-4 h-4" />
        New Root Unit
      </button>
    </div>

    <!-- Loading state -->
    <div v-if="orgStore.isLoading" class="flex justify-center py-20">
      <div class="animate-pulse flex flex-col items-center gap-4">
        <div class="w-10 h-10 border-4 border-emerald-500 border-t-transparent rounded-full animate-spin"></div>
        <p class="text-gray-400">Loading organization tree...</p>
      </div>
    </div>

    <!-- Error state -->
    <div v-else-if="orgStore.error" class="p-4 bg-red-500/10 border border-red-500/20 rounded-xl text-red-400">
      {{ orgStore.error }}
    </div>

    <!-- Tree View -->
    <div v-else class="pt-4 pl-2">
      <div v-if="orgStore.tree.length === 0" class="text-center py-12 border-2 border-dashed border-dark-border rounded-xl">
        <Network class="w-12 h-12 text-gray-600 mx-auto mb-3" />
        <p class="text-gray-400">No organizational units found.</p>
        <button class="mt-4 text-emerald-400 hover:underline">Create the first one</button>
      </div>
      
      <!-- Root Nodes -->
      <OrgTreeNode 
        v-for="rootNode in orgStore.tree" 
        :key="rootNode.id" 
        :node="rootNode" 
        :level="0"
        @add-child="handleAddChild"
      />
    </div>

    <!-- Modals -->
    <CreateUnitModal 
      :is-open="isModalOpen" 
      :parent-id="selectedParentId"
      :parent-type="selectedParentType"
      @close="isModalOpen = false"
    />
  </div>
</template>
