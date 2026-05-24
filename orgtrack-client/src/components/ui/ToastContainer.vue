<script setup lang="ts">
import { useToastStore } from '../../stores/toastStore';
import { AlertCircle, CheckCircle2, Info, X } from 'lucide-vue-next';

const toastStore = useToastStore();

const icons = {
  success: CheckCircle2,
  error: AlertCircle,
  info: Info
};

const colors = {
  success: 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400',
  error: 'bg-red-500/10 border-red-500/20 text-red-400',
  info: 'bg-blue-500/10 border-blue-500/20 text-blue-400'
};
</script>

<template>
  <div class="fixed top-4 right-4 z-[100] flex flex-col gap-2 max-w-sm w-full pointer-events-none">
    <TransitionGroup 
      enter-active-class="transition duration-300 ease-out"
      enter-from-class="transform translate-x-8 opacity-0"
      enter-to-class="transform translate-x-0 opacity-100"
      leave-active-class="transition duration-200 ease-in"
      leave-from-class="transform translate-x-0 opacity-100"
      leave-to-class="transform translate-x-8 opacity-0"
    >
      <div 
        v-for="toast in toastStore.toasts" 
        :key="toast.id"
        class="pointer-events-auto flex items-start gap-3 p-4 rounded-xl border shadow-xl backdrop-blur-md"
        :class="colors[toast.type]"
      >
        <component :is="icons[toast.type]" class="w-5 h-5 flex-shrink-0 mt-0.5" />
        <p class="flex-1 text-sm font-medium leading-relaxed">{{ toast.message }}</p>
        <button @click="toastStore.removeToast(toast.id)" class="opacity-70 hover:opacity-100 transition-opacity flex-shrink-0">
          <X class="w-4 h-4" />
        </button>
      </div>
    </TransitionGroup>
  </div>
</template>
