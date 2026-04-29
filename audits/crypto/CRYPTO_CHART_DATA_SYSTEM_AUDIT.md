# Crypto Chart Data System Audit

## Context
The platform already has:
- Binance WebSocket trade ingestion
- 1m candle aggregation from trades
- data storage for candles
- logging + hosted service architecture
- a crypto detail page with SignalR live price feed

This audit designs a full chart data system for crypto assets while preserving the existing live Binance path.

---

## 1. Backend – Data Layer

### Recommended table

1. `Candles`
   - `Id INT PK`
   - `Symbol NVARCHAR(32)`
   - `Source NVARCHAR(16)` — `binance` or `coingecko`
   - `IntervalMinutes INT` — `1`, `5`, `60`, `1440`
   - `OpenTime DATETIME`
   - `CloseTime DATETIME`
   - `Open DECIMAL(18,8)`
   - `High DECIMAL(18,8)`
   - `Low DECIMAL(18,8)`
   - `Close DECIMAL(18,8)`
   - `Volume DECIMAL(28,8)`
   - `CreatedAt DATETIME`
   - Unique index: `(Symbol, Source, IntervalMinutes, OpenTime)`

### Why one table?
- All candles share the same shape.
- `Source` keeps Binance vs CoinGecko separated without extra tables.
- Daily `coingecko` candles are still isolated by `Source='coingecko'` and `IntervalMinutes=1440`.
- This is simpler, easier to maintain, and still production-safe.

### Interval storage
- 1m candles: `Candles`, `Source = 'binance'`, `IntervalMinutes = 1`
- 5m candles: `Candles`, `Source = 'binance'`, `IntervalMinutes = 5`
- 1h candles: `Candles`, `Source = 'binance'`, `IntervalMinutes = 60`
- daily candles: `Candles`, `Source = 'coingecko'`, `IntervalMinutes = 1440`

### Recommended indexes
- `IX_Candles_Symbol_Source_Interval_OpenTime`
- `IX_Candles_Symbol_OpenTime`

### Asset symbol mapping
Add a small mapping table to normalize exchange symbols to CoinGecko IDs:

3. `AssetMappings`
   - `Id INT PK`
   - `Symbol NVARCHAR(32)` — `BTCUSDT`
   - `CoingeckoId NVARCHAR(64)` — `bitcoin`
   - `Source NVARCHAR(16)` — optional for future provider expansion
   - Unique index on `(Symbol, CoingeckoId)`

This mapping is mandatory for multi-asset support and CoinGecko backfill.

### Logical partitioning guidance
Treat `Candles` as logically partitioned by symbol + interval. Query always as:
- `WHERE Symbol = @symbol AND Source = 'binance' AND IntervalMinutes = @interval`
- `ORDER BY OpenTime DESC`
- `TAKE N`

Do not use broad `OpenTime BETWEEN` scans for live chart requests. That pattern causes slow queries at scale.

---

## 2. Backend – Data Ingestion

### Binance live path
- Keep existing `BinanceWebSocketService` + `MarketProcessingService`.
- Continue building 1m candles from live trade ticks.
- On each completed 1m candle:
  - save it to `Candles` as `IntervalMinutes = 1`
  - send it to a `CandleAggregationService`
- `CandleAggregationService` must persist higher-resolution candles.
  - do not compute 5m/1h on request
  - always store 5m and 1h bars in `Candles`
- Treat 1m bars as the source of truth for derived intervals.
  - 5m and 1h bars must be reconstructable from 1m history
  - keep raw 1m candles in the DB for auditability and reprocessing
- `CandleAggregationService` should produce and persist:
  - 5m candle windows
  - 1h candle windows
- This ensures `7d` and `1y` queries are read-optimized and do not consume runtime CPU.

### Binance historical backfill
- Use Binance REST `GET /api/v3/klines`
- Backfill up to 1 year of 1m candles to `Candles`
- Derive 5m/1h from backfilled 1m bars if needed
- Keep this path separate from live processing logic

### CoinGecko historical backfill
- Use CoinGecko market chart or range API
- Store daily rows in `Candles` with `Source = 'coingecko'` and `IntervalMinutes = 1440`
- Use only for `range=all`

### Source strategy
- `Binance` = accurate live + short/medium history
- `CoinGecko` = full daily history only
- `Binance` path remains canonical for `30m`, `7d`, `1y`
- `CoinGecko` path only for `all`

---

## 3. Backend – API Design

### New endpoint
`GET /api/chart`

Query parameters:
- `symbol=BTCUSDT`
- `range=30m|7d|1y|all`
- optional `interval=1m|5m|1h|1d`
- optional `limit`
- optional `to=UTC-timestamp` — fetch data ending at this timestamp

`to` is required for chart panning/zoom and pagination. It allows the frontend to request historical windows without scanning the whole range.

### Range mapping
- `30m` → Binance only, `1m`
- `7d` → Binance only, `5m`
- `1y` → Binance only, `1h`
- `all` → CoinGecko only, `1d`

### API rule
- range drives source and interval
- no dynamic source combinations
- no runtime source fallback
- only `all` may use `coingecko`
- other ranges always query `binance`

### Response shape
```json
{
  "symbol": "BTCUSDT",
  "source": "binance",
  "interval": "5m",
  "range": "7d",
  "candles": [
    {
      "timestamp": "2026-04-29T12:00:00Z",
      "open": 68500.12,
      "high": 68620.00,
      "low": 68300.50,
      "close": 68520.33,
      "volume": 123.45
    }
  ]
}
```

### Selection logic
- Validate `symbol` via instrument service
- Choose table and interval by range
- Query rows by `Symbol`, `Source`, `Interval`, `OpenTime`
- Limit row count before projection
- Return ascending time order

### Performance handling
- enforce safe limits per range
- cache hot queries
- avoid fetching 1m data for 7d/1y on the frontend
- support `to` for pagination / chart scrolling
- always query latest rows with `ORDER BY OpenTime DESC` and `TAKE N`
- reverse results to ascending order before returning

---

## 4. Data Normalization

### Unified candle format
All API responses should use:
- `timestamp` (UTC open time)
- `open`
- `high`
- `low`
- `close`
- `volume`

### Normalization rules
- Convert Binance and CoinGecko fields into identical DTOs
- Keep `source` and `interval` metadata in the top-level response
- Do not expose provider-specific raw fields to frontend

### Volume handling
- Map CoinGecko volume if available
- If not available, return `0` or allow `null`

---

## 5. Frontend

### Chart data consumption
- Add a chart hook: `useCryptoChart(symbol, range)`
- Request `GET /api/chart?symbol=${symbol}&range=${range}`
- Normalize response to a candlestick series

### Expected format
- array of `{ timestamp, open, high, low, close, volume }`
- ascending order

### Range switching
- buttons: `30m`, `7d`, `1y`, `ALL`
- fetch only when range changes
- preserve cached result per `(symbol, range)`
- show source badge: `Binance` or `CoinGecko`

### Avoid overfetching
- cache chart results in memory using local hook state
- do not reload unless symbol/range changes
- avoid Redux / global chart state for this feature
- poll only if live chart updates are necessary
- use live candle updates for the current 1m bar, not only price ticks

### Live candle update
- push the latest current 1m candle through SignalR
- update only the last candle on the chart as new ticks arrive
- keep full range fetch as a separate API request
- this gives real-time chart behavior without full refetches

---

## 6. Crypto Page (UI)

### Required changes
- add chart area under live summary
- add range selector controls
- display chart status and source
- keep SignalR live price section intact

### Required state
- `symbol`
- `range`
- `chartData`
- `chartLoading`
- `chartError`
- `selectedInterval`

### Integration notes
- current `CryptoPage` remains the instrument header + live summary
- extend it with a chart card and range buttons
- fetch chart data on mount and when `symbol` or `range` changes

---

## 7. Performance + Scaling

### Indexes
- `IX_Candles_Symbol_Source_Interval_OpenTime`
- `IX_CoingeckoDailyCandles_Symbol_OpenTime`

### Query optimization
- filter by `symbol + source + interval`
- sort descending and `Take(limit)`
- project only required candle fields

### Limits
- `30m` → 30–100 rows
- `7d` → max 1500 rows
- `1y` → max 2000 rows
- `all` → max 1000 daily rows

### Caching
- short TTL for live ranges
- longer TTL for `all`
- cache key: `chart:{symbol}:{range}`

---

## 8. Step-by-Step Implementation Plan

1. Extend DB schema
   - add `Source` and `IntervalMinutes` to `Candles`
   - create `AssetMappings`
   - add indexes and logical partition guidance
2. Extend Binance processing
   - keep 1m live generation
   - build `CandleAggregationService` for 5m and 1h
   - persist 5m/1h bars in DB
3. Backfill history
   - Binance REST 1m backfill for 1 year
   - derive 5m/1h from backfilled 1m bars
   - CoinGecko daily backfill for all history using `AssetMappings`
4. Build API
   - implement `GET /api/chart`
   - support `symbol`, `range`, `interval`, `limit`, `to`
   - normalize response DTOs and paginate by `to`
5. Connect frontend
   - add chart component and range switcher to `CryptoPage`
   - use `useCryptoChart`
   - add live 1m candle update via SignalR
6. Validate
   - test each range and source selection
   - verify query pattern `ORDER BY OpenTime DESC` + `TAKE N`
   - validate live candle update behavior
7. Harden
   - monitor chart query latency
   - refresh daily history responsibly

---

## 9. Realization and Deployment

### What will be used
- Backend: .NET 9, ASP.NET Core, EF Core, SQL Server
- Frontend: React + TypeScript, Vite, existing SignalR client
- Existing backend files to extend:
  - `backend/TradingPlatform.Data/Services/Market/BinanceWebSocketService.cs`
  - `backend/TradingPlatform.Data/Services/Market/MarketProcessingService.cs`
  - `backend/TradingPlatform.Data/Repositories/SqlCandleRepository.cs`
  - `backend/TradingPlatform.Data/Context/TradingPlatformDbContext.cs`
  - `backend/TradingPlatform.Api/Controllers/CryptoController.cs`
  - `backend/TradingPlatform.Core/Dtos/CandleDto.cs`
  - `backend/TradingPlatform.Api/Hubs/CryptoPricesHub.cs`
  - `backend/TradingPlatform.Api/Services/PriceUpdatePublisher.cs`
  - `backend/TradingPlatform.Data/Extensions/ServiceCollectionExtensions.cs`
- Frontend files to extend/add:
  - `frontend/src/pages/CryptoPage.tsx`
  - `frontend/src/config/apiConfig.ts`
  - `frontend/src/hooks/useSignalR.ts`
  - `frontend/src/services/SignalRService.ts`
  - `frontend/src/services/MarketDataService.ts`
  - add `frontend/src/hooks/useCryptoChart.ts`
  - add `frontend/src/components/crypto/CryptoChart.tsx`

### Realization plan
1. Add new EF entities:
   - `CandleEntity` extended with `Source` and `IntervalMinutes`
   - represent daily CoinGecko candles in `Candles` using `Source = 'coingecko'` and `IntervalMinutes = 1440`
   - new `AssetMappingEntity`
2. Add migrations in `backend/TradingPlatform.Data/Migrations`
   - update DB schema
   - ensure `Program.cs` still runs `dbContext.Database.Migrate()` on startup
3. Add `CandleAggregationService`
   - persist 5m and 1h bars to `Candles`
   - use completed 1m candles from `MarketProcessingService`
4. Add mapping from exchange symbol to CoinGecko ID
   - store in `AssetMappings`
   - use in CoinGecko backfill and all history requests
5. Add chart API endpoint in `CryptoController`
   - support `symbol`, `range`, `interval`, `limit`, `to`
   - build response DTO with normalized candle structure
6. Extend SignalR path
   - push current 1m candle updates through `CryptoPricesHub`
   - reuse `PriceUpdatePublisher` or add a dedicated candle update publisher
7. Implement frontend chart UI
   - range selector + source badge
   - hook `useCryptoChart`
   - update last candle with live SignalR events

### Deployment notes
- Use existing Docker Compose setup; no new backend service required
- Migration is already applied on startup via `Program.cs`
- Backend container update is enough for schema and API changes
- Frontend update deploys via existing Vite build pipeline
- Ensure Redis remains available for SignalR and existing 2FA flows

### Verification
- Validate `GET /api/chart?symbol=BTCUSDT&range=7d` returns <= 1500 5m candles
- Validate `GET /api/chart?symbol=BTCUSDT&range=all` returns daily CoinGecko rows
- Validate live 1m chart updates when websocket ticks arrive
- Validate history requests use `ORDER BY OpenTime DESC` + `Take(N)`
- Validate symbol mapping works for CoinGecko `bitcoin` vs exchange `BTCUSDT`
