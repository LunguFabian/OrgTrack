<script setup lang="ts">
import KanbanBoard from '../components/unit/KanbanBoard.vue';
</script>

<template>
  <div class="max-w-7xl mx-auto py-8 px-4 sm:px-6 lg:px-8 min-h-screen">
    <!-- Header -->
    <div class="mb-8">
      <h1 class="text-3xl font-bold text-text-strong mb-2">My Tasks</h1>
      <p class="text-text-muted">Manage all your assignments across all committees and departments.</p>
    </div>

    <!-- Kanban Board in "me" mode -->
    <div class="bg-surface border border-border rounded-2xl p-6 shadow-xl">
      <KanbanBoard mode="me" />
    </div>
  </div>
</template>
