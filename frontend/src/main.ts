/**
 * Application bootstrap.
 *
 * Wires the three cross-cutting concerns for the SPA:
 *  - Pinia — single source of truth for shared state (trades + positions).
 *  - PrimeVue — component library, configured with the Aura theme preset.
 *  - ToastService — app-wide toast notifications (used to surface server 400s).
 *
 * darkModeSelector is pinned to a class we never add, so the UI stays in light mode
 * deterministically instead of following the OS colour scheme.
 */
import { createApp } from 'vue'
import { createPinia } from 'pinia'
import PrimeVue from 'primevue/config'
import Aura from '@primeuix/themes/aura'
import ToastService from 'primevue/toastservice'

import 'primeicons/primeicons.css'
import './style.css'

import App from './App.vue'

const app = createApp(App)

app.use(createPinia())
app.use(PrimeVue, {
  theme: {
    preset: Aura,
    options: {
      darkModeSelector: '.app-dark',
    },
  },
})
app.use(ToastService)

app.mount('#app')
