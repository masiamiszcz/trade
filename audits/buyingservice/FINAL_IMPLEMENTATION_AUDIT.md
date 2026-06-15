  # 🎯 FINAL IMPLEMENTATION AUDIT - Account Management + Crypto Trading

**Status:** ✅ READY TO BUILD  
**Data:** 2026-06-12  
**Scope:** Account Management + Buy/Sell Crypto Operations (bez overengineering)  
**Timeline:** 2-3 tygodnie (backend 1w, frontend 1w)

---

## 📋 PLAN NA MIESIĄC - TASKI I ETAPY

### ETAP 1: FUNDACJA (Dni 1-3) - Backend Setup
| # | Task | Priorytet | Pliki | Czas |
|---|------|-----------|-------|------|
| T1 | Create `ITradingService` interface + DTOs | 🔴 HIGH | 5 nowych | 2h |
| T2 | Create `IPositionRepository` + `ICryptoTransactionRepository` | 🔴 HIGH | 4 nowych | 2h |
| T3 | Implement repositories (SQL) | 🔴 HIGH | 2 nowych | 2h |
| T4 | Extend `AccountEntity` + `PositionEntity` + Create `CryptoTransactionEntity` | 🔴 HIGH | 3 edycje | 1h |
| T5 | Create EF Core configurations + migration | 🟡 MEDIUM | 2 nowe + 1 edycja | 1h |
| **RAZEM ETAP 1** | | | | **8h** |

### ETAP 2: LOGIKA BIZNESOWA (Dni 4-6) - Core Service Layer
| # | Task | Priorytet | Pliki | Czas |
|---|------|-----------|-------|------|
| T6 | Implement `PortfolioTransactionService` (BUY logic) | 🔴 HIGH | 1 nowy | 3h |
| T7 | Implement `PortfolioTransactionService` (SELL logic) | 🔴 HIGH | 1 edycja | 2h |
| T8 | Add price validation + concurrency control | 🔴 HIGH | 1 edycja | 2h |
| T9 | Add Unit of Work + transaction boundaries | 🟡 MEDIUM | 2 nowe | 2h |
| T10 | Register services in DI (Program.cs) | 🟢 LOW | 1 edycja | 15min |
| **RAZEM ETAP 2** | | | | **9.25h** |

### ETAP 3: API ENDPOINTS (Dni 7-8) - Controllers
| # | Task | Priorytet | Pliki | Czas |
|---|------|-----------|-------|------|
| T11 | Create `CryptoPortfolioController` (POST /buy, /sell) | 🔴 HIGH | 1 nowy | 2h |
| T12 | Extend `AccountController` (GET /portfolio, /transactions) | 🔴 HIGH | 1 edycja | 1h |
| T13 | Add error handling + validation | 🟡 MEDIUM | 1 edycja | 1h |
| **RAZEM ETAP 3** | | | | **4h** |

### ETAP 4: TESTING + DB (Dni 9-10) - Quality Assurance
| # | Task | Priorytet | Pliki | Czas |
|---|------|-----------|-------|------|
| T14 | Unit tests (service layer) | 🔴 HIGH | 1 nowy | 2h |
| T15 | Integration tests (API endpoints) | 🔴 HIGH | 1 nowy | 2h |
| T16 | Database migration + seed test data | 🟡 MEDIUM | 1 auto-gen | 1h |
| T17 | Manual testing in Postman | 🟡 MEDIUM | - | 1h |
| **RAZEM ETAP 4** | | | | **6h** |

### ETAP 5: FRONTEND (Dni 11-14) - UI Integration
| # | Task | Priorytet | Pliki | Czas |
|---|------|-----------|-------|------|
| T18 | Create `usePortfolio` hook + `useBuySell` hook | 🔴 HIGH | 2 nowe | 2h |
| T19 | Create `PortfolioPanel` component | 🔴 HIGH | 1 nowy | 2h |
| T20 | Create `BuySellForm` component + validation | 🔴 HIGH | 1 nowy | 2h |
| T21 | Integrate with API + handle errors | 🟡 MEDIUM | 3 edycje | 2h |
| T22 | Testing + UI polish | 🟡 MEDIUM | - | 2h |
| **RAZEM ETAP 5** | | | | **10h** |

### ETAP 6: FINALIZACJA (Dzień 15) - Deployment
| # | Task | Priorytet | Pliki | Czas |
|---|------|-----------|-------|------|
| T23 | Docker build + test | 🟡 MEDIUM | 1 edycja | 1h |
| T24 | E2E testing (cały flow) | 🔴 HIGH | - | 2h |
| T25 | Documentation update | 🟢 LOW | 1 nowy | 1h |
| **RAZEM ETAP 6** | | | | **4h** |

---

## 📊 PODSUMOWANIE TASKOWEGO

```
RAZEM BACKEND: 27.25h (T1-T17)
RAZEM FRONTEND: 10h (T18-T22)
RAZEM DEPLOYMENT: 4h (T23-T25)
=====================================
ŁĄCZNIE: ~41.25h (~1 tydzień intensive, lub 2-3 tygodnie casual)
```

---

## 🗂️ STRUKTURA PLIKÓW - CO TWORZYĆ / EDYTOWAĆ

### Backend Nowe Pliki (13)
```
1. backend/TradingPlatform.Core/Interfaces/ITradingService.cs
2. backend/TradingPlatform.Core/Interfaces/IPortfolioTransactionService.cs
3. backend/TradingPlatform.Core/Interfaces/IPositionRepository.cs
4. backend/TradingPlatform.Core/Interfaces/ICryptoTransactionRepository.cs
5. backend/TradingPlatform.Data/Repositories/SqlPositionRepository.cs
6. backend/TradingPlatform.Data/Repositories/SqlCryptoTransactionRepository.cs
7. backend/TradingPlatform.Core/Services/PortfolioTransactionService.cs
8. backend/TradingPlatform.Data/UnitOfWork/IUnitOfWork.cs
9. backend/TradingPlatform.Data/UnitOfWork/UnitOfWork.cs
10. backend/TradingPlatform.Api/Controllers/CryptoPortfolioController.cs
11. backend/TradingPlatform.Core/Dtos/BuyCryptoRequest.cs
12. backend/TradingPlatform.Core/Dtos/SellCryptoRequest.cs
13. backend/TradingPlatform.Core/Dtos/CryptoTransactionDto.cs
```

### Backend Edycje (11)
```
1. backend/TradingPlatform.Data/Entities/AccountEntity.cs (+ RowVersion)
2. backend/TradingPlatform.Data/Entities/PositionEntity.cs (+ new fields)
3. backend/TradingPlatform.Data/Entities/CryptoTransactionEntity.cs (CREATE)
4. backend/TradingPlatform.Data/Configurations/PositionEntityConfiguration.cs
5. backend/TradingPlatform.Data/Context/TradingPlatformDbContext.cs
6. backend/TradingPlatform.Data/Repositories/SqlAccountRepository.cs (+ concurrency)
7. backend/TradingPlatform.Api/Controllers/AccountController.cs (+ endpoints)
8. backend/TradingPlatform.Api/Program.cs (DI registration)
9. backend/TradingPlatform.Tests/Integration/PortfolioTransactionServiceTests.cs
10. backend/TradingPlatform.Tests/Integration/CryptoPortfolioControllerTests.cs
11. backend/TradingPlatform.Data/Migrations/[AUTO-GENERATED]
```

### Frontend Nowe Pliki (5)
```
1. frontend/src/hooks/usePortfolio.ts
2. frontend/src/hooks/useBuySell.ts
3. frontend/src/components/crypto/PortfolioPanel.tsx
4. frontend/src/components/crypto/BuySellForm.tsx
5. frontend/src/components/crypto/TransactionHistory.tsx
```

### Frontend Edycje (3)
```
1. frontend/src/config/apiConfig.ts (+ endpoints)
2. frontend/src/services/api.ts (+ requests)
3. frontend/src/pages/TradingDashboard.tsx (integrate)
```

---

## 🔑 KLUCZOWA ARCHITEKTURA

### BUY Flow (3 kroki)
```
1. Frontend wysyła: { symbol, quantity, requestedPrice }
   ↓
2. Backend:
   - Pobierz cenę z WebSocket (executionPrice)
   - Waliduj: |requestedPrice - executionPrice| <= 1%
   - Jeśli OK: update Account.AvailableBalance, create Position/CryptoTransaction
   - Jeśli FAIL: return 400 with reason
   ↓
3. Frontend: Pokaż success/error message
```

### SELL Flow (3 kroki)
```
1. Frontend wysyła: { symbol, quantity, requestedPrice }
   ↓
2. Backend:
   - Pobierz Position (sprawdź Quantity)
   - Pobierz cenę z WebSocket
   - Waliduj cenę
   - Jeśli OK: Position.Quantity -= quantity, Account.AvailableBalance += totalValue
   - Jeśli FAIL: return 400
   ↓
3. Frontend: Pokaż success/error message
```

### Walidacja Ceny (Kluczowy algorytm)
```csharp
// Backend NIGDY nie używa requestedPrice do kalkulacji!
executionPrice = await binanceWebSocket.GetLatestPriceAsync(symbol);
diff = Math.Abs(requestedPrice - executionPrice);
tolerance = requestedPrice * 0.01M; // 1%

if (diff > tolerance)
    return BadRequest("Cena zmieniła się o " + (diff/requestedPrice*100) + "%");

// Użyj executionPrice, nie requestedPrice!
totalValue = quantity * executionPrice;
```

### Concurrency Control (Optimistic Locking)
```csharp
// AccountEntity ma byte[] RowVersion
// Przy każdej transakcji EF Core sprawdza czy się zmienił
try {
    await dbContext.SaveChangesAsync();
} catch (DbUpdateConcurrencyException) {
    return Conflict("Saldo uległo zmianie. Spróbuj ponownie.");
}
```

### Transaction Boundaries (Atomic operations)
```csharp
using (var transaction = await dbContext.Database.BeginTransactionAsync()) {
    try {
        // 1. Update Account
        // 2. Update/Create Position
        // 3. Create CryptoTransaction
        // 4. Log Audit
        await dbContext.SaveChangesAsync();
        await transaction.CommitAsync();
    } catch {
        await transaction.RollbackAsync();
        throw;
    }
}
```

---

## 📡 API ENDPOINTS - KOMPLETNY SPEC

### POST /api/cryptoportfolio/buy
```json
REQUEST:
{
  "symbol": "BTCUSDT",
  "quantity": 0.5,
  "requestedPrice": 42500.50,
  "tolerancePercent": 1.0
}

RESPONSE 200 OK:
{
  "id": "guid",
  "symbol": "BTCUSDT",
  "type": "Buy",
  "quantity": 0.5,
  "requestedPrice": 42500.50,
  "executionPrice": 42510.00,
  "totalValue": 21255.00,
  "status": "Completed",
  "createdAt": "2026-06-12T10:30:00Z"
}

RESPONSE 400 BAD REQUEST:
{
  "error": "PriceValidationFailed",
  "message": "Cena zmieniła się o 2.5%. Tolerancja: 1%"
}

RESPONSE 400 BAD REQUEST:
{
  "error": "InsufficientBalance",
  "message": "Wymagane: 21255.00 USD, Dostępne: 5000.00 USD"
}
```

### POST /api/cryptoportfolio/sell
```json
REQUEST:
{
  "symbol": "BTCUSDT",
  "quantity": 0.5,
  "requestedPrice": 42500.50,
  "tolerancePercent": 1.0
}

RESPONSE 200 OK:
{
  "id": "guid",
  "symbol": "BTCUSDT",
  "type": "Sell",
  "quantity": 0.5,
  "requestedPrice": 42500.50,
  "executionPrice": 42490.00,
  "totalValue": 21245.00,
  "status": "Completed",
  "createdAt": "2026-06-12T10:35:00Z"
}

RESPONSE 400 BAD REQUEST:
{
  "error": "InsufficientQuantity",
  "message": "Posiadasz: 0.2 BTC, Chcesz sprzedać: 0.5 BTC"
}
```

### GET /api/cryptoportfolio/portfolio
```json
RESPONSE 200 OK:
{
  "baseCurrency": "USD",
  "positions": [
    {
      "symbol": "BTCUSDT",
      "quantity": 0.5,
      "averagePrice": 41000.00,
      "currentPrice": 42500.00,
      "totalValue": 21250.00,
      "pnl": 750.00,
      "pnlPercent": 3.67
    },
    {
      "symbol": "ETHUSDT",
      "quantity": 5.0,
      "averagePrice": 2000.00,
      "currentPrice": 2250.00,
      "totalValue": 11250.00,
      "pnl": 1250.00,
      "pnlPercent": 12.5
    }
  ],
  "summary": {
    "totalValue": 32500.00,
    "totalPnL": 2000.00,
    "totalPnLPercent": 6.54
  }
}
```

### GET /api/cryptoportfolio/transactions?limit=50&offset=0&type=Buy&status=Completed
```json
RESPONSE 200 OK:
{
  "total": 23,
  "limit": 50,
  "offset": 0,
  "transactions": [
    {
      "id": "guid-1",
      "symbol": "BTCUSDT",
      "type": "Buy",
      "quantity": 0.5,
      "executionPrice": 42500.00,
      "totalValue": 21250.00,
      "status": "Completed",
      "createdAt": "2026-06-12T10:30:00Z"
    },
    {
      "id": "guid-2",
      "symbol": "ETHUSDT",
      "type": "Buy",
      "quantity": 5.0,
      "executionPrice": 2250.00,
      "totalValue": 11250.00,
      "status": "Completed",
      "createdAt": "2026-06-12T09:15:00Z"
    }
  ]
}
```

---

## 🚀 QUICK START - ZARAZ PO AUDICIE

### Dzień 1-3: Stwórz fundację
```bash
# Backup current DB
docker compose down

# T1: Create interfaces + DTOs (2h)
# → Create 5 nowych plików w Interfaces/ i Dtos/

# T2-T4: Create entities + repositories (5h)
# → Extend entities, create SQL repos, create configurations

# T5: Generate migration
cd backend/TradingPlatform.Data
dotnet ef migrations add AddCryptoTrading --project . --startup-project ../TradingPlatform.Api
dotnet ef database update

# Build
cd ../../.. && docker compose up --build
```

### Dzień 4-6: Logika biznesowa
```bash
# T6-T9: Implement PortfolioTransactionService + UnitOfWork (9.25h)
# → Serce systemu - BUY/SELL logic

# Testy unit
dotnet test backend/TradingPlatform.Tests
```

### Dzień 7-8: API
```bash
# T11-T13: Controllers (4h)
# → POST /buy, /sell, GET /portfolio, /transactions

# Test w Postman
```

### Dzień 9-10: Testing
```bash
# T14-T17: Unit + integration tests (6h)
# → Pokrycie 80%+ core logic
```

### Dzień 11-14: Frontend
```bash
# T18-T22: Hooks + Components (10h)
# → usePortfolio, useBuySell, PortfolioPanel, BuySellForm

npm install
npm start
```

### Dzień 15: Finalizacja
```bash
# T23-T25: Docker + E2E + Docs (4h)
docker compose down
docker compose up --build

# Test pełny flow: Login → Kup crypto → Sprzedaj → Historia
```

---

## ✅ DEFINITION OF DONE

System jest **GOTOWY** gdy:

- [ ] **Backend kompiluje** bez błędów i ostrzeżeń
- [ ] **Migrations** zastosowana, baza ma nowe tabele
- [ ] **POST /api/cryptoportfolio/buy** działa: kupujesz crypto → saldo się zmienia
- [ ] **POST /api/cryptoportfolio/sell** działa: sprzedajesz → saldo się zmienia
- [ ] **GET /api/cryptoportfolio/portfolio** zwraca aktualne pozycje z P&L
- [ ] **GET /api/cryptoportfolio/transactions** zwraca historiję
- [ ] **Walidacja ceny**: jeśli cena zmienisz >1%, transakcja się odrzuca
- [ ] **Concurrency**: jeśli 2 użytkownicy jednocześnie kupią, oba się pomyślnie przetworzą
- [ ] **Testy**: Unit + Integration testy pass (80%+ coverage)
- [ ] **Frontend**: Widoczne portfolio, formularz buy/sell, historia
- [ ] **Docker**: Build successful, pełny flow działa w containerze
- [ ] **Dokumentacja**: API endpoints + diagram flow udokumentowane

---

## ⚠️ KRYTYCZNE WARUNKI

1. **Frontend wysyła `requestedPrice`**, backend pobiera i używa `executionPrice` z WebSocket'a
2. **Transakcje muszą być atomowe** - wszystko albo nic
3. **RowVersion** na Account zapobiega race conditions
4. **Error handling** - zawsze return Result<T>, nigdy raw exception
5. **Audit logging** - każda transakcja musi być zloggowana
6. **No overengineering** - tylko to co potrzebne, bez futures, bez advanced features

---

## 📚 REFERENCJE Z OBECNYCH PLIKÓW

```
Dobierz z audit.md:           Krótki, pragmatyczny plan
Dobierz z IMPLEMENTATION_PLAN: Szczegółowe architektury, error handling
Dobierz z CRYPTO_PORTFOLIO:   Walidacja ceny, concurrency control, WebSocket integration
```

---

## 🎯 NASTĘPNY KROK

👉 **Kiedy gotowy, zatnij tego audita i stwórz szczegółowy kod dla ETAPU 1 (T1-T5)**

Każdy task ma być skończy w ciągu 1-2 godzin. Bez długich dystrybuacji.

---

**Status:** ✅ READY  
**Szacowany czas:** 1 tydzień intensive / 2-3 tygodnie casual  
**Kolejny krok:** Start ETAP 1 - Fundacja  
**Odpowiedzialność:** Stick to planu, nie dodawaj scope, nie overengineer

---

---

# 🎨 FRONTEND IMPLEMENTATION PLAN - UI/UX GUIDE

**Cel:** Obsługa kont + Kupowanie/Sprzedawanie crypto  
**Stack:** React + TypeScript + TanStack Query + Vite  
**Lokalizacja:** `frontend/src/`

---

## 📱 STRUKTURA STRON I KOMPONENTÓW

### Główna nawigacja (zmiana w `Layout.tsx`)
```
Header (top):
├─ Logo/Brand
├─ User Menu (Imię + Avatar)
│  ├─ Mój profil
│  ├─ Ustawienia
│  ├─ 2FA
│  └─ Wyloguj
└─ Notifications bell (3 notifications)

Sidebar (left):
├─ 📊 Dashboard (home)
├─ 💰 Moje Konto (account management)
│  ├─ Saldo
│  ├─ Transfery
│  └─ Historia
├─ 📈 Trading (crypto buy/sell)
│  ├─ Portfolio
│  ├─ Kup/Sprzedaj
│  └─ Transakcje
├─ 📋 Historyjki
│  ├─ Transakcje
│  ├─ Transfery
│  └─ Audyty
└─ ⚙️ Settings
```

---

## 🏪 STRONY I KOMPONENTY - SZCZEGÓŁOWO

### STRONA 1: Moje Konto (`/account`)
**Cel:** Zarządzanie kontem + saldo

#### Layout
```
┌─────────────────────────────────────────┐
│ MOJE KONTO                              │
├─────────────────────────────────────────┤
│                                         │
│ ┌─ CARD: Saldo Konta ─────────────────┐ │
│ │ Total Balance:        50,000.00 USD  │ │
│ │ Available:            45,000.00 USD  │ │
│ │ Reserved:              5,000.00 USD  │ │
│ │                                      │ │
│ │ [Wpłata] [Wypłata] [Transfer]       │ │
│ └──────────────────────────────────────┘ │
│                                         │
│ ┌─ CARD: Ostatnie Operacje ────────────┐ │
│ │ Data       Typ        Kwota    Status │ │
│ │ 2026-06-12 Wpłata     10k USD  ✓     │ │
│ │ 2026-06-11 Transfer   5k USD   ✓     │ │
│ │ 2026-06-10 Wypłata    2k USD   ✓     │ │
│ └──────────────────────────────────────┘ │
│                                         │
│ [Pokaż historię] [Pobierz wyciąg]      │
└─────────────────────────────────────────┘
```

#### Komponenty
- **`AccountBalanceCard.tsx`** - Wyświetla saldo (3 wartości)
  - Props: `totalBalance`, `availableBalance`, `reservedBalance`
  - Guziki: [Wpłata] [Wypłata] [Transfer]
  
- **`RecentTransactionsCard.tsx`** - Tabela ostatnich transakcji (5 wierszy)
  - Props: `transactions: Transaction[]`
  - Kolumny: Data | Typ | Kwota | Status | Akcja(?)
  
- **`Modal: DepositModal.tsx`** - Pop-up wpłaty
  - Pola: Kwota (decimal), Metoda płatności
  - Guziki: [Wpłata] [Anuluj]
  
- **`Modal: WithdrawalModal.tsx`** - Pop-up wypłaty
  - Pola: Kwota, Numer rachunku
  - Guziki: [Wypłata] [Anuluj]
  
- **`Modal: TransferModal.tsx`** - Pop-up transferu wewnętrznego
  - Pola: Do konta (select), Kwota
  - Guziki: [Transfer] [Anuluj]

---

### STRONA 2: Trading - Portfolio (`/trading/portfolio`)
**Cel:** Przeglądanie pozycji crypto + P&L

#### Layout
```
┌────────────────────────────────────────────┐
│ PORTFOLIO                                  │
├────────────────────────────────────────────┤
│                                            │
│ ┌─ CARD: Podsumowanie ──────────────────┐ │
│ │ Total Portfolio Value:    50,000.00   │ │
│ │ Total P&L:                +1,500.00   │ │
│ │ Total P&L %:                 +3.0%    │ │
│ │                                       │ │
│ │ [Wykresy] [Details] [Export]          │ │
│ └───────────────────────────────────────┘ │
│                                            │
│ ┌─ TABELA: Moje Pozycje ────────────────┐ │
│ │ Symbol  Qty  Avg Price  Current  PnL  │ │
│ │ ───────────────────────────────────── │ │
│ │ BTC     0.5    41,000    42,500  +3%  │ │
│ │   [DETAILS]  [SELL]                  │ │
│ │ ETH     5.0     2,000     2,250  +6%  │ │
│ │   [DETAILS]  [SELL]                  │ │
│ │ ADA    100.0     0.50      0.65  +30% │ │
│ │   [DETAILS]  [SELL]                  │ │
│ └───────────────────────────────────────┘ │
│                                            │
│ [Kup nowe] [Odśwież]                      │
└────────────────────────────────────────────┘
```

#### Komponenty
- **`PortfolioSummaryCard.tsx`** - Podsumowanie portfolio
  - Props: `totalValue`, `totalPnL`, `totalPnLPercent`
  - Guziki: [Wykresy] [Details] [Export]
  
- **`PositionsTable.tsx`** - Tabela pozycji (sortowalna, filtruj po symbolach)
  - Props: `positions: Position[]`
  - Kolumny: Symbol | Qty | Avg Price | Current Price | Value | PnL | % | Akcja
  - Każdy row ma: [DETAILS] [SELL]
  
- **`PositionDetailsRow.tsx`** - Ekspandowana sekcja z detailami pozycji
  - Props: `position: Position`
  - Informacje: Entry date, Entry price, Current price, Gain/Loss, Commission
  - Guziki: [SELL] [Transfer] [Lock]
  
- **`Modal: SellPositionModal.tsx`** - Pop-up sprzedaży
  - Pola: Ilość (max dostępna), Cena, Tolerancja
  - Guziki: [Sprzedaj] [Anuluj]

---

### STRONA 3: Trading - Kup/Sprzedaj (`/trading/buy-sell`)
**Cel:** Kupowanie i sprzedawanie crypto

#### Layout
```
┌────────────────────────────────────────────┐
│ KUP / SPRZEDAJ CRYPTO                      │
├────────────────────────────────────────────┤
│                                            │
│  [KUP]  [SPRZEDAJ]  (toggle)               │
│                                            │
│ ┌─ FORM: Kup Crypto ───────────────────┐ │
│ │                                      │ │
│ │ Symbol:        [BTCUSDT ▼]           │ │
│ │ Cena aktualna: 42,500.00 USD         │ │
│ │                                      │ │
│ │ Ilość:         [   0.5    ]          │ │
│ │ Cena limit:    [42500.00 ]           │ │
│ │ Tolerancja:    [1.0 %]               │ │
│ │                                      │ │
│ │ RAZEM:         21,250.00 USD         │ │
│ │ Dostępnie:     45,000.00 USD (✓)     │ │
│ │                                      │ │
│ │              [KUP NOW]  [ANULUJ]     │ │
│ └──────────────────────────────────────┘ │
│                                            │
│ ┌─ INFO: Ostatnie Transakcje ───────────┐ │
│ │ 2026-06-12  BUY  BTCUSDT  0.5 ✓       │ │
│ │ 2026-06-11  BUY  ETHUSDT  5.0 ✓       │ │
│ │ 2026-06-10  SELL ADAUSDT  100 ✓       │ │
│ └──────────────────────────────────────┘ │
└────────────────────────────────────────────┘
```

#### Komponenty
- **`BuySellTabs.tsx`** - Toggle między KUP i SPRZEDAJ
  - State: `activeTab: 'buy' | 'sell'`
  - Guziki: [KUP] [SPRZEDAJ]
  
- **`BuyCryptoForm.tsx`** - Formularz kupna
  - Pola:
    - Symbol (autocomplete select z BTCUSDT, ETHUSDT, etc.)
    - Aktualna cena (read-only, z WebSocket)
    - Ilość (decimal input, min=0.001)
    - Cena limit (decimal input, default = current price)
    - Tolerancja (number input, %, default = 1%)
  - Kalkulacja: `Total = Quantity × CurrentPrice` (real-time)
  - Walidacja: `Available >= Total`
  - Guziki: [KUP NOW] [ANULUJ]
  
- **`SellCryptoForm.tsx`** - Formularz sprzedaży
  - Pola:
    - Symbol (select z dostępnych pozycji)
    - Max dostępna ilość (read-only)
    - Ilość do sprzedaży (decimal input, max validation)
    - Cena limit
    - Tolerancja
  - Kalkulacja: `Total = Quantity × CurrentPrice`
  - Guziki: [SPRZEDAJ] [ANULUJ]
  
- **`RecentTradesWidget.tsx`** - Ostatnie transakcje (3-5 wierszy)
  - Props: `recentTransactions: Transaction[]`
  - Info: Data, Symbol, Typ (BUY/SELL), Ilość, Status (✓/✗)

---

### STRONA 4: Historia Transakcji (`/trading/transactions`)
**Cel:** Pełna historia handlu (z filtrowaniem, paginacją)

#### Layout
```
┌────────────────────────────────────────────┐
│ HISTORIA TRANSAKCJI                        │
├────────────────────────────────────────────┤
│                                            │
│ Filtry:  [Typ ▼] [Status ▼] [Data ▼]     │
│          [Szukaj po symbolu]              │
│                                            │
│ ┌─ TABELA: Transakcje ──────────────────┐ │
│ │ Data       Symbol  Typ   Ilość  Cena  │ │
│ │ ──────────────────────────────────── │ │
│ │ 2026-06-12 BTC    BUY   0.5    42500  │ │
│ │ 2026-06-11 ETH    BUY   5.0     2250  │ │
│ │ 2026-06-10 ADA    SELL  100     0.65  │ │
│ │ 2026-06-09 BTC    SELL   0.2    42000 │ │
│ │ 2026-06-08 ETH    BUY    2.0     2100 │ │
│ │ ...                                   │ │
│ └───────────────────────────────────────┘ │
│                                            │
│ [◄ Poprzednia] [1] [2] [3] [...] [Następna ►]
│                                            │
│ [Pobierz CSV] [Eksport PDF]                │
└────────────────────────────────────────────┘
```

#### Komponenty
- **`TransactionFilters.tsx`** - Pasek filtrów
  - Filtry:
    - Typ (BUY/SELL/ALL)
    - Status (Completed/Rejected/Pending)
    - Data (From-To date picker)
    - Symbol (text input autocomplete)
  - Guziki: [Filter] [Reset]
  
- **`TransactionsTable.tsx`** - Tabela z paginacją
  - Props: `transactions: Transaction[]`, `totalCount`, `pageSize`, `onPageChange`
  - Kolumny: Data | Symbol | Typ | Ilość | Cena Limit | Cena Wykonania | Razem | Status | Akcja
  - Sortowalne kolumny: Data, Symbol, Typ
  - Status badges: ✓ (Completed), ✗ (Rejected), ⏳ (Pending)
  
- **`Pagination.tsx`** - Paginacja
  - Props: `total`, `pageSize`, `currentPage`, `onPageChange`
  - Display: [◄] [1] [2] [3] [...] [►]
  
- **`Modal: TransactionDetailModal.tsx`** - Szczegóły transakcji
  - Props: `transaction: Transaction`
  - Informacje: ID, Symbol, Typ, Ilość, Cena zażądana, Cena wykonania, Razem, Status, Data, Error (jeśli)

---

### STRONA 5: Historia Transferów (`/account/transfers`)
**Cel:** Historia transferów pieniędzy (wpłaty, wypłaty, transfery wewnętrzne)

#### Layout
```
┌────────────────────────────────────────────┐
│ HISTORIA TRANSFERÓW                        │
├────────────────────────────────────────────┤
│                                            │
│ Filtry: [Typ ▼] [Status ▼]                │
│                                            │
│ ┌─ TABELA: Transfery ───────────────────┐ │
│ │ Data       Typ        Kwota    Status │ │
│ │ ────────────────────────────────────  │ │
│ │ 2026-06-12 Wpłata    10,000 USD  ✓    │ │
│ │ 2026-06-11 Transfer   5,000 USD  ✓    │ │
│ │ 2026-06-10 Wypłata    2,000 USD  ✓    │ │
│ │ 2026-06-09 Wpłata     1,500 USD  ✓    │ │
│ │ ...                                   │ │
│ └───────────────────────────────────────┘ │
│                                            │
│ [Paginacja...]                             │
│                                            │
│ [Pobierz CSV]                              │
└────────────────────────────────────────────┘
```

#### Komponenty
- **`TransferFilters.tsx`** - Filtry (Typ, Status)
  - Typ: Wpłata, Wypłata, Transfer
  - Status: Pending, Completed, Rejected
  
- **`TransfersTable.tsx`** - Tabela transferów
  - Kolumny: Data | Typ | Kwota | Waluta | Status | Akcja
  - Status badges: ✓ / ✗ / ⏳

---

### KOMPONENT: Account Settings Modal
**Cel:** Ustawienia konta (zmiana hasła, 2FA, itp.)

#### Layout
```
┌─────────────────────────────────┐
│ USTAWIENIA KONTA       [✕]      │
├─────────────────────────────────┤
│                                 │
│ Email:  kuba@example.com        │
│ [Zmień email]                   │
│                                 │
│ Hasło:  ••••••••••              │
│ [Zmień hasło]                   │
│                                 │
│ 2FA:    ✓ Włączony              │
│ [Zarządzaj 2FA]                 │
│                                 │
│ Notyfikacje:                    │
│ ☑ Email na transakcje           │
│ ☑ Email na transfery            │
│ ☑ SMS alerts                    │
│                                 │
│         [Zapisz] [Anuluj]       │
└─────────────────────────────────┘
```

---

## 🔗 HOOKS - JAKIE UTWORZYĆ

### 1. `usePortfolio.ts` - Pobiera portfolio
```typescript
export function usePortfolio() {
  return useQuery({
    queryKey: ['portfolio'],
    queryFn: async () => {
      const res = await fetch('/api/cryptoportfolio/portfolio');
      return res.json(); // { positions, summary }
    },
    refetchInterval: 5000 // Co 5 sekund
  });
}
```

### 2. `useBuySell.ts` - Kupuj/Sprzedawaj
```typescript
export function useBuySell() {
  const queryClient = useQueryClient();
  
  return useMutation({
    mutationFn: async (order: BuyOrderRequest | SellOrderRequest) => {
      const endpoint = order.type === 'buy' 
        ? '/api/cryptoportfolio/buy' 
        : '/api/cryptoportfolio/sell';
      
      const res = await fetch(endpoint, {
        method: 'POST',
        body: JSON.stringify(order)
      });
      return res.json();
    },
    onSuccess: () => {
      queryClient.invalidateQueries(['portfolio']);
      queryClient.invalidateQueries(['transactions']);
    }
  });
}
```

### 3. `useTransactions.ts` - Historia transakcji
```typescript
export function useTransactions(limit = 50, offset = 0) {
  return useQuery({
    queryKey: ['transactions', limit, offset],
    queryFn: async () => {
      const res = await fetch(`/api/cryptoportfolio/transactions?limit=${limit}&offset=${offset}`);
      return res.json();
    }
  });
}
```

### 4. `useAccount.ts` - Info o koncie
```typescript
export function useAccount() {
  return useQuery({
    queryKey: ['account'],
    queryFn: async () => {
      const res = await fetch('/api/account/main');
      return res.json(); // { id, userId, balance, currency, ... }
    },
    refetchInterval: 10000
  });
}
```

### 5. `useTransfers.ts` - Historia transferów
```typescript
export function useTransfers(limit = 50, offset = 0) {
  return useQuery({
    queryKey: ['transfers', limit, offset],
    queryFn: async () => {
      const res = await fetch(`/api/account/transfers?limit=${limit}&offset=${offset}`);
      return res.json();
    }
  });
}
```

### 6. `useCurrentPrice.ts` - Cena z WebSocket
```typescript
export function useCurrentPrice(symbol: string) {
  const [price, setPrice] = useState<number | null>(null);
  
  useEffect(() => {
    const ws = new WebSocket(`wss://stream.binance.com:9443/ws/${symbol.toLowerCase()}@ticker`);
    ws.onmessage = (e) => {
      const data = JSON.parse(e.data);
      setPrice(parseFloat(data.c)); // current price
    };
    return () => ws.close();
  }, [symbol]);
  
  return price;
}
```

---

## 📊 WYGLĄD - DESIGN TOKENS

### Kolory
```
Primary:    #007AFF (Blue - actions)
Success:    #34C759 (Green - completed)
Danger:     #FF3B30 (Red - errors, sells)
Warning:    #FF9500 (Orange - pending)
Neutral:    #8E8E93 (Gray - secondary info)
Background: #F5F5F7 (Light gray)
```

### Komponenty
```
Card:         Border-radius: 12px, Shadow: 0 2px 8px rgba(0,0,0,0.1)
Button:       Padding: 12px 24px, Border-radius: 8px, Font-weight: 600
Input:        Padding: 10px 12px, Border: 1px solid #E5E5EA, Border-radius: 8px
Badge:        Padding: 4px 8px, Border-radius: 4px, Font-size: 12px
```

### Breakpoints
```
Mobile:    < 640px
Tablet:    640px - 1024px
Desktop:   > 1024px
```

---

## 📋 GUZIKI - PEŁNA LISTA

### Na stronie Account
- [Wpłata] → DepositModal
- [Wypłata] → WithdrawalModal
- [Transfer] → TransferModal
- [Pokaż historię] → `/account/transfers`
- [Pobierz wyciąg] → Export CSV/PDF

### Na stronie Portfolio
- [DETAILS] (na każdej pozycji) → PositionDetailsRow expand
- [SELL] (na każdej pozycji) → SellPositionModal
- [Wykresy] → Chart modal (future?)
- [Details] → Summary details (future?)
- [Export] → Export portfolio
- [Kup nowe] → `/trading/buy-sell`
- [Odśwież] → refetch portfolio

### Na stronie Buy/Sell
- [KUP] / [SPRZEDAJ] → Toggle tabs
- [KUP NOW] → Submit buy order
- [SPRZEDAJ] → Submit sell order
- [ANULUJ] → Cancel transaction

### Na stronie Transakcji
- [Typ ▼] / [Status ▼] / [Data ▼] → Filter dropdowns
- [Filter] / [Reset] → Apply/clear filters
- [◄] [1] [2] [3] [...] [►] → Pagination
- [Pobierz CSV] / [Eksport PDF] → Export
- Na każdej transakcji: [VIEW] → TransactionDetailModal

---

## 🎯 UŻYTKOWNIKA JOURNEY - KROK PO KROKU

### User Story 1: Kupić crypto
```
1. Zaloguj się
2. Przejdź do /trading/buy-sell
3. Wybierz symbol (BTCUSDT)
4. Wpisz ilość (0.5) i cenę limit (42500)
5. Kliknij [KUP NOW]
6. System waliduje cenę z WebSocket
7. Jeśli ✓ → Transakcja completed, portfolio updated
8. Jeśli ✗ → Pokaż error "Cena zmieniła się o X%"
9. Widać nową pozycję w /trading/portfolio
```

### User Story 2: Sprzedać crypto
```
1. Przejdź do /trading/portfolio
2. Znajdź pozycję (BTC 0.5)
3. Kliknij [SELL]
4. Wpisz ilość (0.5), cenę limit (42500)
5. Kliknij [SPRZEDAJ]
6. System waliduje
7. Jeśli ✓ → Pozycja zmniejszona, saldo zwiększone
8. Historia widoczna w /trading/transactions
```

### User Story 3: Zarządzać kontem
```
1. Kliknij account menu (top right)
2. Przejdź do /account
3. Widać: Total Balance, Available, Reserved
4. Kliknij [Wpłata] → Enter kwota → Confirm
5. Historia w /account/transfers
```

---

## 🚀 FRONTEND DEVELOPMENT PHASES

### Faza 1 (2 dni): Layout + Navigation
- [ ] Create `Layout.tsx` (header, sidebar)
- [ ] Create routing structure
- [ ] Stub out all pages with empty placeholders

### Faza 2 (3 dni): Account Management
- [ ] Implement `/account` page
- [ ] Create `AccountBalanceCard`, `RecentTransactionsCard`
- [ ] Create modals (Deposit, Withdrawal, Transfer)
- [ ] Create `/account/transfers` page

### Faza 3 (3 dni): Trading UI
- [ ] Implement `/trading/portfolio` page
- [ ] Create `PortfolioSummaryCard`, `PositionsTable`
- [ ] Implement `/trading/buy-sell` page
- [ ] Create `BuyCryptoForm`, `SellCryptoForm`

### Faza 4 (2 dni): Transactions
- [ ] Implement `/trading/transactions` page
- [ ] Create filters, pagination, export

### Faza 5 (2 dni): Integration
- [ ] Connect all hooks to real API
- [ ] Add error handling, loading states
- [ ] Test all flows end-to-end

---

**Frontend Status:** ✅ READY TO BUILD  
**Szacowany czas:** 10-12 dni (casual pace)  
**Następny krok:** Layout + Routing (Faza 1)
