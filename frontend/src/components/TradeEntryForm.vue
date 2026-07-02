<script setup lang="ts">
/**
 * Trade entry form.
 *
 * Single responsibility: collect + validate a new trade and hand it to the store.
 * Validation mirrors the server rules (non-empty symbol, positive whole-number quantity,
 * positive price) so the user gets immediate feedback; the server remains authoritative.
 * On success the store updates the blotter + positions reactively (no reload). Server-side
 * 400s are surfaced via a toast so they are never silently swallowed.
 */
import { computed, reactive, shallowRef, watch } from 'vue'
import InputText from 'primevue/inputtext'
import InputNumber from 'primevue/inputnumber'
import SelectButton from 'primevue/selectbutton'
import Button from 'primevue/button'
import { useToast } from 'primevue/usetoast'
import { ApiError } from '../api/client'
import { useBlotterStore } from '../stores/blotter'
import type { CreateTradeRequest, Side } from '../types'

const store = useBlotterStore()
const toast = useToast()

const sideOptions: Side[] = ['Buy', 'Sell']

// Reactive form model (a "single state object" — the recommended shape for forms).
const form = reactive({
  symbol: '',
  side: 'Buy' as Side,
  quantity: null as number | null,
  price: null as number | null,
})

// Track which fields the user has interacted with, so errors show on blur rather than
// immediately on an untouched form.
const touched = reactive({ symbol: false, quantity: false, price: false })
const submitting = shallowRef(false)

// Keep the symbol uppercased as the user types (the server also uppercases).
watch(
  () => form.symbol,
  (value) => {
    const upper = value.toUpperCase()
    if (upper !== value) form.symbol = upper
  },
)

// Pure validation derived from the current values (mirrors server rules).
const errors = computed(() => ({
  symbol: form.symbol.trim() ? '' : 'Symbol is required.',
  quantity:
    form.quantity == null
      ? 'Quantity is required.'
      : !Number.isInteger(form.quantity)
        ? 'Quantity must be a whole number of shares.'
        : form.quantity <= 0
          ? 'Quantity must be greater than zero.'
          : '',
  price:
    form.price == null
      ? 'Price is required.'
      : form.price <= 0
        ? 'Price must be greater than zero.'
        : '',
}))

const isValid = computed(() => !errors.value.symbol && !errors.value.quantity && !errors.value.price)

// Only show a field's error once it has been touched (or after a submit attempt).
const visibleErrors = computed(() => ({
  symbol: touched.symbol ? errors.value.symbol : '',
  quantity: touched.quantity ? errors.value.quantity : '',
  price: touched.price ? errors.value.price : '',
}))

function resetForm() {
  form.symbol = ''
  form.side = 'Buy'
  form.quantity = null
  form.price = null
  touched.symbol = touched.quantity = touched.price = false
}

async function onSubmit() {
  // Enter-key submits bypass the disabled button, so guard here too.
  touched.symbol = touched.quantity = touched.price = true
  if (!isValid.value || submitting.value) return

  const payload: CreateTradeRequest = {
    symbol: form.symbol.trim().toUpperCase(),
    side: form.side,
    quantity: form.quantity as number,
    price: form.price as number,
  }

  submitting.value = true
  try {
    const created = await store.submitTrade(payload)
    toast.add({
      severity: 'success',
      summary: 'Trade recorded',
      detail: `${created.side} ${created.quantity} ${created.symbol} @ ${created.price}`,
      life: 3000,
    })
    resetForm()
  } catch (error) {
    toast.add({
      severity: 'error',
      summary: 'Could not submit trade',
      detail: error instanceof ApiError ? error.message : 'Failed to submit trade.',
      life: 5000,
    })
  } finally {
    submitting.value = false
  }
}
</script>

<template>
  <section class="card entry">
    <h2 class="card__heading">New trade</h2>

    <form class="entry__form" @submit.prevent="onSubmit">
      <div class="field entry__symbol">
        <label for="symbol" class="field__label">Symbol</label>
        <InputText
          id="symbol"
          v-model="form.symbol"
          placeholder="e.g. AAPL"
          autocomplete="off"
          fluid
          :invalid="!!visibleErrors.symbol"
          @blur="touched.symbol = true"
        />
        <small v-if="visibleErrors.symbol" class="field__error">{{ visibleErrors.symbol }}</small>
      </div>

      <div class="field">
        <span class="field__label">Side</span>
        <SelectButton v-model="form.side" :options="sideOptions" :allow-empty="false" aria-label="Side" />
      </div>

      <div class="field">
        <label for="quantity" class="field__label">Quantity</label>
        <InputNumber
          input-id="quantity"
          v-model="form.quantity"
          :min="1"
          :max-fraction-digits="0"
          placeholder="Shares"
          fluid
          :invalid="!!visibleErrors.quantity"
          @blur="touched.quantity = true"
        />
        <small v-if="visibleErrors.quantity" class="field__error">{{ visibleErrors.quantity }}</small>
      </div>

      <div class="field">
        <label for="price" class="field__label">Price</label>
        <InputNumber
          input-id="price"
          v-model="form.price"
          mode="currency"
          currency="USD"
          :min="0"
          :min-fraction-digits="2"
          :max-fraction-digits="2"
          placeholder="Per share"
          fluid
          :invalid="!!visibleErrors.price"
          @blur="touched.price = true"
        />
        <small v-if="visibleErrors.price" class="field__error">{{ visibleErrors.price }}</small>
      </div>

      <div class="field entry__actions">
        <Button type="submit" label="Add trade" icon="pi pi-plus" :loading="submitting" :disabled="!isValid" />
      </div>
    </form>
  </section>
</template>

<style scoped>
.entry__form {
  display: flex;
  flex-wrap: wrap;
  gap: 1rem;
  align-items: flex-start;
}

.field {
  display: flex;
  flex-direction: column;
  gap: 0.35rem;
  min-width: 9rem;
}

.entry__symbol {
  flex: 1 1 10rem;
}

.field__label {
  font-size: 0.8rem;
  font-weight: 600;
  color: #475569;
}

.field__error {
  color: #dc2626;
  font-size: 0.75rem;
}

/* Align the submit button with the inputs (accounting for the label row above them). */
.entry__actions {
  align-self: stretch;
  justify-content: flex-end;
  padding-top: 1.4rem;
}
</style>
