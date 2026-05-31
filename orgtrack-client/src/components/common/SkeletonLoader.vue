<script setup lang="ts">
import { computed } from 'vue';

const props = defineProps<{
  type?: 'text' | 'circular' | 'rectangular';
  width?: string;
  height?: string;
  class?: string;
}>();

const baseClasses = 'animate-pulse bg-surface-hover';

const computedClasses = computed(() => {
  const classes = [baseClasses];
  
  if (props.type === 'circular') {
    classes.push('rounded-full');
  } else if (props.type === 'rectangular') {
    classes.push('rounded-lg');
  } else {
    classes.push('rounded'); // default text type
  }

  if (props.class) {
    classes.push(props.class);
  }

  return classes.join(' ');
});

const computedStyle = computed(() => {
  return {
    width: props.width || (props.type === 'circular' ? '40px' : '100%'),
    height: props.height || (props.type === 'circular' ? '40px' : props.type === 'text' ? '1rem' : '100px')
  };
});
</script>

<template>
  <div :class="computedClasses" :style="computedStyle"></div>
</template>

<style scoped>
/* Optional: slightly adjust the pulse animation if default Tailwind is too fast/slow */
.animate-pulse {
  animation: pulse 2s cubic-bezier(0.4, 0, 0.6, 1) infinite;
}

@keyframes pulse {
  0%, 100% {
    opacity: 1;
  }
  50% {
    opacity: .5;
  }
}
</style>
