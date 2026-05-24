<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import { useAuthStore } from '../stores/authStore';

const router = useRouter();
const authStore = useAuthStore();
const email = ref('');
const error = ref('');
const loading = ref(false);

const handleDevLogin = async () => {
  if (!email.value) return;
  
  error.value = '';
  loading.value = true;
  
  try {
    await authStore.devLogin(email.value);
    router.push('/');
  } catch (err: any) {
    error.value = err.response?.data?.error || 'Login failed';
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <div class="flex flex-col items-center justify-center min-h-screen">
    <!-- Main component -->
    <div class="w-full max-w-md p-10 bg-dark-surface border border-dark-border rounded-2xl shadow-2xl">
      <div class="text-center mb-10">
        <h1 class="text-3xl font-bold text-white mb-2">OrgTrack</h1>
        <p class="text-gray-400">Sign in to your organization</p>
      </div>

      <!-- Google Login Button Placeholder -->
      <button class="w-full flex items-center justify-center gap-3 px-4 py-3 mb-6 bg-white text-gray-800 font-semibold rounded-lg hover:bg-gray-100 transition-colors">
        <img src="https://www.svgrepo.com/show/475656/google-color.svg" alt="Google" class="w-5 h-5" />
        Sign in with Google
      </button>

      <div class="flex items-center my-6">
        <div class="flex-1 border-t border-dark-border"></div>
        <span class="px-4 text-sm text-gray-500">OR (DEV ONLY)</span>
        <div class="flex-1 border-t border-dark-border"></div>
      </div>

      <!-- Dev Login Form -->
      <form @submit.prevent="handleDevLogin" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-gray-300 mb-1">Email address</label>
          <input 
            v-model="email"
            type="email" 
            placeholder="admin@aiesec.ro"
            class="w-full px-4 py-3 bg-dark-bg border border-dark-border rounded-lg text-white focus:outline-none focus:ring-2 focus:ring-emerald-500 focus:border-transparent transition-all"
          />
        </div>

        <div v-if="error" class="text-red-400 text-sm">
          {{ error }}
        </div>

        <button 
          type="submit"
          :disabled="loading || !email"
          class="w-full px-4 py-3 bg-emerald-600 hover:bg-emerald-500 disabled:opacity-50 disabled:cursor-not-allowed text-white font-semibold rounded-lg shadow-lg shadow-emerald-500/20 transition-colors"
        >
          <span v-if="loading">Signing in...</span>
          <span v-else>Developer Login</span>
        </button>
      </form>
    </div>
  </div>
</template>
