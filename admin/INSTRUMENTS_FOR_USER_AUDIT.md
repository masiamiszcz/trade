# INSTRUMENTS FOR USER - API INTEGRATION AUDIT

## 📋 OVERVIEW
This document summarizes the refactoring completed to replace mock instrument data with real backend API data across the trading dashboard.

**Date:** April 22, 2026  
**Status:** ✅ COMPLETE  
**Priority:** High - Data consistency & live market integration

---

## 🎯 OBJECTIVE
Replace all hardcoded mock data on user dashboards with real data from `GET /api/instruments/active` endpoint:
- ✅ Remove mock data completely
- ✅ Implement single source of truth (API)
- ✅ Filter by type: Stock, Crypto, CFD
- ✅ Handle loading/error states
- ✅ No fallback mocks

---

## 📊 WORK COMPLETED

### 1. ✅ BACKEND ENDPOINT IDENTIFICATION
- **Endpoint:** `GET /api/instruments/active`
- **Purpose:** Returns only approved, non-blocked instruments for regular users
- **Filtering:** Backend automatically filters `isActive=true`, `isBlocked=false`, `status='Approved'`
- **Auth:** No auth required (AllowAnonymous)
- **Returns:** Full `InstrumentDto` objects with `type` field (Stock|Crypto|Cfd|Etf|Forex)

**API Endpoints Available:**
- `GET /api/instruments/active` ← **USED FOR DASHBOARDS** (User-facing, filtered)
- `GET /api/instruments` (All instruments, paginated)
- `GET /api/instruments/{id}` (Single by ID)
- `GET /api/instruments/symbol/{symbol}` (Single by symbol)
- `GET /api/admin/instruments` (Admin: all statuses)

---

### 2. ✅ INSTRUMENT DATA STRUCTURE

**InstrumentDto fields:**
```typescript
{
  id: Guid                          // Unique instrument ID
  symbol: string                    // e.g., "AAPL", "BTC", "EURUSD"
  name: string                      // e.g., "Apple Inc."
  description: string               // Admin notes
  type: string                      // "Stock" | "Crypto" | "Cfd" | "Etf" | "Forex"
  pillar: string                    // "General" | "Stocks" | "Crypto" | "Cfd"
  baseCurrency: string              // "USD", "EUR", "PLN"
  quoteCurrency: string             // "USD", "EUR", "PLN"
  status: string                    // "Draft" | "PendingApproval" | "Approved" | "Rejected" | "Blocked" | "Archived"
  isActive: boolean                 // Logical delete flag
  isBlocked: boolean                // Admin-set block flag (EXCLUDED by /active endpoint)
  createdBy: Guid                   // Admin ID
  createdAtUtc: string              // ISO datetime
  modifiedBy?: Guid                 // Last modifier
  modifiedAtUtc?: string            // Last modification time
}
```

**Type Mapping:**
- `type === "Stock"` → Stock Dashboard
- `type === "Crypto"` → Crypto Dashboard  
- `type === "Cfd"` → CFD Dashboard
- NOT included: Etf, Forex (not used in dashboards)

---

### 3. ✅ FRONTEND CHANGES

#### 3.1 API Configuration
**File:** `frontend/src/config/apiConfig.ts`
- Added endpoint: `availableInstruments: '/instruments/active'`

#### 3.2 Service Layer - MarketDataService
**File:** `frontend/src/services/MarketDataService.ts`
- ✅ Added method: `getAvailableInstruments(): Promise<Instrument[]>`
- ✅ Uses centralized `httpClient.fetch<T>()` (no fetch/axios)
- ✅ Proper error handling via `ApiError` class
- ✅ Auto JWT injection via httpClient interceptors
- ✅ Retry logic (3 attempts, exponential backoff)
- ✅ 30s timeout

**Code:**
```typescript
async getAvailableInstruments(): Promise<Instrument[]> {
  try {
    return await httpClient.fetch<Instrument[]>({
      url: API_CONFIG.endpoints.market.availableInstruments,
      method: 'GET',
    });
  } catch (error) {
    throw this.handleError(error);
  }
}
```

#### 3.3 Custom Hook - useAvailableInstruments
**File:** `frontend/src/hooks/useAvailableInstruments.ts` (NEW)
- ✅ Fetches from API on mount
- ✅ Auto-filters by type:
  - `stocks` → type === "Stock"
  - `crypto` → type === "Crypto"
  - `cfd` → type === "Cfd"
  - `etf` → type === "Etf"
  - `forex` → type === "Forex"
- ✅ Returns: `allInstruments`, `stocks`, `crypto`, `cfd`, `etf`, `forex`, `loading`, `error`, `refetch()`
- ✅ Handles loading state
- ✅ Error handling with message
- ✅ Manual refetch capability

**Usage:**
```typescript
const { stocks, crypto, cfd, loading, error, refetch } = useAvailableInstruments();
```

#### 3.4 Dashboard Components - REFACTORED

##### 3.4.1 StockDashboard.tsx
**Status:** ✅ REPLACED  
**Changes:**
- ❌ Removed: `MOCK_STOCKS` array (5 Polish stocks)
- ✅ Added: `useAvailableInstruments()` hook
- ✅ Uses: `stocks` filtered data
- ✅ Displays: symbol, name, baseCurrency, status, description
- ✅ Loading state: "Wczytywanie danych..."
- ✅ Error state: Error message display
- ✅ Empty state: "Brak dostępnych akcji"

##### 3.4.2 CryptoDashboard.tsx
**Status:** ✅ REPLACED  
**Changes:**
- ❌ Removed: `MOCK_CRYPTOS` array (5 cryptocurrencies)
- ✅ Added: `useAvailableInstruments()` hook
- ✅ Uses: `crypto` filtered data
- ✅ Displays: symbol, name, baseCurrency, quoteCurrency, status, description
- ✅ All state handling (loading/error/empty)

##### 3.4.3 CFDDashboard.tsx
**Status:** ✅ REPLACED  
**Changes:**
- ❌ Removed: `MOCK_CFDS` array (5 CFD instruments)
- ✅ Added: `useAvailableInstruments()` hook
- ✅ Uses: `cfd` filtered data
- ✅ Displays: symbol, name, baseCurrency, quoteCurrency, status, description
- ✅ All state handling (loading/error/empty)

#### 3.5 Portfolio Component - REFACTORED

**File:** `frontend/src/components/user/PortfolioGrid.tsx`
- ❌ Removed: `DEFAULT_TILES` mock array (3 placeholder tiles)
- ✅ Added: `useAvailableInstruments()` hook
- ✅ Dynamic portfolio: Combines 2 stocks + 2 crypto + 2 CFD (configurable)
- ✅ Converts API instruments to `PortfolioTile` format
- ✅ Loading state with message
- ✅ Error state with message
- ✅ Empty state message
- ✅ Real-time data from API

---

### 4. ✅ DATA FLOW ARCHITECTURE

```
┌─────────────────────────────────────────────────────┐
│                   React Component                    │
│            (StockDashboard, CryptoDashboard, etc)    │
└────────────────┬────────────────────────────────────┘
                 │ calls
                 ▼
┌─────────────────────────────────────────────────────┐
│          useAvailableInstruments() Hook              │
│   - Fetches from MarketDataService on mount          │
│   - Filters by type (Stock|Crypto|Cfd)              │
│   - Returns: stocks[], crypto[], cfd[], loading,    │
│     error, refetch()                                 │
└────────────────┬────────────────────────────────────┘
                 │ calls
                 ▼
┌─────────────────────────────────────────────────────┐
│        MarketDataService.getAvailableInstruments()   │
│            Uses centralized httpClient              │
└────────────────┬────────────────────────────────────┘
                 │ calls
                 ▼
┌─────────────────────────────────────────────────────┐
│     httpClient.fetch() (Centralized HTTP Layer)      │
│   - JWT auto-injection                               │
│   - Retry logic (3x, exponential backoff)            │
│   - Timeout: 30s                                     │
│   - Error normalization                             │
└────────────────┬────────────────────────────────────┘
                 │ GET request
                 ▼
┌─────────────────────────────────────────────────────┐
│        GET /api/instruments/active                   │
│              (Backend Endpoint)                      │
│      Returns: Instrument[] (filtered)                │
└─────────────────────────────────────────────────────┘
```

---

### 5. ✅ REMOVED MOCK DATA

| Component | File | Mock Removed | Count |
|-----------|------|--------------|-------|
| StockDashboard | StockDashboard.tsx | MOCK_STOCKS | 5 items |
| CryptoDashboard | CryptoDashboard.tsx | MOCK_CRYPTOS | 5 items |
| CFDDashboard | CFDDashboard.tsx | MOCK_CFDS | 5 items |
| PortfolioGrid | PortfolioGrid.tsx | DEFAULT_TILES | 3 items |
| **TOTAL** | **4 files** | **18 mock items** | ✅ |

**Verification:** ✅ No remaining MOCK_, DEFAULT_, hardcoded data arrays found

---

### 6. ✅ STATE HANDLING

#### Loading State
- Message: "Wczytywanie danych..."
- Displayed during API fetch
- Applies to all dashboards and portfolio grid

#### Error State
- Message: "❌ Błąd podczas wczytywania danych: {error.message}"
- Shows only if `useAvailableInstruments()` error occurs
- Applies to all dashboards and portfolio grid

#### Empty State
- Message: "Brak dostępnych {typ}" or "Brak dostępnych instrumentów"
- Displayed when filtered list is empty
- Indicates no instruments of that type are available from API

---

### 7. ✅ IMPLEMENTATION CHECKLIST

- [x] Backend endpoint identified: `/api/instruments/active`
- [x] Instrument data structure verified
- [x] API config updated with new endpoint
- [x] MarketDataService extended with `getAvailableInstruments()`
- [x] Hook created: `useAvailableInstruments.ts`
- [x] Hook implements filtering by type
- [x] StockDashboard refactored (no mocks)
- [x] CryptoDashboard refactored (no mocks)
- [x] CFDDashboard refactored (no mocks)
- [x] PortfolioGrid refactored (no mocks)
- [x] All components handle loading state
- [x] All components handle error state
- [x] All components handle empty state
- [x] Centralized httpClient used (no fetch/axios)
- [x] No mock fallbacks left in code
- [x] Single source of truth: API

---

### 8. ✅ EDGE CASES HANDLED

| Edge Case | Implementation |
|-----------|-----------------|
| No data returned | "Brak dostępnych {typ}" message |
| Loading in progress | "Wczytywanie danych..." spinner |
| API error (network, 5xx, etc) | Error message with details |
| No stocks/crypto/CFD available | Graceful empty state per dashboard |
| API timeout | Retry logic (3 attempts, exponential backoff) |
| Partial data (1-2 types available) | Show only what's available, empty for others |
| Blocked instruments | Automatically excluded by `/api/instruments/active` |

---

## 🔍 VERIFICATION STEPS

### Pre-deployment Checklist
- [x] No `MOCK_` constants remain in codebase
- [x] No hardcoded arrays with instrument data
- [x] All dashboards fetch from `/api/instruments/active`
- [x] All requests use `httpClient` (centralized)
- [x] Error/loading states implemented everywhere
- [x] TypeScript types align with backend InstrumentDto
- [x] Filtering by type works correctly
- [x] Single filtering logic location (useAvailableInstruments hook)

### Testing Recommendations
1. **Happy Path:** Load dashboard → verify stocks/crypto/CFD display
2. **Loading:** Check loading spinner appears during fetch
3. **Error:** Disconnect network → verify error message
4. **Empty:** If no instruments of type available → verify "Brak danych" message
5. **Filter Validation:** Verify no blocked instruments appear
6. **Pagination:** If backend returns paginated data, verify handling
7. **Performance:** Check hook memoization prevents unnecessary re-renders

---

## 📁 FILES MODIFIED

### Created
- ✅ `frontend/src/hooks/useAvailableInstruments.ts` (NEW)

### Updated
- ✅ `frontend/src/config/apiConfig.ts` (added endpoint)
- ✅ `frontend/src/services/MarketDataService.ts` (added method)
- ✅ `frontend/src/pages/StockDashboard.tsx` (refactored)
- ✅ `frontend/src/pages/CryptoDashboard.tsx` (refactored)
- ✅ `frontend/src/pages/CFDDashboard.tsx` (refactored)
- ✅ `frontend/src/components/user/PortfolioGrid.tsx` (refactored)

### No Changes Needed
- ❌ Backend endpoints (already correct)
- ❌ InstrumentsService (admin-only, unaffected)
- ❌ DataTable component (reusable, no changes)

---

## 🚀 DEPLOYMENT IMPACT

- **Risk Level:** LOW - Read-only changes, no backend modifications
- **User Impact:** HIGH - Real data now shown instead of placeholders
- **Performance:** Minimal - Single API call on component mount, memoized filtering
- **Rollback:** Easy - Switch back to mock data if needed
- **Testing:** Requires live API backend with `/api/instruments/active` endpoint

---

## 📝 NEXT STEPS (OPTIONAL)

1. **Pagination:** If dataset is large, implement pagination in hook
2. **Caching:** Add optional caching layer in MarketDataService
3. **Real-time:** Implement WebSocket for live price updates
4. **Search:** Add instrument search/filter UI
5. **Analytics:** Track which instruments users view most
6. **Performance:** Lazy-load dashboard tabs

---

## 📌 NOTES

- ✅ All mock data completely removed (18 items from 4 files)
- ✅ Single source of truth: `/api/instruments/active` endpoint
- ✅ No fallback mocks - dashboards fail gracefully if API unavailable
- ✅ Proper error boundary implementation
- ✅ Loading states improve UX
- ✅ Type-safe filtering by instrument type
- ✅ Reusable hook for other components needing instruments
- ✅ Centralized HTTP client prevents duplicate code

---

**Status:** ✅ READY FOR TESTING & DEPLOYMENT

**Last Updated:** April 22, 2026  
**Reviewer:** Refactoring Complete
