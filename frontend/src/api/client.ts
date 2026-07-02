/**
 * Thin fetch wrapper around the Trade Blotter API.
 *
 * Responsibilities:
 *  - Resolve the API base URL from VITE_API_BASE_URL (default http://localhost:5099).
 *  - Parse the server's error envelope on non-2xx responses and throw a typed
 *    {@link ApiError} so callers (the store / form) can surface `message` + `field`.
 *  - Translate network/CORS failures into an ApiError too, so nothing throws a raw
 *    TypeError at the UI layer.
 */
import type {
  ApiErrorDetail,
  ApiErrorEnvelope,
  CreateTradeRequest,
  Position,
  Trade,
} from '../types'

const BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:5099').replace(/\/$/, '')

/** Error carrying the API's structured detail (code/message/field) plus HTTP status. */
export class ApiError extends Error {
  readonly code: string
  readonly field: string | null
  readonly status: number

  constructor(status: number, detail: ApiErrorDetail) {
    super(detail.message)
    this.name = 'ApiError'
    this.status = status
    this.code = detail.code
    this.field = detail.field ?? null
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  let response: Response
  try {
    response = await fetch(`${BASE_URL}${path}`, {
      headers: { 'Content-Type': 'application/json' },
      ...init,
    })
  } catch {
    // fetch only rejects on network-level failures (server down, CORS, DNS).
    throw new ApiError(0, {
      code: 'NETWORK_ERROR',
      message: 'Could not reach the trade blotter API. Is the backend running?',
    })
  }

  if (!response.ok) {
    // Prefer the server's error envelope; fall back to a generic message if the body
    // isn't the expected shape (or isn't JSON at all).
    let detail: ApiErrorDetail = {
      code: 'ERROR',
      message: `Request failed with status ${response.status}.`,
    }
    try {
      const body = (await response.json()) as ApiErrorEnvelope
      if (body?.error?.message) detail = body.error
    } catch {
      /* keep the fallback */
    }
    throw new ApiError(response.status, detail)
  }

  return (await response.json()) as T
}

export const api = {
  getTrades: () => request<Trade[]>('/trades'),
  getPositions: () => request<Position[]>('/positions'),
  createTrade: (payload: CreateTradeRequest) =>
    request<Trade>('/trades', { method: 'POST', body: JSON.stringify(payload) }),
}
