-- schema.sql
-- =============================================================================
-- Single source of truth for the Trade Blotter database structure.
--
-- Design intent
--   * The application applies this file on startup (idempotent CREATE ... IF NOT
--     EXISTS), so there are no implicit EF-style migrations hiding the schema.
--   * Money is NOT stored as a native numeric type. SQLite has no real DECIMAL,
--     and REAL (double) would introduce float drift in prices/avg-cost. We store
--     `price` as TEXT (a canonical invariant-culture decimal string) and parse it
--     to C# `decimal` in the application layer. All money math stays in C#.
--   * Net quantity is exact integer math and is expressed here as a VIEW
--     (vw_positions). Order-dependent average cost is intentionally NOT computed
--     in SQL — it lives in the C# service layer (see README design notes), which
--     keeps the moving-average / short / sign-flip logic readable and in decimal.
-- =============================================================================

-- Trades are an append-only ledger. Positions are always DERIVED from this table
-- (via the view below + the C# accounting service); they are never stored as
-- separate mutable state.
CREATE TABLE IF NOT EXISTS trades (
  id        INTEGER PRIMARY KEY AUTOINCREMENT,
  symbol    TEXT    NOT NULL,
  side      TEXT    NOT NULL CHECK (side IN ('Buy','Sell')),
  quantity  INTEGER NOT NULL CHECK (quantity > 0),   -- whole shares, always positive; direction comes from `side`
  price     TEXT    NOT NULL,                         -- per-share price as a canonical decimal string (e.g. "150.25")
  timestamp TEXT    NOT NULL                          -- ISO-8601 UTC, server-generated; lexical order == chronological order
);

-- ix_trades_symbol    : positions/accounting group and iterate trades per symbol.
-- ix_trades_timestamp : GET /trades sorts newest-first; the accounting fold walks oldest-first.
CREATE INDEX IF NOT EXISTS ix_trades_symbol    ON trades(symbol);
CREATE INDEX IF NOT EXISTS ix_trades_timestamp ON trades(timestamp);

-- Net quantity per symbol is exact integer math (Buy = +qty, Sell = -qty).
-- Symbols that net to zero are omitted (HAVING net_quantity <> 0), matching the
-- positions contract. This view is the SQL-level source of truth for net qty;
-- the C# service produces the identical net plus the order-dependent avg cost.
CREATE VIEW IF NOT EXISTS vw_positions AS
SELECT
  symbol,
  SUM(CASE WHEN side = 'Buy' THEN quantity ELSE -quantity END) AS net_quantity
FROM trades
GROUP BY symbol
HAVING net_quantity <> 0;
