/**
 * Shared domain types mirroring the backend API contracts. These are the wire shapes
 * exchanged with the .NET API; monetary values arrive as JS numbers (the server keeps
 * full `decimal` precision and rounds notional/averageCost to 2dp for display).
 */

export type Side = 'Buy' | 'Sell'

/** A persisted trade as returned by GET /trades and POST /trades. */
export interface Trade {
  id: number
  symbol: string
  side: Side
  quantity: number
  price: number
  notional: number
  /** ISO-8601 UTC string, server-generated. */
  timestamp: string
}

/** A derived position from GET /positions (zero-net symbols are omitted server-side). */
export interface Position {
  symbol: string
  /** Signed: positive = long, negative = short. */
  netQuantity: number
  averageCost: number
}

/** Payload for POST /trades (no id/timestamp — both are server-assigned). */
export interface CreateTradeRequest {
  symbol: string
  side: Side
  quantity: number
  price: number
}

/** The `error` object inside the API's error envelope. */
export interface ApiErrorDetail {
  code: string
  message: string
  field?: string | null
}

/** The full error envelope body: `{ "error": { code, message, field? } }`. */
export interface ApiErrorEnvelope {
  error: ApiErrorDetail
}
