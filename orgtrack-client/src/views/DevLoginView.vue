<script setup lang="ts">
import { ref } from 'vue';
import { useAuthStore } from '../stores/authStore';
import { Code2, ArrowLeft, Terminal } from 'lucide-vue-next';

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
    window.location.href = '/';
  } catch (err: any) {
    error.value = err.response?.data?.error || 'Login failed';
  } finally {
    loading.value = false;
  }
};
</script>

<template>
  <div class="flex flex-col items-center justify-center min-h-screen">
    <div class="w-full max-w-md p-10 bg-surface border border-border rounded-2xl shadow-2xl">
      <!-- Header -->
      <div class="text-center mb-8">
        <div class="w-16 h-16 bg-purple-500/10 border border-purple-500/20 rounded-2xl flex items-center justify-center mx-auto mb-5">
          <Terminal class="w-8 h-8 text-purple-400" />
        </div>
        <h1 class="text-2xl font-bold text-text-strong mb-2">Developer Login</h1>
        <p class="text-text-muted text-sm">Quick access for development & testing</p>
      </div>

      <!-- Warning Banner -->
      <div class="p-3 bg-amber-500/10 border border-amber-500/20 rounded-xl mb-6">
        <div class="flex items-center gap-2">
          <Code2 class="w-4 h-4 text-amber-400 flex-shrink-0" />
          <p class="text-xs text-amber-400">This login method is only available in development mode.</p>
        </div>
      </div>

      <!-- Dev Login Form -->
      <form @submit.prevent="handleDevLogin" class="space-y-4">
        <div>
          <label class="block text-sm font-medium text-text-muted mb-1">Email address</label>
          <input 
            v-model="email"
            type="email" 
            placeholder="admin@aiesec.ro"
            class="w-full px-4 py-3 bg-bg border border-border rounded-lg text-text-strong focus:outline-none focus:ring-2 focus:ring-purple-500 focus:border-transparent transition-all"
          />
        </div>

        <div v-if="error" class="p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm">
          {{ error }}
        </div>

        <button 
          type="submit"
          :disabled="loading || !email"
          class="w-full px-4 py-3 bg-purple-600 hover:bg-purple-500 disabled:opacity-50 disabled:cursor-not-allowed text-white font-semibold rounded-lg shadow-lg shadow-purple-500/20 transition-colors"
        >
          <span v-if="loading">Signing in...</span>
          <span v-else>Sign In as Developer</span>
        </button>
      </form>

      <!-- Back to regular login -->
      <div class="mt-6 pt-6 border-t border-border text-center">
        <router-link 
          to="/login"
          class="inline-flex items-center gap-2 text-sm text-text-muted hover:text-emerald-400 transition-colors"
        >
          <ArrowLeft class="w-4 h-4" />
          Back to Sign In
        </router-link>
      </div>
    </div>
  </div>
</template>
