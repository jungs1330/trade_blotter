# Trade Blotter

A single-page trade blotter. Enter trades, see them in a live blotter table, and see
positions automatically derived from trade history.

- **Backend:** C# / .NET 8 minimal API + SQLite (Dapper, `Microsoft.Data.Sqlite`)
- **Frontend:** Vue 3 (Composition API, TypeScript) + Pinia + Vite + PrimeVue 4 (Aura theme)

## Design Decisions Made

- Position is derviced from trades using sql view instead of using code so that other 
  applications/reports can re-use the logic.
- Used PrimeVue as UI compnent library.
- Use SQLite as Database.
- Because I had time left, I add Symbol search feature.

## Other Notes

- Initial prompt used is in trade-blotter-prompt.md.
- Also add CLAUDE.md and DOCUMENTATION.md.
- Used following skills: vue-skills-bundle and dotnet-skills.
- Complete conversation history with Claude is in my_conversation.md.

## Prerequisites

- **.NET 8 SDK** (backend). Check with `dotnet --version` (should report `8.x`).
- **Node.js 18+ or 20+** and npm (frontend).

## Repository layout

```
trade_blotter/
  schema.sql        # DB structure: trades table, indexes, vw_positions (single source of truth)
  seed.sql          # demo data exercising every accounting path
  backend/
    TradeBlotter.sln
    src/TradeBlotter.Api/     # minimal API, Dapper, SQLite
    tests/TradeBlotter.Tests/ # xUnit
  frontend/                   # Vue 3 + TypeScript SPA (Vite)
```

## Running the backend

```bash
cd backend/src/TradeBlotter.Api
dotnet run
```

- Serves at **http://localhost:5099** (pinned in `Properties/launchSettings.json`).
- The database schema is applied automatically on startup from `schema.sql` (idempotent),
  so the API works on a fresh clone with no manual DB setup. The SQLite file `blotter.db`
  is created in the run directory (git-ignored).

### Creating and seeding the database

Schema is created automatically on first run. To load the demo data (see below), run the
one-shot seed command — it clears existing rows and inserts the seed set, then exits:

```bash
cd backend/src/TradeBlotter.Api
dotnet run -- --seed
```

No external `sqlite3` client is required — seeding runs `seed.sql` through
`Microsoft.Data.Sqlite`.

### Running the backend tests

```bash
cd backend
dotnet test
```

Covers the moving-average accounting edge cases (weighted average, partial sell, net-zero
omission, short, sign flip), the 2dp rounding policy, and endpoint smoke tests
(POST→GET round-trip and 400 validation cases) over a temporary SQLite database.

## Running the frontend

```bash
cd frontend
npm install        # installs Vue, Pinia, PrimeVue, primeicons
npm run dev
```

- Serves at **http://localhost:5173** (Vite default).
- Calls the API at `http://localhost:5099` by default. Override by copying `.env.example`
  to `.env` and setting `VITE_API_BASE_URL`.
- Start the backend first (and optionally `--seed` it) so the blotter and positions load.

The UI updates **live** after your own submit: on a successful POST the store prepends the
returned trade and refreshes positions — no page reload. Client-side validation mirrors the
server; server 400s are surfaced via a toast.

## API reference

Base URL: `http://localhost:5099`

### `POST /trades`
Submit a new trade. Request body (no `id`/`timestamp` — both server-assigned):
```json
{ "symbol": "AAPL", "side": "Buy", "quantity": 100, "price": 150.25 }
```
`201 Created` with the persisted trade:
```json
{ "id": 1, "symbol": "AAPL", "side": "Buy", "quantity": 100, "price": 150.25,
  "notional": 15025.00, "timestamp": "2026-06-29T14:30:00.000Z" }
```
`symbol` is trimmed + uppercased server-side; `notional = quantity × price`.

### `GET /trades`
`200 OK` — all trades, **newest first**, each shaped like the `POST` response.

### `GET /positions`
`200 OK` — derived positions per symbol; symbols with `netQuantity == 0` are omitted:
```json
[ { "symbol": "AAPL", "netQuantity": 90, "averageCost": 151.67 } ]
```

### Errors
Consistent envelope for all non-success responses:
```json
{ "error": { "code": "VALIDATION_ERROR", "message": "Quantity must be greater than zero.", "field": "quantity" } }
```
Status codes: `200` reads, `201` create, `400` validation (empty symbol; side not exactly
`Buy`/`Sell`; quantity missing/non-integer/`<= 0`; price missing/`<= 0`; malformed JSON),
`404` unknown route, `500` unexpected (logged; generic message, no stack traces).

## Design notes

### Average-cost approach (computed in the application layer)
Average cost is the **moving average of the open position**, computed in C# by
[`PositionCalculator`](backend/src/TradeBlotter.Api/Services/PositionCalculator.cs) — a pure
function that folds each symbol's trades in `(timestamp, id)` order:

- **Increase** (same direction): weighted average `(|net|·avg + |signed|·price) / |net+signed|`.
- **Partial reduce** (opposite, smaller): quantity falls, **average unchanged**.
- **Close** (opposite, equal): position goes to zero and is omitted.
- **Sign flip** (opposite, larger): close, then open the opposite side at the flip price.

Short positions are allowed (net may be negative). This lives in the service layer rather
than SQL because it keeps all money math in `decimal` and makes the flip/short/reduce cases
readable and directly unit-testable.

Net quantity is *also* cleanly derivable in SQL, and `schema.sql` ships the
`vw_positions` view (integer `SUM(±quantity)`, omitting zero-net symbols) as the SQL-level
source of truth for net quantity. The `/positions` endpoint uses the C# fold, which produces
the identical net quantity plus the order-dependent average cost in a single pass.

Positions are **always derived** from the trade ledger — never stored as separate mutable state.

### Money & numeric handling
- All monetary values use C# `decimal`. `price` is stored in SQLite as **TEXT** (a canonical
  invariant-culture decimal string, e.g. `"150.25"`) and parsed back to `decimal`; no money
  math happens in SQL. Net-quantity math (integers) is exact in SQL/`int`.
- `notional` and `averageCost` are rounded to **2 decimal places, half-up
  (`MidpointRounding.AwayFromZero`)** for display only; full precision is kept in calculations.
  `price` is returned as entered.
- Timestamps are server-generated UTC (`yyyy-MM-ddTHH:mm:ss.fffZ`), so lexical order equals
  chronological order for both the newest-first read and the oldest-first accounting fold.

### Assumptions & choices worth calling out
- **Moving-average** cost, **short positions allowed**, **whole-share integer** quantities,
  and **TEXT decimal** money storage — these follow the spec's default assumptions unchanged.
- **Frontend is TypeScript** (the spec referenced `main.js`); the entry point is `main.ts`
  with typed store/API/components.
- `dotnet run -- --seed` **resets** the trades table to the seed set (clears existing rows),
  so it always yields a known state.
- The price input/display allows up to 4 fractional digits (so exact prices aren't rounded
  away in the UI); `notional`/`averageCost` are always shown at 2dp.
- Data access uses Dapper + `Microsoft.Data.Sqlite` with `schema.sql` as the source of truth
  (no EF migrations).

### Seed data (after `--seed`)
| Symbol | Trades | Net | Avg cost |
|---|---|---|---|
| AAPL | Buy 100 @ 150, Buy 50 @ 155, Sell 60 @ 160 | 90 | 151.67 |
| MSFT | Buy 200 @ 300, Sell 200 @ 310 | 0 (omitted) | — |
| TSLA | Sell 50 @ 250, Buy 20 @ 240 | −30 (short) | 250.00 |
| NVDA | Buy 10 @ 800 | 10 | 800.00 |
