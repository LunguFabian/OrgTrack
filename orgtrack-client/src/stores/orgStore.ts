import { defineStore } from 'pinia';
import { ref } from 'vue';
import type { OrganizationUnitDto } from '../types/organization';
import { organizationService } from '../api/services/organization.service';

export const useOrgStore = defineStore('organization', () => {
  const tree = ref<OrganizationUnitDto[]>([]);
  const myUnits = ref<OrganizationUnitDto[]>([]);
  const isLoading = ref(false);
  const error = ref<string | null>(null);

  async function fetchTree() {
    isLoading.value = true;
    error.value = null;
    try {
      tree.value = await organizationService.getFullTree();
    } catch (err: any) {
      error.value = err.response?.data?.error || 'Failed to load organization tree';
      console.error(err);
    } finally {
      isLoading.value = false;
    }
  }

  async function fetchMyUnits() {
    try {
      myUnits.value = await organizationService.getMyUnits();
    } catch (err) {
      console.error('Failed to load user units', err);
    }
  }

  async function createUnit(data: import('../types/organization').CreateUnitRequest) {
    await organizationService.createUnit(data);
    await fetchTree();
  }

  return {
    tree,
    myUnits,
    isLoading,
    error,
    fetchTree,
    fetchMyUnits,
    createUnit
  };
});
