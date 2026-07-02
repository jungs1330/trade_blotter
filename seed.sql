-- seed.sql
-- =============================================================================
-- Demo data that exercises every accounting code path. Applied via `dotnet run -- --seed`.
--
-- Re-runnable: existing rows are cleared first, so seeding always yields exactly this
-- state (on a fresh database the ids come out 1..8). Timestamps are ISO-8601 UTC in the
-- same format the server generates, spread ascending across a single trading day.
--
-- Expected positions after seeding (GET /positions):
--   AAPL  net  90  avg 151.67   (moving average; the sell does not change avg)
--   TSLA  net -30  avg 250.00   (short position; the partial cover does not change avg)
--   NVDA  net  10  avg 800.00   (simple single buy)
--   MSFT  omitted               (nets to zero)
-- =============================================================================

DELETE FROM trades;

INSERT INTO trades (symbol, side, quantity, price, timestamp) VALUES
  ('AAPL', 'Buy',  100, '150.00', '2026-06-29T09:30:00.000Z'),
  ('MSFT', 'Buy',  200, '300.00', '2026-06-29T09:45:00.000Z'),
  ('TSLA', 'Sell',  50, '250.00', '2026-06-29T10:00:00.000Z'),
  ('NVDA', 'Buy',   10, '800.00', '2026-06-29T10:15:00.000Z'),
  ('AAPL', 'Buy',   50, '155.00', '2026-06-29T11:00:00.000Z'),
  ('TSLA', 'Buy',   20, '240.00', '2026-06-29T12:30:00.000Z'),
  ('MSFT', 'Sell', 200, '310.00', '2026-06-29T13:45:00.000Z'),
  ('AAPL', 'Sell',  60, '160.00', '2026-06-29T14:30:00.000Z');
