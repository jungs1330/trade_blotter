/**
 * Pure display formatters (kept as plain utilities, not composables).
 *
 * Formatter instances are created once at module scope — Intl formatters are relatively
 * expensive to construct, and reusing them avoids rebuilding one per cell render.
 */

const currencyFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

// Prices can carry up to 4 fractional digits without being rounded away in display.
const priceFormatter = new Intl.NumberFormat('en-US', {
  style: 'currency',
  currency: 'USD',
  minimumFractionDigits: 2,
  maximumFractionDigits: 4,
})

const quantityFormatter = new Intl.NumberFormat('en-US')

const dateTimeFormatter = new Intl.DateTimeFormat(undefined, {
  dateStyle: 'medium',
  timeStyle: 'medium',
})

/** Currency with exactly 2dp (for notional and cost-basis). */
export function formatCurrency(value: number): string {
  return currencyFormatter.format(value)
}

/** Currency for per-share prices (2–4dp so exact prices aren't rounded in display). */
export function formatPrice(value: number): string {
  return priceFormatter.format(value)
}

/** Whole-share quantity with thousands grouping; preserves the sign for shorts. */
export function formatQuantity(value: number): string {
  return quantityFormatter.format(value)
}

/**
 * Format a server ISO-8601 UTC timestamp into the viewer's local date + time.
 * Falls back to the raw string if it can't be parsed.
 */
export function formatDateTime(isoUtc: string): string {
  const date = new Date(isoUtc)
  return Number.isNaN(date.getTime()) ? isoUtc : dateTimeFormatter.format(date)
}
