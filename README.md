# Trade Blotter

A single-page trade blotter. Enter trades, see them in a live blotter table, and see
positions automatically derived from trade history.

- **Backend:** C# / .NET 8 minimal API + SQLite (Dapper, `Microsoft.Data.Sqlite`)
- **Frontend:** Vue 3 (Composition API, TypeScript) + Pinia + Vite + PrimeVue 4 (Aura theme)

> Status: work in progress — this README is filled in as the build proceeds (see the
> commit history). Prerequisites, run steps, API reference, and design notes are finalized
> in the last step.

## Prerequisites

- **.NET 8 SDK** (backend)
- **Node.js 18+ or 20+** (frontend)

## Repository layout

```
trade_blotter/
  schema.sql        # DB structure: trades table, indexes, vw_positions (source of truth)
  seed.sql          # sample data exercising every accounting path
  backend/          # .NET 8 solution (API + tests)
  frontend/         # Vue 3 + TypeScript SPA
```

## Running

_TODO (finalized in the last commit): backend `dotnet run` + port, DB create/seed steps,
frontend `npm install` / `npm run dev` + port._

## API reference

_TODO._

## Design notes

_TODO: average-cost approach, money-storage choice, assumptions._
