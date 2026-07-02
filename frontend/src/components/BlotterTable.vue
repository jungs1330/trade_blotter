<script setup lang="ts">
/**
 * Blotter table — read-only view of the full trade ledger, newest first.
 *
 * Binds to the store's `sortedTrades` getter (single source of truth) and lets PrimeVue's
 * DataTable handle client-side sorting. Buy/Sell is rendered as a colored Tag and numeric
 * columns are right-aligned with tabular numerals for scannability.
 */
import { storeToRefs } from 'pinia'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import { useBlotterStore } from '../stores/blotter'
import { formatCurrency, formatDateTime, formatPrice, formatQuantity } from '../utils/format'

const store = useBlotterStore()
const { sortedTrades, loading } = storeToRefs(store)
</script>

<template>
  <section class="card">
    <h2 class="card__heading">Blotter</h2>

    <DataTable
      :value="sortedTrades"
      :loading="loading"
      data-key="id"
      sort-field="timestamp"
      :sort-order="-1"
      removable-sort
      paginator
      :rows="10"
      :rows-per-page-options="[10, 20, 50]"
      size="small"
      striped-rows
    >
      <template #empty>No trades yet — submit one above.</template>

      <Column field="timestamp" header="Time" sortable>
        <template #body="{ data }">{{ formatDateTime(data.timestamp) }}</template>
      </Column>

      <Column field="symbol" header="Symbol" sortable />

      <Column field="side" header="Side" sortable>
        <template #body="{ data }">
          <Tag :value="data.side" :severity="data.side === 'Buy' ? 'success' : 'danger'" />
        </template>
      </Column>

      <Column field="quantity" header="Qty" sortable header-class="num-cell" body-class="num-cell">
        <template #body="{ data }">{{ formatQuantity(data.quantity) }}</template>
      </Column>

      <Column field="price" header="Price" sortable header-class="num-cell" body-class="num-cell">
        <template #body="{ data }">{{ formatPrice(data.price) }}</template>
      </Column>

      <Column field="notional" header="Notional" sortable header-class="num-cell" body-class="num-cell">
        <template #body="{ data }">{{ formatCurrency(data.notional) }}</template>
      </Column>
    </DataTable>
  </section>
</template>
