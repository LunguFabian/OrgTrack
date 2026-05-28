<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { api } from '../../api/axios';
import { Mail, Shield, Calendar, UserMinus, Link2, Copy, Check, Clock, Users, ChevronsUp } from 'lucide-vue-next';
import { inviteService } from '../../api/services/invite.service';
import type { UnitMemberDto, InviteLinkCreatedDto } from '../../types/unit';
import { useOrgStore } from '../../stores/orgStore';
import { useToastStore } from '../../stores/toastStore';
import type { OrganizationUnitDto } from '../../types/organization';

const toastStore = useToastStore();

const props = defineProps<{
  unitId: string;
  members: UnitMemberDto[];
  isLoading: boolean;
}>();

const emit = defineEmits<{
  (e: 'member-removed', userId: string): void;
  (e: 'role-updated'): void;
}>();

const orgStore = useOrgStore();
onMounted(() => {
  if (orgStore.tree.length === 0) orgStore.fetchTree();
});
const showInviteForm = ref(false);
const inviteForm = ref({
  expiresInHours: 48,
  maxUses: 10
});
const generatedLink = ref<InviteLinkCreatedDto | null>(null);
const isGenerating = ref(false);
const copied = ref(false);
const inviteError = ref('');

const generateInviteLink = async () => {
  inviteError.value = '';
  isGenerating.value = true;
  try {
    generatedLink.value = await inviteService.generateLink({
      organizationUnitId: props.unitId,
      roleName: 'Member', // Invite links are ALWAYS for Member role only
      expiresInHours: inviteForm.value.expiresInHours,
      maxUses: inviteForm.value.maxUses
    });
  } catch (err: any) {
    inviteError.value = err.response?.data?.error || 'Failed to generate link';
  } finally {
    isGenerating.value = false;
  }
};

const copyLink = async () => {
  if (!generatedLink.value) return;
  await navigator.clipboard.writeText(generatedLink.value.inviteUrl);
  copied.value = true;
  setTimeout(() => { copied.value = false; }, 2500);
};

const resetInviteForm = () => {
  showInviteForm.value = false;
  generatedLink.value = null;
  inviteError.value = '';
  inviteForm.value = { expiresInHours: 48, maxUses: 10 };
};
const assignRoleModal = ref<{ open: boolean; member: UnitMemberDto | null }>({ open: false, member: null });
const leadershipRoles = [
  { value: 'Member', label: 'Member' },
  { value: 'TeamLeader', label: 'Team Leader (TL)' },
  { value: 'LocalVicePresident', label: 'Vice President (VP)' },
  { value: 'LocalPresident', label: 'Local Committee President (LCP)' },
  { value: 'NationalVicePresident', label: 'Member Committee VP (MCVP)' },
  { value: 'NationalPresident', label: 'Member Committee President (MCP)' },
];
const selectedRole = ref('Member');
const selectedTargetUnitId = ref('');

const openAssignRole = (member: UnitMemberDto) => {
  assignRoleModal.value = { open: true, member };
  selectedRole.value = member.roleName;
  selectedTargetUnitId.value = ''; // Reset target
};

const flattenUnits = (units: OrganizationUnitDto[]): OrganizationUnitDto[] => {
  let result: OrganizationUnitDto[] = [];
  for (const u of units) {
    result.push(u);
    if (u.children) result.push(...flattenUnits(u.children));
  }
  return result;
};

const allUnitsFlattened = computed(() => flattenUnits(orgStore.tree));

const targetUnitRules = computed(() => {
  const role = selectedRole.value;
  if (role === 'NationalPresident') return { type: 'National' };
  if (role === 'NationalVicePresident') return { type: 'Department', parentType: 'National' };
  if (role === 'LocalPresident') return { type: 'Committee' };
  if (role === 'LocalVicePresident' || role.includes('VicePresident')) return { type: 'Department', parentType: 'Committee' };
  if (role === 'TeamLeader' || role === 'Member') return { type: 'Team' };
  return null;
});

const availableTargetUnits = computed(() => {
  const rule = targetUnitRules.value;
  if (!rule) return [];
  
  return allUnitsFlattened.value.filter(u => {
    if (u.type !== rule.type) return false;
    if (rule.parentType) {
      const parent = allUnitsFlattened.value.find(p => p.id === u.parentUnitId);
      if (!parent || parent.type !== rule.parentType) return false;
    }
    
    return true;
  });
});

const isAssigningRole = ref(false);

const confirmAssignRole = async () => {
  if (!assignRoleModal.value.member) return;
  
  isAssigningRole.value = true;
  try {
    const userId = assignRoleModal.value.member.userId;
    const currentUnitId = assignRoleModal.value.member.unitId || props.unitId;
    
    await api.put(`/organization/units/${currentUnitId}/members/${userId}/role`, {
      roleName: selectedRole.value,
      targetUnitId: selectedTargetUnitId.value || null
    });
    
    emit('role-updated');
    assignRoleModal.value = { open: false, member: null };
    toastStore.showToast(`Role updated successfully.`, 'success');
  } catch (err: any) {
    console.error('Failed to assign role:', err.response?.data?.error);
    toastStore.showToast(err.response?.data?.error || 'Failed to assign role', 'error');
  } finally {
    isAssigningRole.value = false;
  }
};
const roleColor = (role: string) => {
  if (role.includes('National')) return 'bg-blue-500/15 text-blue-400 border-blue-500/20';
  if (role.includes('President')) return 'bg-purple-500/15 text-purple-400 border-purple-500/20';
  if (role.includes('Leader')) return 'bg-orange-500/15 text-orange-400 border-orange-500/20';
  return 'bg-gray-500/15 text-text-muted border-gray-500/20';
};

const initials = (m: UnitMemberDto) =>
  `${m.firstName[0]}${m.lastName[0]}`.toUpperCase();

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'short', year: 'numeric' });
</script>

<template>
  <div class="space-y-6">

    <!-- Members Header -->
    <div class="flex items-center justify-between">
      <div class="flex items-center gap-2">
        <Users class="w-5 h-5 text-text-muted" />
        <span class="text-text-strong font-semibold">{{ members.length }} Member{{ members.length !== 1 ? 's' : '' }}</span>
      </div>
      <button
        @click="showInviteForm = !showInviteForm"
        class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium bg-emerald-600 hover:bg-emerald-500 text-white transition-all shadow-lg shadow-emerald-500/20"
      >
        <Link2 class="w-4 h-4" />
        Generate Invite Link
      </button>
    </div>

    <!-- Invite Link Generator Panel -->
    <div v-if="showInviteForm" class="bg-bg border border-emerald-500/20 rounded-2xl p-5 space-y-4">
      <h4 class="text-text-strong font-semibold flex items-center gap-2">
        <Link2 class="w-4 h-4 text-emerald-400" />
        New Invite Link
      </h4>

      <div v-if="!generatedLink" class="space-y-4">
        <div v-if="inviteError" class="p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm">
          {{ inviteError }}
        </div>

        <!-- Info: invite links are Member-only -->
        <div class="flex items-start gap-2 p-3 bg-surface border border-border rounded-lg text-xs text-text-muted">
          <Shield class="w-4 h-4 text-emerald-400 flex-shrink-0 mt-0.5" />
          <span>Invite links always assign the <strong class="text-text-strong">Member</strong> role. To grant leadership roles (TL, VP, LCP…), use <strong class="text-text-strong">Assign Role</strong> on an existing member.</span>
        </div>

        <div class="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <div>
            <label class="block text-xs font-medium text-text-muted mb-1.5">Expires in (hours)</label>
            <input v-model.number="inviteForm.expiresInHours" type="number" min="1" max="720"
              class="w-full px-3 py-2 bg-surface border border-border rounded-lg text-text-strong text-sm focus:outline-none focus:border-emerald-500 transition-colors" />
          </div>
          <div>
            <label class="block text-xs font-medium text-text-muted mb-1.5">Max uses</label>
            <input v-model.number="inviteForm.maxUses" type="number" min="1" max="500"
              class="w-full px-3 py-2 bg-surface border border-border rounded-lg text-text-strong text-sm focus:outline-none focus:border-emerald-500 transition-colors" />
          </div>
        </div>

        <div class="flex items-center gap-3">
          <button @click="generateInviteLink" :disabled="isGenerating"
            class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium bg-emerald-600 hover:bg-emerald-500 text-white transition-all disabled:opacity-50">
            <span v-if="isGenerating" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
            <Link2 v-else class="w-4 h-4" />
            {{ isGenerating ? 'Generating...' : 'Generate' }}
          </button>
          <button @click="resetInviteForm" class="px-4 py-2 rounded-lg text-sm text-text-muted hover:text-text-strong transition-colors">
            Cancel
          </button>
        </div>
      </div>

      <!-- Generated Link Result -->
      <div v-else class="space-y-4">
        <div class="flex items-center gap-2 text-emerald-400 text-sm font-medium">
          <Check class="w-4 h-4" />
          Link generated successfully!
        </div>
        <div class="flex items-center gap-2 bg-surface border border-border rounded-lg px-4 py-3">
          <span class="flex-1 text-sm text-text-muted truncate font-mono">{{ generatedLink.inviteUrl }}</span>
          <button @click="copyLink" class="flex-shrink-0 flex items-center gap-1.5 text-xs font-medium text-emerald-400 hover:text-emerald-300 transition-colors">
            <Check v-if="copied" class="w-4 h-4" />
            <Copy v-else class="w-4 h-4" />
            {{ copied ? 'Copied!' : 'Copy' }}
          </button>
        </div>
        <div class="flex items-center gap-2 text-xs text-text-muted">
          <Clock class="w-3.5 h-3.5" />
          Expires: {{ formatDate(generatedLink.expiresAt) }}
        </div>
        <button @click="resetInviteForm" class="text-sm text-text-muted hover:text-text-strong transition-colors">
          Generate another link
        </button>
      </div>
    </div>

    <!-- Loading State -->
    <div v-if="isLoading" class="space-y-3">
      <div v-for="i in 3" :key="i" class="h-16 bg-bg rounded-xl animate-pulse"></div>
    </div>

    <!-- Empty State -->
    <div v-else-if="members.length === 0" class="text-center py-12 border-2 border-dashed border-border rounded-xl">
      <Users class="w-10 h-10 text-gray-600 mx-auto mb-3" />
      <p class="text-text-muted font-medium">No members yet</p>
      <p class="text-gray-600 text-sm mt-1">Generate an invite link to add the first member.</p>
    </div>

    <!-- Members Table -->
    <div v-else class="overflow-hidden rounded-xl border border-border">
      <table class="w-full">
        <thead>
          <tr class="bg-bg border-b border-border">
            <th class="px-4 py-3 text-left text-xs font-medium text-text-muted uppercase tracking-wider">Member</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-text-muted uppercase tracking-wider">Role</th>
            <th class="px-4 py-3 text-left text-xs font-medium text-text-muted uppercase tracking-wider hidden sm:table-cell">Joined</th>
            <th class="px-4 py-3 text-right text-xs font-medium text-text-muted uppercase tracking-wider">Actions</th>
          </tr>
        </thead>
        <tbody class="divide-y divide-dark-border">
          <tr v-for="member in members" :key="member.userId" class="hover:bg-bg/50 transition-colors">
            <!-- Avatar + Name -->
            <td class="px-4 py-3">
              <div class="flex items-center gap-3">
                <img 
                  v-if="member.pictureUrl"
                  :src="member.pictureUrl"
                  alt="Member Avatar"
                  class="w-9 h-9 rounded-full object-cover flex-shrink-0"
                />
                <div v-else class="w-9 h-9 rounded-full bg-emerald-500/20 text-emerald-400 flex items-center justify-center font-bold text-sm flex-shrink-0">
                  {{ initials(member) }}
                </div>
                <div>
                  <p class="text-sm font-medium text-text-strong">{{ member.firstName }} {{ member.lastName }}</p>
                  <p class="text-xs text-text-muted flex items-center gap-1">
                    <Mail class="w-3 h-3" />{{ member.email }}
                  </p>
                </div>
              </div>
            </td>
            <!-- Role Badge & Sub-unit info -->
            <td class="px-4 py-3">
              <div class="flex items-center gap-2">
                <span :class="['px-2.5 py-1 rounded-md text-xs font-medium border', roleColor(member.roleName)]">
                  {{ member.roleName }}
                </span>
                <span v-if="member.unitName && member.roleName !== 'Member'" class="text-xs font-medium text-text-muted bg-bg border border-border px-2 py-0.5 rounded-md flex items-center gap-1">
                  {{ member.unitName }}
                </span>
              </div>
            </td>
            <!-- Date -->
            <td class="px-4 py-3 text-xs text-text-muted hidden sm:table-cell">
              <div class="flex items-center gap-1.5">
                <Calendar class="w-3.5 h-3.5" />
                {{ formatDate(member.joinedAt) }}
              </div>
            </td>
            <!-- Assign Role + Remove Buttons -->
            <td class="px-4 py-3 text-right">
              <div class="flex items-center justify-end gap-1">
                <button
                  @click="openAssignRole(member)"
                  class="p-1.5 rounded-lg text-gray-600 hover:text-emerald-400 hover:bg-emerald-500/10 transition-colors"
                  title="Assign Role"
                >
                  <ChevronsUp class="w-4 h-4" />
                </button>
                <button
                  @click="emit('member-removed', member.userId)"
                  class="p-1.5 rounded-lg text-gray-600 hover:text-red-400 hover:bg-red-500/10 transition-colors"
                  title="Remove member"
                >
                  <UserMinus class="w-4 h-4" />
                </button>
              </div>
            </td>
          </tr>
        </tbody>
      </table>
    </div>

    <!-- Assign Role Modal -->
    <div v-if="assignRoleModal.open" class="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div class="absolute inset-0 bg-black/60 backdrop-blur-sm" @click="assignRoleModal.open = false"></div>
      <div class="relative bg-surface border border-border rounded-2xl shadow-2xl w-full max-w-sm p-6 space-y-5">
        <h3 class="text-lg font-bold text-text-strong">Assign Role</h3>
        <p class="text-sm text-text-muted">
          Assigning a new role to <strong class="text-text-strong">{{ assignRoleModal.member?.firstName }} {{ assignRoleModal.member?.lastName }}</strong>.
        </p>
        <div>
          <label class="block text-xs font-medium text-text-muted mb-1.5">Select Role</label>
          <select v-model="selectedRole" class="w-full px-3 py-2.5 bg-bg border border-border rounded-lg text-text-strong text-sm focus:outline-none focus:border-emerald-500 transition-colors">
            <option v-for="role in leadershipRoles" :key="role.value" :value="role.value">{{ role.label }}</option>
          </select>
        </div>

        <!-- Select Target Unit (if options exist) -->
        <div v-if="availableTargetUnits.length > 0" class="mb-4">
          <label class="block text-xs text-text-muted font-medium mb-1.5">Destination Unit *</label>
          <select v-model="selectedTargetUnitId" class="w-full bg-bg border border-border rounded-lg px-3 py-2 text-sm text-text-strong outline-none focus:border-emerald-500 transition-colors">
            <option value="" disabled>Select destination...</option>
            <option v-for="u in availableTargetUnits" :key="u.id" :value="u.id">{{ u.name }}</option>
          </select>
        </div>

        <div class="flex justify-end gap-3 pt-2">
          <button @click="assignRoleModal.open = false" :disabled="isAssigningRole" class="px-4 py-2 rounded-lg text-sm text-text-muted hover:text-text-strong transition-colors disabled:opacity-50">Cancel</button>
          <button @click="confirmAssignRole" :disabled="isAssigningRole || (availableTargetUnits.length > 0 && !selectedTargetUnitId)" class="flex items-center gap-2 px-4 py-2 rounded-lg text-sm font-medium bg-emerald-600 hover:bg-emerald-500 text-white transition-all disabled:opacity-50 disabled:hover:bg-emerald-600">
            <span v-if="isAssigningRole" class="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
            {{ isAssigningRole ? 'Saving...' : 'Save Role' }}
          </button>
        </div>
      </div>
    </div>
  </div>
</template>
