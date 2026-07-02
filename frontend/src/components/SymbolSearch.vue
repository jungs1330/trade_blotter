<script setup lang="ts">
/**
 * Symbol search — one shared filter that narrows BOTH the positions panel and the blotter
 * to matching symbols at once. Binds directly to the store's `symbolQuery` (shared state),
 * so the two tables react together. Matching is case-insensitive substring (see the store).
 */
import { storeToRefs } from 'pinia'
import IconField from 'primevue/iconfield'
import InputIcon from 'primevue/inputicon'
import InputText from 'primevue/inputtext'
import Button from 'primevue/button'
import { useBlotterStore } from '../stores/blotter'

const store = useBlotterStore()
const { symbolQuery } = storeToRefs(store)
</script>

<template>
  <div class="symbol-search">
    <IconField class="symbol-search__field">
      <InputIcon class="pi pi-search" />
      <InputText
        v-model="symbolQuery"
        placeholder="Filter by symbol (e.g. AAPL)"
        aria-label="Filter positions and trades by symbol"
        fluid
      />
    </IconField>
    <Button
      v-if="symbolQuery"
      icon="pi pi-times"
      label="Clear"
      severity="secondary"
      text
      @click="symbolQuery = ''"
    />
  </div>
</template>

<style scoped>
.symbol-search {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.symbol-search__field {
  flex: 1 1 auto;
  max-width: 22rem;
}
</style>
