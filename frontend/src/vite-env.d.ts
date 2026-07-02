/// <reference types="vite/client" />

// Augment Vite's env typing with the app's own variables so `import.meta.env`
// access is type-checked. Optional — the API client falls back to a default.
interface ImportMetaEnv {
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
