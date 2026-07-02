/**
 * Pinia store — the single source of truth for all shared blotter state.
 *
 * Holds the trade ledger and derived positions plus loading/error flags. Components
 * read from here (via storeToRefs) and never fetch on their own, so the blotter and
 * positions stay consistent. Setup-store style: every ref/computed/action is returned
 * (required for DevTools/SSR visibility).
 */
import { computed, ref, shallowRef } from 'vue'
import { defineStore } from 'pinia'
import { ApiError, api } from '../api/client'
import type { CreateTradeRequest, Position, Trade } from '../types'

export const useBlotterStore = defineStore('blotter', () => {
  // --- state ---
  const trades = ref<Trade[]>([])
  const positions = ref<Position[]>([])
  const loading = shallowRef(false)
  const error = shallowRef<string | null>(null)
  // Shared symbol filter applied to both the blotter and the positions panel.
  const symbolQuery = shallowRef('')

  // --- getters ---
  // The API already returns newest-first, but this derives a stable, explicitly sorted
  // view (newest timestamp first) that the blotter table binds to as its default order.
  const sortedTrades = computed(() =>
    [...trades.value].sort((a, b) => b.timestamp.localeCompare(a.timestamp)),
  )

  // Case-insensitive symbol filter. An empty query shows everything; symbols are stored
  // uppercase, so the query is uppercased and matched as a substring (typing "aa" finds AAPL).
  const normalizedQuery = computed(() => symbolQuery.value.trim().toUpperCase())

  const filteredTrades = computed(() =>
    normalizedQuery.value
      ? sortedTrades.value.filter((trade) => trade.symbol.includes(normalizedQuery.value))
      : sortedTrades.value,
  )

  const filteredPositions = computed(() =>
    normalizedQuery.value
      ? positions.value.filter((position) => position.symbol.includes(normalizedQuery.value))
      : positions.value,
  )

  // --- actions ---
  async function fetchTrades(): Promise<void> {
    loading.value = true
    error.value = null
    try {
      trades.value = await api.getTrades()
    } catch (e) {
      error.value = messageOf(e)
      throw e
    } finally {
      loading.value = false
    }
  }

  async function fetchPositions(): Promise<void> {
    try {
      positions.value = await api.getPositions()
    } catch (e) {
      error.value = messageOf(e)
      throw e
    }
  }

  /**
   * Submit a new trade. On success the persisted trade (with server id/timestamp/notional)
   * is prepended to the ledger and positions are refreshed — so both the blotter and the
   * positions panel update reactively with no page reload.
   *
   * Re-throws {@link ApiError} so the caller (entry form) can surface validation messages
   * via a toast; the store's own `error` flag is reserved for background fetch failures.
   */
  async function submitTrade(payload: CreateTradeRequest): Promise<Trade> {
    const created = await api.createTrade(payload)
    trades.value = [created, ...trades.value]
    await fetchPositions()
    return created
  }

  return {
    // state
    trades,
    positions,
    loading,
    error,
    symbolQuery,
    // getters
    sortedTrades,
    filteredTrades,
    filteredPositions,
    // actions
    fetchTrades,
    fetchPositions,
    submitTrade,
  }
})

/** Extract a user-facing message from a thrown value. */
function messageOf(e: unknown): string {
  return e instanceof ApiError ? e.message : 'An unexpected error occurred.'
}
