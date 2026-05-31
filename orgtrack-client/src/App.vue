<script setup lang="ts">
import ToastContainer from './components/ui/ToastContainer.vue';
import ErrorBoundary from './components/common/ErrorBoundary.vue';
</script>

<template>
  <ToastContainer />
  <ErrorBoundary>
    <router-view />
  </ErrorBoundary>
</template>

<style scoped>
</style>
