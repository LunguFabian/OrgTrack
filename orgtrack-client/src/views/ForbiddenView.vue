<script setup lang="ts">
import { useRouter } from 'vue-router';
import { Home, ShieldAlert } from 'lucide-vue-next';

const router = useRouter();
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-bg text-text-strong px-4">
    <div class="text-center flex flex-col items-center">
      <div class="w-24 h-24 bg-red-500/10 rounded-full flex items-center justify-center mb-6">
        <ShieldAlert class="w-12 h-12 text-red-500" />
      </div>
      <h1 class="text-9xl font-black text-red-500/10 absolute -z-10 pointer-events-none select-none">403</h1>
      <h2 class="text-3xl font-bold mb-4">Access Denied</h2>
      <p class="text-text-muted mb-8 max-w-md mx-auto leading-relaxed">
        You don't have the required permissions to access this page. 
        If you think this is a mistake, please contact your Local President.
      </p>
      
      <button 
        @click="router.push('/')" 
        class="inline-flex items-center gap-2 px-6 py-3 bg-emerald-600 hover:bg-emerald-500 text-white rounded-xl transition-all shadow-lg shadow-emerald-500/20 font-medium"
      >
        <Home class="w-5 h-5" />
        Return to Dashboard
      </button>
    </div>
  </div>
</template>
