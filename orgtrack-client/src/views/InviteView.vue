<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { inviteService } from '../api/services/invite.service';
import { useAuthStore } from '../stores/authStore';
import { Users, Clock, CheckCircle, XCircle, LogIn, ArrowRight } from 'lucide-vue-next';

const route = useRoute();
const router = useRouter();
const authStore = useAuthStore();

const token = route.params.token as string;

interface InvitePreview {
  organizationUnitName: string;
  roleName: string;
  createdByUserName: string;
  expiresAt: string;
  isExpired: boolean;
  isMaxUsesReached: boolean;
}

const preview = ref<InvitePreview | null>(null);
const isLoading = ref(true);
const error = ref('');
const isJoining = ref(false);
const joinSuccess = ref(false);

onMounted(async () => {
  try {
    preview.value = await inviteService.getInviteDetails(token);
  } catch {
    error.value = 'This invite link is invalid or has expired.';
  } finally {
    isLoading.value = false;
  }
});

const handleJoin = async () => {
  if (!authStore.isAuthenticated) {
    sessionStorage.setItem('pendingInviteToken', token);
    router.push('/login');
    return;
  }

  isJoining.value = true;
  try {
    await inviteService.joinViaLink(token);
    joinSuccess.value = true;
  } catch (err: any) {
    error.value = err.response?.data?.error || 'Failed to join. You may already be a member.';
  } finally {
    isJoining.value = false;
  }
};

const formatDate = (iso: string) =>
  new Date(iso).toLocaleDateString('en-GB', { day: 'numeric', month: 'long', year: 'numeric', hour: '2-digit', minute: '2-digit' });
</script>

<template>
  <div class="min-h-screen bg-dark-bg flex items-center justify-center p-4">
    <div class="w-full max-w-md">

      <!-- Logo / Brand -->
      <div class="text-center mb-8">
        <h1 class="text-2xl font-bold text-white tracking-tight">OrgTrack</h1>
        <p class="text-gray-500 text-sm mt-1">Organization Management Platform</p>
      </div>

      <!-- Loading -->
      <div v-if="isLoading" class="bg-dark-surface border border-dark-border rounded-2xl p-8 text-center">
        <div class="w-10 h-10 border-4 border-emerald-500 border-t-transparent rounded-full animate-spin mx-auto mb-4"></div>
        <p class="text-gray-400">Loading invite details...</p>
      </div>

      <!-- Invalid / Error -->
      <div v-else-if="error && !preview" class="bg-dark-surface border border-red-500/20 rounded-2xl p-8 text-center">
        <XCircle class="w-12 h-12 text-red-400 mx-auto mb-4" />
        <h2 class="text-white font-bold text-xl mb-2">Invalid Invite</h2>
        <p class="text-gray-400 text-sm">{{ error }}</p>
      </div>

      <!-- Success State -->
      <div v-else-if="joinSuccess" class="bg-dark-surface border border-emerald-500/20 rounded-2xl p-8 text-center">
        <CheckCircle class="w-12 h-12 text-emerald-400 mx-auto mb-4" />
        <h2 class="text-white font-bold text-xl mb-2">You're in! 🎉</h2>
        <p class="text-gray-400 text-sm mb-6">
          You've successfully joined <strong class="text-white">{{ preview?.organizationUnitName }}</strong>.
        </p>
        <button
          @click="router.push('/')"
          class="flex items-center gap-2 px-5 py-2.5 bg-emerald-600 hover:bg-emerald-500 text-white font-medium rounded-lg transition-all mx-auto"
        >
          Go to Dashboard
          <ArrowRight class="w-4 h-4" />
        </button>
      </div>

      <!-- Invite Card -->
      <div v-else-if="preview" class="bg-dark-surface border border-dark-border rounded-2xl overflow-hidden shadow-2xl">
        <!-- Header -->
        <div class="bg-emerald-500/10 border-b border-emerald-500/20 px-6 py-5">
          <div class="flex items-center gap-3">
            <div class="p-2.5 bg-emerald-500/20 rounded-xl">
              <Users class="w-6 h-6 text-emerald-400" />
            </div>
            <div>
              <p class="text-xs text-emerald-400 font-medium uppercase tracking-wider">You've been invited to join</p>
              <h2 class="text-xl font-bold text-white">{{ preview.organizationUnitName }}</h2>
            </div>
          </div>
        </div>

        <!-- Details -->
        <div class="px-6 py-5 space-y-4">
          <!-- Expired / Max Uses Warning -->
          <div v-if="preview.isExpired || preview.isMaxUsesReached"
            class="flex items-center gap-2 p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm">
            <XCircle class="w-4 h-4 flex-shrink-0" />
            <span>{{ preview.isExpired ? 'This invite link has expired.' : 'This invite link has reached its maximum uses.' }}</span>
          </div>

          <div class="grid grid-cols-2 gap-4">
            <div class="bg-dark-bg rounded-xl p-3">
              <p class="text-xs text-gray-500 mb-1">Role you'll receive</p>
              <p class="text-white font-semibold text-sm">{{ preview.roleName }}</p>
            </div>
            <div class="bg-dark-bg rounded-xl p-3">
              <p class="text-xs text-gray-500 mb-1">Invited by</p>
              <p class="text-white font-semibold text-sm">{{ preview.createdByUserName || 'A team admin' }}</p>
            </div>
          </div>

          <div class="flex items-center gap-2 text-xs text-gray-500">
            <Clock class="w-3.5 h-3.5" />
            Expires: {{ formatDate(preview.expiresAt) }}
          </div>

          <div v-if="error" class="p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm">
            {{ error }}
          </div>
        </div>

        <!-- Action Footer -->
        <div class="px-6 pb-6">
          <button
            v-if="!preview.isExpired && !preview.isMaxUsesReached"
            @click="handleJoin"
            :disabled="isJoining"
            class="w-full flex items-center justify-center gap-2 py-3 rounded-xl font-semibold text-white bg-emerald-600 hover:bg-emerald-500 transition-all shadow-lg shadow-emerald-500/20 disabled:opacity-50"
          >
            <span v-if="isJoining" class="w-5 h-5 border-2 border-white/30 border-t-white rounded-full animate-spin"></span>
            <LogIn v-else-if="!authStore.isAuthenticated" class="w-5 h-5" />
            <CheckCircle v-else class="w-5 h-5" />
            {{ isJoining ? 'Joining...' : authStore.isAuthenticated ? 'Accept Invitation' : 'Sign in to Accept' }}
          </button>

          <p v-if="!authStore.isAuthenticated && !preview.isExpired" class="text-center text-xs text-gray-500 mt-3">
            You'll be redirected to sign in, then automatically returned here.
          </p>
        </div>
      </div>

    </div>
  </div>
</template>
