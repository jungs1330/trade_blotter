<script setup lang="ts">
/**
 * Root layout / composition surface.
 *
 * Intentionally thin: it lays out the three feature sections (entry form, positions,
 * blotter) and mounts the global <Toast/> outlet. Feature logic and shared state live in
 * the Pinia store and the child components (wired in subsequent steps).
 */
import Toast from 'primevue/toast'
</script>

<template>
  <div class="app">
    <header class="app__header">
      <h1 class="app__title">Trade Blotter</h1>
      <p class="app__subtitle">
        Enter trades and watch the blotter and derived positions update live.
      </p>
    </header>

    <main class="app__layout">
      <!-- TradeEntryForm, PositionsPanel, and BlotterTable are added in later steps. -->
    </main>

    <Toast />
  </div>
</template>

<style scoped>
.app__header {
  margin-bottom: 1.5rem;
}

.app__title {
  margin: 0;
  font-size: 1.75rem;
  font-weight: 600;
  color: #0f172a;
}

.app__subtitle {
  margin: 0.25rem 0 0;
  color: #64748b;
}

.app__layout {
  display: grid;
  gap: 1.5rem;
}
</style>
