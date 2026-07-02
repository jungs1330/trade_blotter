<script setup lang="ts">
/**
 * Positions panel — derived net position + moving-average cost per symbol.
 *
 * Purely reactive: it binds to the store's `positions`, which `submitTrade` refreshes
 * after every successful trade, so this updates with no reload. Short positions (negative
 * net qty) are flagged with a distinct Tag and a red quantity. No market value / P&L —
 * there is no market data (see project scope).
 */
import { storeToRefs } from 'pinia'
import DataTable from 'primevue/datatable'
import Column from 'primevue/column'
import Tag from 'primevue/tag'
import { useBlotterStore } from '../stores/blotter'
import { formatCurrency, formatQuantity } from '../utils/format'

const store = useBlotterStore()
const { positions, loading } = storeToRefs(store)
</script>

<template>
  <section class="card">
    <h2 class="card__heading">Positions</h2>

    <DataTable
      :value="positions"
      :loading="loading"
      data-key="symbol"
      sort-field="symbol"
      :sort-order="1"
      removable-sort
      size="small"
      striped-rows
    >
      <template #empty>No open positions.</template>

      <Column field="symbol" header="Symbol" sortable />

      <Column field="netQuantity" header="Net qty" sortable header-class="num-cell" body-class="num-cell">
        <template #body="{ data }">
          <span :class="{ positions__short: data.netQuantity < 0 }">
            {{ formatQuantity(data.netQuantity) }}
          </span>
        </template>
      </Column>

      <Column header="Side">
        <template #body="{ data }">
          <Tag
            :value="data.netQuantity < 0 ? 'Short' : 'Long'"
            :severity="data.netQuantity < 0 ? 'danger' : 'success'"
          />
        </template>
      </Column>

      <Column field="averageCost" header="Avg cost" sortable header-class="num-cell" body-class="num-cell">
        <template #body="{ data }">{{ formatCurrency(data.averageCost) }}</template>
      </Column>

      <!-- Cost basis = net qty × avg cost (negative for shorts). Optional per spec. -->
      <Column header="Cost basis" header-class="num-cell" body-class="num-cell">
        <template #body="{ data }">{{ formatCurrency(data.netQuantity * data.averageCost) }}</template>
      </Column>
    </DataTable>
  </section>
</template>

<style scoped>
.positions__short {
  color: #dc2626;
  font-weight: 600;
}
</style>
