<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useAuthStore } from '../stores/authStore';
import { LogIn, Code2 } from 'lucide-vue-next';

const authStore = useAuthStore();
const error = ref('');
const googleLoading = ref(false);

const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID || '';

const handleGoogleCredentialResponse = async (response: any) => {
  googleLoading.value = true;
  error.value = '';
  try {
    await authStore.loginWithGoogle(response.credential);
    window.location.href = '/';
  } catch (err: any) {
    error.value = err.response?.data?.error || 'Google sign-in failed. Please try again.';
  } finally {
    googleLoading.value = false;
  }
};

const initGoogleSignIn = () => {
  if (!GOOGLE_CLIENT_ID) return;
  
  const script = document.createElement('script');
  script.src = 'https://accounts.google.com/gsi/client';
  script.async = true;
  script.defer = true;
  script.onload = () => {
    (window as any).google.accounts.id.initialize({
      client_id: GOOGLE_CLIENT_ID,
      callback: handleGoogleCredentialResponse,
    });
    (window as any).google.accounts.id.renderButton(
      document.getElementById('google-signin-btn'),
      { theme: 'outline', size: 'large', width: 400, text: 'signin_with', shape: 'rectangular' }
    );
  };
  document.head.appendChild(script);
};

onMounted(() => {
  initGoogleSignIn();
});
</script>

<template>
  <div class="flex flex-col items-center justify-center min-h-screen">
    <div class="w-full max-w-md p-10 bg-surface border border-border rounded-2xl shadow-2xl">
      <!-- Logo & Title -->
      <div class="text-center mb-10">
        <div class="w-16 h-16 bg-emerald-500/10 border border-emerald-500/20 rounded-2xl flex items-center justify-center mx-auto mb-5">
          <LogIn class="w-8 h-8 text-emerald-400" />
        </div>
        <h1 class="text-3xl font-bold text-text-strong mb-2">OrgTrack</h1>
        <p class="text-text-muted">Sign in to your organization</p>
      </div>

      <!-- Google Sign-In -->
      <div v-if="GOOGLE_CLIENT_ID" class="flex justify-center">
        <div id="google-signin-btn"></div>
      </div>

      <!-- Fallback when no Google Client ID configured -->
      <div v-else class="text-center space-y-4">
        <div class="p-4 bg-amber-500/10 border border-amber-500/20 rounded-xl">
          <p class="text-sm text-amber-400 font-medium">Google Sign-In not configured</p>
          <p class="text-xs text-text-muted mt-1">Set <code class="px-1 py-0.5 bg-bg rounded text-emerald-400">VITE_GOOGLE_CLIENT_ID</code> in your .env file</p>
        </div>
      </div>

      <!-- Error Message -->
      <div v-if="error" class="mt-4 p-3 bg-red-500/10 border border-red-500/20 rounded-lg text-red-400 text-sm text-center">
        {{ error }}
      </div>

      <!-- Loading overlay -->
      <div v-if="googleLoading" class="mt-4 text-center">
        <p class="text-sm text-text-muted animate-pulse">Signing you in...</p>
      </div>

      <!-- Developer Login link -->
      <div class="mt-8 pt-6 border-t border-border text-center">
        <router-link 
          to="/developer-login"
          class="inline-flex items-center gap-2 text-sm text-text-muted hover:text-emerald-400 transition-colors"
        >
          <Code2 class="w-4 h-4" />
          Developer Login
        </router-link>
      </div>
    </div>
  </div>
</template>
