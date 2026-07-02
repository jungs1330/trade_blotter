# Trade Blotter — Build Specification

Build a single-page trade blotter application. A user enters trades, sees them in a live blotter table, and sees current positions automatically derived from trade history. Treat this as a small but production-minded application: clear API contracts, correct money/accounting handling, clean reactivity, and a tidy commit history.

## Tech stack

- Backend: C# / .NET 8 (minimal API or controllers — your call)
- Frontend: Vue 3 (Composition API) + Pinia + Vite
- Component library: **PrimeVue** (register PrimeVue with a theme preset — e.g. the Aura preset — in `main.js`)
- Database: SQLite

---

## Domain model & accounting rules

These rules remove the ambiguity in "net qty / avg cost." Implement exactly this behavior.

**Trade fields**

| Field | Type | Notes |
|---|---|---|
| `id` | integer | server-assigned |
| `symbol` | string | uppercased + trimmed on the server (e.g. `aapl ` → `AAPL`) |
| `side` | enum | `Buy` or `Sell` only |
| `quantity` | integer | whole shares, must be `> 0` (see note on fractional shares below) |
| `price` | decimal | per share, must be `> 0` |
| `timestamp` | string | ISO-8601 UTC, **server-generated on insert**; ignore any client-supplied value |

**Notional** (blotter column) = `quantity × price`.

**Signed quantity:** a Buy is `+quantity`, a Sell is `-quantity`. Net position = sum of signed quantities per symbol. **Short positions are allowed** (a symbol may net negative). A net position of **zero** is omitted from `GET /positions`.

**Average cost = moving average cost of the open position.** Process a symbol's trades in timestamp order:

- **Buy that increases the position** → update the weighted-average cost:
  `new_avg = (old_qty × old_avg + buy_qty × buy_price) / (old_qty + buy_qty)`
- **Sell that reduces a long** (or buy that reduces a short) → quantity decreases, **average cost is unchanged**. (You do not need to expose realized P&L; it's optional — see Out of scope.)
- **A trade that flips the sign** (e.g. long 100, then Sell 150) → close the existing position, then open the new opposite position at the flipping trade's price. The new average cost is that trade's price.
- **Position returns to zero** → symbol omitted from positions output.

**Worked example (AAPL):**
1. Buy 100 @ 150.00 → net 100, avg 150.00
2. Buy 50 @ 155.00 → net 150, avg (15000 + 7750) / 150 = **151.6667**
3. Sell 60 @ 160.00 → net 90, avg **unchanged at 151.6667**

So `GET /positions` reports AAPL as net 90, avg ≈ 151.67.

> If you'd prefer simpler accounting (e.g. avg cost = total buy notional ÷ total buy qty, ignoring sells), that's a valid simplification — but then change this section and say so in the README. Don't leave it implicit.

---

## Backend (C# / .NET 8)

### Endpoints

**`POST /trades`** — submit a new trade.

Request body (no `id`, no `timestamp`):
```json
{ "symbol": "AAPL", "side": "Buy", "quantity": 100, "price": 150.25 }
```
On success → **201 Created**, returns the persisted trade including server-assigned `id`, `timestamp`, and computed `notional`:
```json
{ "id": 1, "symbol": "AAPL", "side": "Buy", "quantity": 100, "price": 150.25, "notional": 15025.00, "timestamp": "2026-06-29T14:30:00Z" }
```

**`GET /trades`** — all trades, **newest first** → **200 OK**, array of trade objects (same shape as above).

**`GET /positions`** — derived positions per symbol → **200 OK**:
```json
[ { "symbol": "AAPL", "netQuantity": 90, "averageCost": 151.67 } ]
```
Symbols with `netQuantity == 0` are omitted.

### Validation (server-side — never trust the client)

Reject and return **400 Bad Request** when: symbol is empty/whitespace; side is not exactly `Buy` or `Sell`; quantity is missing, non-integer, or `<= 0`; price is missing or `<= 0`. Use a consistent error envelope:
```json
{ "error": { "code": "VALIDATION_ERROR", "message": "Quantity must be greater than zero.", "field": "quantity" } }
```

### Status codes

`200` reads, `201` create, `400` validation, `404` unknown route, `500` unexpected (don't leak stack traces — log them, return a generic message).

### Money & numeric correctness

- Use C# `decimal` (not `double`/`float`) for `price`, `notional`, and `averageCost`. Round `averageCost` and `notional` to 2 decimal places for display only; keep full precision in calculations.
- **SQLite has no native decimal.** Store `price` as `TEXT` (canonical decimal string) and parse to `decimal` in C#. Keep monetary math out of SQL — do net-quantity (integer) math in SQL and all money/avg-cost math in C# `decimal`. (If you prefer scaled-integer minor units, that's fine too — document the choice.)
- Round half-up; be consistent.

### Database

Provide a checked-in `schema.sql` that creates everything (no implicit EF migrations as the source of truth). Suggested:
```sql
CREATE TABLE IF NOT EXISTS trades (
  id        INTEGER PRIMARY KEY AUTOINCREMENT,
  symbol    TEXT    NOT NULL,
  side      TEXT    NOT NULL CHECK (side IN ('Buy','Sell')),
  quantity  INTEGER NOT NULL CHECK (quantity > 0),
  price     TEXT    NOT NULL,          -- decimal as canonical string
  timestamp TEXT    NOT NULL           -- ISO-8601 UTC
);
CREATE INDEX IF NOT EXISTS ix_trades_symbol    ON trades(symbol);
CREATE INDEX IF NOT EXISTS ix_trades_timestamp ON trades(timestamp);

-- Net quantity is cleanly derivable in a view (integer math only, exact):
CREATE VIEW IF NOT EXISTS vw_positions AS
SELECT
  symbol,
  SUM(CASE WHEN side = 'Buy' THEN quantity ELSE -quantity END) AS net_quantity
FROM trades
GROUP BY symbol
HAVING net_quantity <> 0;
```

**Positions are always derived, never stored as mutable state.** Net quantity comes from `vw_positions`. Average cost is order-dependent (see accounting rules), so compute it either in the repository/service layer or via a recursive CTE in SQL — **state which approach you chose and why in the README.** The application-layer approach is recommended because it keeps money math in `decimal` and the flip/short edge cases readable.

### CORS

The Vue dev server runs on a different origin than the API. Configure CORS to allow the Vite dev origin (e.g. `http://localhost:5173`) so the SPA can call the API in development.

### Tests

Include backend tests, at minimum covering the trickiest logic: the moving-average cost calculation across buys, a partial sell (avg unchanged), a position that nets to zero (omitted), a short position, and a sign flip. A couple of endpoint smoke tests (POST then GET round-trip, plus a validation 400) are a bonus.

---

## Frontend (Vue 3, Composition API)

Single-page app, three sections. Composition API used idiomatically; reactivity clean; **all shared state in Pinia, not scattered across components.** Build the UI with **PrimeVue** components (register the plugin and a theme preset in `main.js`); reach for PrimeVue primitives rather than hand-rolling inputs and tables.

### Pinia store

- **State:** `trades`, `positions`, plus `loading` / `error` flags.
- **Actions:** `fetchTrades()`, `fetchPositions()`, `submitTrade(payload)`.
- **Behavior on submit:** on a successful `POST`, update the store reactively — add the returned trade and refresh positions — so the blotter and positions update **without a page reload**.
- Getters for derived/sorted views (e.g. `sortedTrades`).

### Trade entry form

- Fields: symbol, side (Buy/Sell), quantity, price.
- Suggested PrimeVue components: `InputText` (symbol), `SelectButton` or `Dropdown` (side), `InputNumber` (quantity in integer mode, and price with a step/`minFractionDigits`), and `Button` (submit).
- Client-side validation mirroring the server: no empty fields; quantity a positive integer; price a positive number; side selected. Show inline errors (e.g. helper `small`/`Message` text with PrimeVue's `p-invalid` styling) and disable submit until valid.
- Surface server-side `400` errors to the user via a PrimeVue `Toast` (don't silently swallow them).
- On submit, the blotter and positions update immediately via the store.

### Blotter table

- Use PrimeVue **`DataTable`** with `Column`s.
- All trades, **newest first** by default.
- Columns: timestamp (formatted to local time), symbol, side, quantity, price, notional.
- **Sortable by at least timestamp and symbol** using `DataTable`'s built-in column sorting (`sortable` on those columns; client-side sorting is fine at this scale).
- **Scannable at a glance:** render side as a PrimeVue `Tag` (e.g. green `success` for Buy, red `danger` for Sell), right-align numeric columns, use tabular/monospaced numerals for prices, and format notional as currency.

### Positions panel

- A PrimeVue `DataTable` (or a set of `Card`s) with columns: symbol, net quantity, average cost. (Optional: cost-basis value = net qty × avg cost.)
- Updates **reactively** when a trade is submitted.
- Indicate sign of net quantity (e.g. negative = short, shown as a distinctly styled `Tag`). No market value / unrealized P&L (no market data — see Out of scope).

### "Live" definition

For this exercise, "live" means the UI updates reactively after the user's own submit. Real-time multi-client updates (polling `GET /trades` on an interval, or SignalR/WebSockets) are an **optional stretch**, not required.

---

## Seed data

Provide a checked-in `seed.sql` with timestamps spread across a day (ascending) that exercises every code path:

- **AAPL** — Buy 100 @ 150, Buy 50 @ 155, Sell 60 @ 160 → net **90**, avg **≈ 151.67** (tests moving average + sell-doesn't-change-avg).
- **MSFT** — Buy 200 @ 300, Sell 200 @ 310 → net **0** → **omitted** (tests the zero-net rule).
- **TSLA** — Sell 50 @ 250, Buy 20 @ 240 → net **−30**, avg **250.00** (tests short position).
- **NVDA** — Buy 10 @ 800 → net **10**, avg **800.00** (tests the simple case).

---

## Deliverables

- **`.gitignore`** — must be created and committed. Cover .NET build output (`bin/`, `obj/`), Node/Vue artifacts (`node_modules/`, `dist/`), the local SQLite database file(s) and any journal/WAL files, IDE folders (`.vs/`, `.idea/`, `.vscode/`), and `.env` files. The compiled DB, `node_modules`, and build output must **not** be committed.
- **`schema.sql`** — all table + view creation (single source of truth for the DB structure).
- **`seed.sql`** — the seed data above.
- **`README.md`** with: prerequisites (.NET 8 SDK; Node 18+ or 20+); exact steps to run the backend (`dotnet run`, including its port) and the frontend (`npm install` — which installs Vue, Pinia, and PrimeVue — then `npm run dev`, including the Vite port); how to create and seed the database; a short API reference; and a **design-notes** section documenting your average-cost approach, money-storage choice, and any assumptions you changed.
- **Clear, incremental commit history** — meaningful messages, one logical step per commit (not one giant "initial commit").

### Suggested build order (also a sensible commit plan)

1. Repo + solution scaffold, `.gitignore`, README skeleton
2. `schema.sql` (trades table, indexes, `vw_positions`)
3. Backend data layer + `GET /trades`
4. `POST /trades` with validation + error envelope
5. `GET /positions` with the moving-average accounting
6. Backend tests for the accounting + a couple of endpoint tests
7. `seed.sql` + seeding mechanism
8. Frontend scaffold (Vite + Vue 3 + Pinia + PrimeVue with a theme preset)
9. API client + Pinia store
10. Trade entry form + validation
11. Blotter table + sorting + Buy/Sell visual treatment
12. Positions panel (reactive)
13. CORS, polish, finalize README

---

## Definition of done

- [ ] `POST /trades` validates input, returns 201 with the persisted trade (server `id` + `timestamp` + `notional`).
- [ ] `GET /trades` returns all trades, newest first.
- [ ] `GET /positions` returns correct net qty + moving-average cost; zero-net symbols omitted; shorts handled.
- [ ] Positions are derived (view + logic), never stored as separate mutable state.
- [ ] Money uses `decimal` server-side; no float drift in prices/avg cost.
- [ ] Frontend (built with PrimeVue components): form validates, blotter updates on submit with no reload, positions update reactively, Buy/Sell are visually distinct, table is sortable.
- [ ] All shared state lives in Pinia.
- [ ] `.gitignore`, `schema.sql`, `seed.sql`, README, and CORS config are present; the app runs by following the README; build artifacts, `node_modules`, and the compiled DB are not committed.
- [ ] Backend tests cover the average-cost edge cases.

---

## Out of scope

- Authentication / authorization
- Real market data; therefore no market value or unrealized P&L in positions
- Multi-currency
- Real-time multi-client sync (optional stretch only)

## Assumptions you may adjust (state any changes in the README)

- Average cost = **moving average** of the open position.
- **Short positions allowed**; sells need not be covered by existing holdings.
- **Whole-share integer** quantities. If fractional shares are wanted, switch `quantity` to `decimal` and relax the integer validation.
- Monetary values stored as **TEXT decimal strings**; mapped to C# `decimal`.
