<script setup lang="ts">
import { useRouter } from 'vue-router';
import { Home } from 'lucide-vue-next';

const router = useRouter();
</script>

<template>
  <div class="min-h-screen flex items-center justify-center bg-dark-bg text-white px-4">
    <div class="text-center">
      <h1 class="text-9xl font-black text-emerald-500/20 mb-4">404</h1>
      <h2 class="text-3xl font-bold mb-4">Page Not Found</h2>
      <p class="text-gray-400 mb-8 max-w-md mx-auto">
        Oops! The page you are looking for doesn't exist or has been moved.
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
