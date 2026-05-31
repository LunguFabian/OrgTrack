<script setup lang="ts">
import { ref, onErrorCaptured } from 'vue';
import { AlertTriangle, RefreshCcw } from 'lucide-vue-next';

const hasError = ref(false);
const error = ref<Error | null>(null);

onErrorCaptured((err: Error) => {
  hasError.value = true;
  error.value = err;
  console.error('Captured by ErrorBoundary:', err);
  // Prevent error from propagating to the global handler
  return false;
});

const reloadPage = () => {
  window.location.reload();
};
</script>

<template>
  <div v-if="hasError" class="min-h-[400px] flex items-center justify-center p-6 bg-bg">
    <div class="max-w-md w-full bg-surface border border-red-500/20 rounded-2xl p-8 text-center shadow-2xl shadow-red-500/10">
      <div class="w-16 h-16 bg-red-500/10 rounded-full flex items-center justify-center mx-auto mb-6">
        <AlertTriangle class="w-8 h-8 text-red-500" />
      </div>
      
      <h2 class="text-xl font-bold text-text-strong mb-2">Something went wrong</h2>
      <p class="text-sm text-text-muted mb-6">
        We've encountered an unexpected error. Please try refreshing the page.
      </p>
      
      <div class="bg-bg border border-border rounded-lg p-4 mb-6 text-left overflow-x-auto text-xs font-mono text-gray-500">
        {{ error?.message || 'Unknown error' }}
      </div>
      
      <button 
        @click="reloadPage" 
        class="flex items-center justify-center gap-2 w-full py-3 bg-red-500 hover:bg-red-600 text-white font-medium rounded-xl transition-colors shadow-lg shadow-red-500/20"
      >
        <RefreshCcw class="w-4 h-4" />
        Refresh Page
      </button>
    </div>
  </div>
  <slot v-else></slot>
</template>
