# Crypto Portfolio Feature - MINIMAL IMPLEMENTATION PLAN

**Data:** 2026-06-09  
**Status:** ✅ READY TO CODE — BEZ TEORII, TYLKO PRAKTYKA

---

## ⚡ KRÓTKO: CO ROBIĆ

System: **Użytkownik kupuje/sprzedaje crypto, backend waliduje cenę z WebSocket, transakcja = atomowa.**

---

## 1. LISTA ZADAŃ (COPY-PASTE DO TODO)

- [ ] **Backend M1:** Rozszerz `PositionEntity` + dodaj `RowVersion` do `AccountEntity`
- [ ] **Backend M2:** Stwórz `CryptoTransactionEntity` + EF config + migracja
- [ ] **Backend M3:** `IPositionRepository` + `ICryptoTransactionRepository` + repozytorium SQL
- [ ] **Backend M4:** `IPortfolioTransactionService` + `PortfolioTransactionService` (BUY/SELL logika)
- [ ] **Backend M5:** `CryptoPortfolioController` + DTOs + DI registration
- [ ] **Backend M6:** Testy (unit + integracja)
- [ ] **Frontend M1:** Hook `usePortfolio` + `useBuySell` 
- [ ] **Frontend M2:** Komponenty UI (PortfolioPanel, BuySellForm, TransactionHistory)
- [ ] **Frontend M3:** Integracja endpointów w `apiConfig`

---

## 2. CO JUŻ ISTNIEJE

✅ `AccountEntity` — wallet fiat  
✅ `PositionEntity` — pozycje crypto  
✅ `IBinanceWebSocketService` — pobieranie ceny z WebSocket'a  
✅ `InstrumentEntity` — katalog instrumentów  
✅ Schemata: enums `TransactionType`, `TransactionStatus` już powinny być

❌ Brakuje: `CryptoTransactionEntity` (trading ledger)  
❌ Brakuje: Service layer do BUY/SELL  
❌ Brakuje: API controller do tradingu  
❌ Brakuje: Frontend integration

---

## 3. PLIKI DO STWORZENIA I ZMODYFIKOWANIA

### Backend

| Plik | Akcja | Cel |
|---|---|---|
| `backend/TradingPlatform.Data/Entities/PositionEntity.cs` | UPDATE | Dodaj nullable pola: `StopPrice`, `TakeProfitPrice`, `Leverage`, `Commission`, `ClosedPrice`, `ClosedAtUtc`, `byte[] RowVersion` |
| `backend/TradingPlatform.Data/Entities/AccountEntity.cs` | UPDATE | Dodaj `byte[] RowVersion` |
| `backend/TradingPlatform.Data/Entities/CryptoTransactionEntity.cs` | CREATE | Nowa tabela: Type, Quantity, RequestedPrice, ExecutionPrice, TotalValue, Status, Currency, Timestamps |
| `backend/TradingPlatform.Data/Configurations/CryptoTransactionEntityConfiguration.cs` | CREATE | EF Core config |
| `backend/TradingPlatform.Data/DbContext/TradingPlatformDbContext.cs` | UPDATE | Dodaj `DbSet<CryptoTransactionEntity>` |
| `backend/TradingPlatform.Core/Interfaces/IPositionRepository.cs` | CREATE | CRUD dla Position + concurrency |
| `backend/TradingPlatform.Core/Interfaces/ICryptoTransactionRepository.cs` | CREATE | CRUD dla CryptoTransaction |
| `backend/TradingPlatform.Core/Interfaces/IAccountRepository.cs` | UPDATE | Dodaj `UpdateWithConcurrencyCheckAsync()` |
| `backend/TradingPlatform.Data/Repositories/SqlPositionRepository.cs` | CREATE | Implementacja |
| `backend/TradingPlatform.Data/Repositories/SqlCryptoTransactionRepository.cs` | CREATE | Implementacja |
| `backend/TradingPlatform.Data/Repositories/SqlAccountRepository.cs` | UPDATE | Dodaj concurrency logic |
| `backend/TradingPlatform.Core/Interfaces/IPortfolioTransactionService.cs` | CREATE | BuyCrypto, SellCrypto, GetPortfolio, GetTransactionHistory |
| `backend/TradingPlatform.Core/Services/PortfolioTransactionService.cs` | CREATE | Główna logika BUY/SELL |
| `backend/TradingPlatform.Core/Dtos/CryptoPortfolioDto.cs` | CREATE | DTOs: BuyCryptoRequest, SellCryptoRequest, CryptoTransactionDto, PortfolioSummaryDto |
| `backend/TradingPlatform.Api/Controllers/CryptoPortfolioController.cs` | CREATE | POST /buy, /sell, GET /positions, /transactions |
| `backend/TradingPlatform.Data/Extensions/ServiceCollectionExtensions.cs` | UPDATE | Zarejestruj w DI |
| Migration | RUN | `dotnet ef migrations add AddPortfolioTransactionModels && dotnet ef database update` |

### Frontend

| Plik | Akcja | Cel |
|---|---|---|
| `frontend/src/hooks/usePortfolio.ts` | CREATE | Pobierz portfolio użytkownika |
| `frontend/src/hooks/useBuySell.ts` | CREATE | POST /buy, /sell |
| `frontend/src/config/apiConfig.ts` | UPDATE | Dodaj endpoints |
| `frontend/src/components/crypto/PortfolioPanel.tsx` | CREATE | Widget portfolio |
| `frontend/src/components/crypto/BuySellForm.tsx` | CREATE | Formularz transakcji |

---

## 4. KLUCZOWA LOGIKA (PSEUDOKOD)

### BUY Flow (Backend)
```
1. Pobierz Account, sprawdź AvailableBalance
2. executionPrice = await WebSocket.GetLatestPrice(symbol)
3. Waliduj: |requestedPrice - executionPrice| <= requestedPrice * 1%
   - Jeśli NIE: REJECT z komunikatem ceny
4. totalValue = quantity × executionPrice
5. Jeśli Account.AvailableBalance < totalValue: REJECT
6. BEGIN TRANSACTION
   - Account.AvailableBalance -= totalValue
   - Position: utwórz lub aktualizuj (liczenie average price)
   - CryptoTransaction: {Type=Buy, RequestedPrice, ExecutionPrice, Status=Completed}
   - COMMIT
7. Zwróć CryptoTransactionDto
```

### SELL Flow (Backend)
```
1. Pobierz Position, sprawdź Quantity >= requestedQuantity
2. executionPrice = await WebSocket.GetLatestPrice(symbol)
3. Waliduj cenę (jak wyżej)
4. totalValue = quantity × executionPrice
5. BEGIN TRANSACTION
   - Position.Quantity -= quantity
   - jeśli Quantity == 0: ustaw ClosedAtUtc, ClosedPrice
   - Account.AvailableBalance += totalValue
   - CryptoTransaction: {Type=Sell, ...Status=Completed}
   - COMMIT
6. Zwróć CryptoTransactionDto
```

### Walidacja ceny
```csharp
diff = Math.Abs(requestedPrice - executionPrice)
tolerance = requestedPrice * (tolerancePercent / 100)
if (diff > tolerance) REJECT("Cena zmieniła się o X%")
else USE(executionPrice)
```

---

## 5. ENDPOINTY API

| Metoda | Endpoint | Request | Response |
|---|---|---|---|
| POST | `/api/cryptoportfolio/buy` | `{symbol, quantity, requestedPrice, tolerancePercent?}` | 200: CryptoTransactionDto \| 400: PriceValidationFailed \| 400: InsufficientBalance \| 404: InstrumentNotFound |
| POST | `/api/cryptoportfolio/sell` | `{symbol, quantity, requestedPrice, tolerancePercent?}` | 200: CryptoTransactionDto \| 400: InsufficientQuantity \| 404: PositionNotFound |
| GET | `/api/cryptoportfolio/positions` | - | 200: PortfolioSummaryDto {positions[], totalValue, totalPnL} |
| GET | `/api/cryptoportfolio/transactions` | `?limit=50&offset=0` | 200: PaginatedResult<CryptoTransactionDto> |

---

## 6. TIMELINE

---

## 7. GOTOWOŚĆ

✅ **System jest GOTOWY** gdy:

**Backend:** CryptoTransaction entity + Service z BUY/SELL + Controller + Testy + Migracja DB  
**Frontend:** Hooks + UI komponenty + API integration  

**Definition of Done:**
1. User może kupić crypto: POST /api/cryptoportfolio/buy → (200 OK / 400 error)
2. User może sprzedać crypto: POST /api/cryptoportfolio/sell → (200 OK / 400 error)
3. User widzi portfolio: GET /api/cryptoportfolio/positions → lista pozycji z P&L
4. User widzi historię: GET /api/cryptoportfolio/transactions → paginowana historia
5. Cena z frontendu jest walidowana z WebSocket'em (1% tolerancja)
6. Transakcje są atomowe (wszystko albo nic)
7. Concurrency protection (RowVersion) na Account i Position
8. Error handling: 400 (validation), 404 (not found), 409 (concurrency)

---

## 8. WAGI ÓWNE SZTUCZKI

1. **Frontend wysyła `requestedPrice`** = cena którą widział na ekranie
2. **Backend pobiera `executionPrice`** = cena z WebSocket'a w tym momencie
3. **Backend waliduje**: `|requested - execution| <= requested * 1%`
4. **Backend używa `executionPrice`** do rozliczenia (nie `requestedPrice`!)
5. **Atomiczność**: BEGIN TX → update Account → update Position → insert CryptoTransaction → COMMIT
6. **RowVersion**: auto-increment na każdą zmianę, EF Core rzuca exception jeśli nie zgadza się

---

## 9. START

👉 **Zacznij od Backend M1** (rozszerz `PositionEntity`)  
👉 Potem **Backend M2** (stwórz `CryptoTransactionEntity`)  
👉 Potem **Backend M3-M6** (repozytoria, service, controller, testy)  
👉 Na koniec **Frontend M1-M3** (hooks, komponenty, integracja)

**Czas:** ~1 tydzień (backend 3-4 dni, frontend 2-3 dni)



```csharp
public sealed class AccountEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid? ParentAccountId { get; set; }
    public string AccountNumber { get; set; }
    public string Currency { get; set; }              // Waluta bazowa: USD, EUR, PLN, etc.
    public decimal AvailableBalance { get; set; }    // Gotówka dostępna
    public decimal ReservedBalance { get; set; }     // Gotówka zarezerwowana
    public byte[] RowVersion { get; set; }           // ✅ NOWE: optimistic locking
    public AccountPillar Pillar { get; set; }        // General, Stocks, Crypto, Cfd
    public DateTimeOffset CreatedAtUtc { get; set; }
    
    // Relacje
    public ICollection<PositionEntity> Positions { get; set; }
    public ICollection<CryptoTransactionEntity> Transactions { get; set; }  // ✅ NOWE
}
```

✅ **Przygotowana na multi-currency** — każde konto może mieć inną walutę bazową.

#### 3.1.2 `PositionEntity` — Uniwersalna pozycja (Crypto/Akcje/CFD)
**Plik:** `backend/TradingPlatform.Data/Entities/PositionEntity.cs`

```csharp
public sealed class PositionEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid InstrumentId { get; set; }
    
    // Wspólne pola dla wszystkich instrumentów
    public decimal Quantity { get; set; }            // Ilość: crypto/akcje/CFD
    public decimal AverageOpenPrice { get; set; }    // Średnia cena wejścia
    public decimal ReservedQuantity { get; set; }    // Ilość zarezerwowana do sprzedaży
    public byte[] RowVersion { get; set; }           // ✅ NOWE: optimistic locking
    
    // Pola opcjonalne dla CFD i zaawansowanych strategii
    public decimal? StopPrice { get; set; }          // ✅ NOWE: stop-loss dla CFD
    public decimal? TakeProfitPrice { get; set; }    // ✅ NOWE: take-profit dla CFD
    public decimal? Leverage { get; set; }           // ✅ NOWE: dźwignia dla CFD
    public decimal? Commission { get; set; }         // ✅ NOWE: prowizja/swap
    public decimal? ClosedPrice { get; set; }        // ✅ NOWE: cena zamknięcia
    public DateTimeOffset? ClosedAtUtc { get; set; } // ✅ NOWE: kiedy zamknięta
    
    public DateTimeOffset OpenedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    
    // Relacje
    public AccountEntity Account { get; set; }
    public InstrumentEntity Instrument { get; set; }
}
```

✅ **Wszystkie pola są nullable (oprócz wymaganych)** → jeden model dla wszystkich typów.

#### 3.1.3 `CryptoTransactionEntity` — Historia handlu (Trading Ledger)
**Plik:** `backend/TradingPlatform.Data/Entities/CryptoTransactionEntity.cs` (DO STWORZENIA)

```csharp
public sealed class CryptoTransactionEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid InstrumentId { get; set; }
    
    // Typ transakcji i ilość
    public TransactionType Type { get; set; }        // Buy, Sell
    public decimal Quantity { get; set; }            // Ilość
    
    // Ceny i walidacja
    public decimal RequestedPrice { get; set; }      // Cena przesłana przez frontend
    public decimal ExecutionPrice { get; set; }      // Rzeczywista cena z WebSocket (zaakceptowana)
    public decimal PriceDifference { get; set; }     // |requestedPrice - executionPrice|
    public decimal PriceTolerancePercent { get; set; } = 1m;  // Tolerancja (% lub wartość absolutna)
    
    // Wyliczenia
    public decimal TotalValue { get; set; }          // Quantity × ExecutionPrice
    public decimal? Commission { get; set; }         // Prowizja transakcji (opcjonalnie)
    
    // Status i walidacja
    public string Currency { get; set; }             // USD, EUR, PLN, etc.
    public TransactionStatus Status { get; set; }    // Pending, Completed, Failed, Rejected
    public string? RejectionReason { get; set; }     // "Cena zmieniła się o 2%, limit 1%"
    
    // Czasowe
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? ExecutedAtUtc { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    
    // Relacje
    public AccountEntity Account { get; set; }
    public InstrumentEntity Instrument { get; set; }
}
```

#### 3.1.4 Enums - AKTUALNE
**Plik:** `backend/TradingPlatform.Core/Enums/TradingEnums.cs`

```csharp
public enum TransactionType
{
    Buy = 1,
    Sell = 2
}

public enum TransactionStatus
{
    Pending = 1,           // Czeka na potwierdzenie ceny
    Completed = 2,         // Udana i rozliczona
    Failed = 3,            // Błąd systemu
    Rejected = 4,          // Odrzucona (cena się zmieniła)
    Cancelled = 5          // Anulowana przez użytkownika
}
```

✅ Już istnieją w kodzie!

### 3.2 Repozytorium i Unit of Work

#### 3.2.1 Interfejsy Repozytorium
**Plik:** `backend/TradingPlatform.Core/Interfaces/` (DO ROZSZERZENIA)

```csharp
// Nowy interfejs dla transakcji
public interface ICryptoTransactionRepository
{
    Task AddAsync(CryptoTransactionEntity transaction, CancellationToken ct = default);
    Task<CryptoTransactionEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<CryptoTransactionEntity>> GetByAccountIdAsync(Guid accountId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

// Rozszerzenie istniejącego
public interface IAccountRepository
{
    // ... istniejące metody ...
    
    // ✅ NOWE: atomiczne transakcje z concurrency control
    Task<Account> UpdateWithConcurrencyCheckAsync(
        Account account, 
        byte[] expectedRowVersion, 
        CancellationToken ct = default);
}
```

#### 3.2.2 Unit of Work (opcjonalnie, ale zalecane)
**Plik:** `backend/TradingPlatform.Core/Interfaces/IUnitOfWork.cs` (OPCJONALNE)

Jeśli chcesz atomiczne operacje na wielu agregatach:

```csharp
public interface IUnitOfWork : IDisposable
{
    IAccountRepository Accounts { get; }
    IPositionRepository Positions { get; }
    ICryptoTransactionRepository Transactions { get; }
    
    Task<int> SaveChangesAsync(CancellationToken ct = default);
    Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default);
}
```

### 3.3 Service Layer - BUSINESS LOGIC

#### 3.3.1 `IPortfolioTransactionService` — Serce systemu
**Plik:** `backend/TradingPlatform.Core/Interfaces/IPortfolioTransactionService.cs` (DO STWORZENIA)

```csharp
public interface IPortfolioTransactionService
{
    /// <summary>
    /// BUY workflow: Frontend podaje symbol, ilość, i cenę którą widział.
    /// Backend waliduje cenę z WebSocket'a, wykonuje transakcję atomowo.
    /// </summary>
    Task<CryptoTransactionDto> BuyCryptoAsync(
        Guid userId,
        string symbol,
        decimal quantity,
        decimal requestedPrice,          // Cena z frontendu
        decimal? tolerancePercent = 1m,  // % tolerancji (1%)
        CancellationToken ct = default);

    /// <summary>
    /// SELL workflow: Sprze daż pozycję.
    /// </summary>
    Task<CryptoTransactionDto> SellCryptoAsync(
        Guid userId,
        string symbol,
        decimal quantity,
        decimal requestedPrice,
        decimal? tolerancePercent = 1m,
        CancellationToken ct = default);

    /// <summary>
    /// Historia transakcji użytkownika (paged).
    /// </summary>
    Task<PaginatedResult<CryptoTransactionDto>> GetTransactionHistoryAsync(
        Guid userId,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default);

    /// <summary>
    /// Portfolio użytkownika z aktualnymi cenami i P&L.
    /// </summary>
    Task<IEnumerable<PortfolioPositionDto>> GetPortfolioAsync(
        Guid userId,
        CancellationToken ct = default);
}
```

#### 3.3.2 `PortfolioTransactionService` — Implementacja
**Plik:** `backend/TradingPlatform.Core/Services/PortfolioTransactionService.cs` (DO STWORZENIA)

**Dependencje:**
- `IAccountRepository` — do pobierania i aktualizacji salda
- `IPositionRepository` — do zarządzania pozycjami (DO STWORZENIA)
- `ICryptoTransactionRepository` — do logowania transakcji
- `IBinanceWebSocketService` — do pobrania ostatniej ceny z WebSocket'a
- `IInstrumentService` — do walidacji instrumentu
- `ILogger<PortfolioTransactionService>` — logging

**Logika BUY:**

```
1. Pobierz AccountEntity (userId) + walidacja Currency
2. Sprawdź czy instrument istnieje (InstrumentType = Crypto/Stock/Cfd)
3. Pobierz executionPrice = await binanceWebSocket.GetLatestPriceAsync(symbol)
4. Waliduj cenę:
   - diff = Math.Abs(requestedPrice - executionPrice)
   - if (diff > requestedPrice * tolerancePercent / 100)
       → Reject(RejectionReason="Cena spadła o X%")
5. Oblicz totalValue = quantity × executionPrice
6. Sprawdź saldo: if (account.AvailableBalance < totalValue)
       → Reject("Brak wystarczających środków")
7. Otwórz DB transaction:
   - Pobierz Position (lub utwórz nową)
   - Aktualizuj AvgPrice: newAvgPrice = (oldQty*oldPrice + qty*executionPrice) / (oldQty + qty)
   - Zmniejsz Account.AvailableBalance o totalValue
   - Utwórz CryptoTransactionEntity(Status=Completed, ExecutionPrice=...)
   - Utwórz AuditLogEntity (jeśli wymagane)
8. Commit
9. Zwróć CryptoTransactionDto
```

**Logika SELL:**

```
1. Pobierz Position (userId, symbol)
   - if (position.Quantity < quantity) → Reject("Brak wystarczającej ilości")
2. Pobierz executionPrice z WebSocket
3. Waliduj cenę (jak wyżej)
4. Oblicz totalValue = quantity × executionPrice
5. Otwórz DB transaction:
   - Zmniejsz Position.Quantity o quantity
   - Jeśli Quantity == 0: opcjonalnie zamknij pozycję (ClosedAtUtc, ClosedPrice)
   - Zwiększ Account.AvailableBalance o totalValue
   - Utwórz CryptoTransactionEntity(Status=Completed, Type=Sell, ...)
   - Utwórz AuditLogEntity
6. Commit
7. Zwróć CryptoTransactionDto
```

**Walidacja ceny — KLUCZOWY ALGORYTM:**

```csharp
private async Task<ValidationResult> ValidatePriceAsync(
    string symbol,
    decimal requestedPrice,
    decimal tolerancePercent,
    CancellationToken ct)
{
    var executionPrice = await _binanceWebSocket.GetLatestPriceAsync(symbol, ct);
    
    if (!executionPrice.HasValue)
        return ValidationResult.Fail("Nie udało się pobrać ceny z rynku");
    
    var diff = Math.Abs(requestedPrice - executionPrice.Value);
    var tolerance = requestedPrice * (tolerancePercent / 100m);
    
    if (diff > tolerance)
        return ValidationResult.Fail(
            $"Cena zmieniła się. Było: {requestedPrice}, Teraz: {executionPrice.Value:F2}. " +
            $"Różnica {diff:F2} > tolerancja {tolerance:F2}");
    
    return ValidationResult.Success(executionPrice.Value);
}
```

**Concurrency Control — Optimistic Locking:**

```csharp
try
{
    await _accountRepository.UpdateWithConcurrencyCheckAsync(
        updatedAccount,
        originalRowVersion,
        ct);
}
catch (DbUpdateConcurrencyException)
{
    throw new ConcurrencyException("Saldo uległo zmianie. Spróbuj ponownie.");
}
```

---

## 4. API I ENDPOINTY - ARCHITEKTURA Z WALIDACJĄ CENY

### 4.1 `CryptoPortfolioController`
**Lokalizacja:** `backend/TradingPlatform.Api/Controllers/CryptoPortfolioController.cs` (DO STWORZENIA)  
**Route:** `/api/cryptoportfolio`  
**Auth:** `[Authorize]` — użytkownik musi być zalogowany

#### 4.1.1 `POST /api/cryptoportfolio/buy` — Kup kryptowalutę/akcje
**Cel:** Użytkownik kupuje określoną ilość instrumentu po cenie którą widział na stronie.

**Request Body:**
```json
{
  "symbol": "BTCUSDT",
  "quantity": 0.5,
  "requestedPrice": 42500.50,
  "tolerancePercent": 1.0
}
```

- `symbol` (string, required) → BTCUSDT, ETHUSDT, AAPL, etc.
- `quantity` (decimal, required) → ilość > 0
- `requestedPrice` (decimal, required) → **cena którą frontend wyświetlił użytkownikowi**
- `tolerancePercent` (decimal, optional, default=1.0) → jak duża zmiana ceny jest akceptowalna (%)

**Response - SUCCESS (200):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "accountId": "550e8400-e29b-41d4-a716-446655440111",
  "symbol": "BTCUSDT",
  "type": "Buy",
  "quantity": 0.5,
  "requestedPrice": 42500.50,
  "executionPrice": 42501.00,
  "totalValue": 21250.50,
  "currency": "USD",
  "status": "Completed",
  "executedAt": "2026-06-09T14:23:45.000Z",
  "message": "Zakupiono pomyślnie. Pozycja zostanie zaktualizowana w portfelu."
}
```

**Response - PRICE VALIDATION FAILED (400):**
```json
{
  "status": 400,
  "error": "PriceValidationFailed",
  "message": "Cena uległa zmianie. Była: 42500.50, Teraz: 43100.00. Różnica: 600 PLN (1.4%), limit tolerancji: 1%",
  "details": {
    "requestedPrice": 42500.50,
    "executionPrice": 43100.00,
    "priceDifference": 599.50,
    "priceDifferencePercent": 1.41,
    "tolerancePercent": 1.0,
    "recommendation": "Cenę podbiła się. Spróbuj ponownie z najnowszą ceną."
  }
}
```

**Response - INSUFFICIENT BALANCE (400):**
```json
{
  "status": 400,
  "error": "InsufficientBalance",
  "message": "Brak wystarczających środków. Potrzebujesz: 21250.50 USD, Masz: 5000.00 USD",
  "details": {
    "required": 21250.50,
    "available": 5000.00,
    "deficit": 16250.50
  }
}
```

**Response - INSTRUMENT NOT FOUND (404):**
```json
{
  "status": 404,
  "error": "InstrumentNotFound",
  "message": "Instrument INVALID nie istnieje w systemie"
}
```

---

#### 4.1.2 `POST /api/cryptoportfolio/sell` — Sprzedaj kryptowalutę/akcje
**Cel:** Użytkownik sprzedaje określoną ilość ze swojej pozycji.

**Request Body:**
```json
{
  "symbol": "ETHUSDT",
  "quantity": 1.5,
  "requestedPrice": 2450.75,
  "tolerancePercent": 1.0
}
```

**Response - SUCCESS (200):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "accountId": "550e8400-e29b-41d4-a716-446655440111",
  "symbol": "ETHUSDT",
  "type": "Sell",
  "quantity": 1.5,
  "requestedPrice": 2450.75,
  "executionPrice": 2450.00,
  "totalValue": 3675.00,
  "currency": "USD",
  "status": "Completed",
  "closedPositionId": null,
  "executedAt": "2026-06-09T14:24:30.000Z",
  "message": "Sprzedano pomyślnie. Saldo zostanie powiększone."
}
```

**Response - INSUFFICIENT QUANTITY (400):**
```json
{
  "status": 400,
  "error": "InsufficientQuantity",
  "message": "Brak wystarczającej ilości. Posiadasz: 0.5, Chcesz sprzedać: 1.5",
  "details": {
    "held": 0.5,
    "requested": 1.5,
    "deficit": 1.0
  }
}
```

---

#### 4.1.3 `GET /api/cryptoportfolio/positions` — Aktualne portfolio
**Cel:** Pobranie wszystkich pozycji użytkownika z aktualnymi cenami i P&L.

**Query Parameters:**
- `baseCurrency` (optional) → filtruj po pillar/walucie (e.g., "Crypto", "Stocks")

**Response - SUCCESS (200):**
```json
{
  "baseCurrency": "USD",
  "positions": [
    {
      "positionId": "550e8400-e29b-41d4-a716-446655440002",
      "symbol": "BTCUSDT",
      "instrumentType": "Crypto",
      "quantity": 0.5,
      "averageOpenPrice": 41000.00,
      "currentPrice": 42500.00,
      "totalValue": 21250.00,
      "profitLoss": 750.00,
      "profitLossPercent": 1.83,
      "openedAt": "2026-05-15T10:00:00.000Z",
      "updatedAt": "2026-06-09T14:00:00.000Z"
    },
    {
      "positionId": "550e8400-e29b-41d4-a716-446655440003",
      "symbol": "ETHUSDT",
      "instrumentType": "Crypto",
      "quantity": 2.0,
      "averageOpenPrice": 2300.00,
      "currentPrice": 2450.00,
      "totalValue": 4900.00,
      "profitLoss": 300.00,
      "profitLossPercent": 6.52,
      "openedAt": "2026-05-20T12:00:00.000Z",
      "updatedAt": "2026-06-09T14:00:00.000Z"
    }
  ],
  "portfolioSummary": {
    "totalPositionsValue": 26150.00,
    "totalInvested": 25100.00,
    "totalProfitLoss": 1050.00,
    "totalProfitLossPercent": 4.18
  }
}
```

---

#### 4.1.4 `GET /api/cryptoportfolio/transactions` — Historia transakcji
**Cel:** Paginowana historia wszystkich BUY/SELL transakcji użytkownika.

**Query Parameters:**
- `limit` (int, optional, default=50)
- `offset` (int, optional, default=0)
- `type` (string, optional) → "Buy" | "Sell" | ""
- `status` (string, optional) → "Completed" | "Failed" | "Rejected" | ""

**Request:**
```
GET /api/cryptoportfolio/transactions?limit=20&offset=0&type=Buy&status=Completed
```

**Response - SUCCESS (200):**
```json
{
  "transactions": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440004",
      "symbol": "BTCUSDT",
      "type": "Buy",
      "quantity": 0.5,
      "requestedPrice": 42500.50,
      "executionPrice": 42501.00,
      "priceDifference": 0.50,
      "totalValue": 21250.50,
      "currency": "USD",
      "status": "Completed",
      "requestedAt": "2026-06-09T14:23:00.000Z",
      "executedAt": "2026-06-09T14:23:45.000Z"
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440005",
      "symbol": "ETHUSDT",
      "type": "Sell",
      "quantity": 1.0,
      "requestedPrice": 2450.00,
      "executionPrice": 2449.50,
      "priceDifference": -0.50,
      "totalValue": 2449.50,
      "currency": "USD",
      "status": "Completed",
      "requestedAt": "2026-06-09T13:30:00.000Z",
      "executedAt": "2026-06-09T13:30:20.000Z"
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440006",
      "symbol": "XRPUSDT",
      "type": "Buy",
      "quantity": 100.0,
      "requestedPrice": 0.50,
      "executionPrice": 0.55,
      "priceDifference": 0.05,
      "totalValue": 55.00,
      "currency": "USD",
      "status": "Rejected",
      "rejectionReason": "Cena wzrosła o 10%, limit tolerancji: 1%",
      "requestedAt": "2026-06-09T12:00:00.000Z",
      "rejectedAt": "2026-06-09T12:00:05.000Z"
    }
  ],
  "pagination": {
    "limit": 20,
    "offset": 0,
    "total": 47,
    "hasMore": true
  }
}
```

---

### 4.2 Walidacja ceny — DOKŁADNE WYJAŚNIENIE

#### Frontend wysyła:
1. `symbol` — jaki instrument
2. `quantity` — ile kupić/sprzedać
3. **`requestedPrice` — Cena wyświetlona na stronie w wybranej walucie bazowej**

#### Backend robi:
1. Pobiera **ostatnią cenę z WebSocket Binance** (rzeczywista cena rynkowa w tym momencie)
   ```csharp
   var executionPrice = await _binanceWebSocket.GetLatestPriceAsync(symbol);
   ```

2. **Waliduje różnicę:**
   ```
   diff = |requestedPrice - executionPrice|
   tolerance = requestedPrice × (tolerancePercent / 100)
   
   if (diff > tolerance)
       REJECT("Cena się zmieniła")
   else
       ACCEPT(executionPrice)
   ```

3. **Jeśli zaakceptowana:** Używa `executionPrice` do rozliczenia transakcji (nie `requestedPrice`!)

4. **Jeśli odrzucona:** Zwraca `400 Bad Request` z informacją jak bardzo cena się zmieniła

#### Tolerancja — Wartości zalecane:
- **Kryptowaluta:** 1-2% (zmienność wysoka)
- **Akcje:** 0.5% (zmienność niska)
- **CFD:** 0.5-1% (zależy od instrumentu)
- **Fiat:** 0.1% lub 1 grosz absolutnie

---

### 4.3 WAŻNE: Frontend NIE LICZY `totalValue`

❌ **ŹRÓDŁO BŁĘDU:**
```javascript
// Frontend robić NIE POWINIEN!
totalValue = quantity * requestedPrice;  // ❌ ❌ ❌
```

✅ **PRAWIDŁOWO:**
```javascript
// Frontend wysyła tylko co widział:
request = {
    symbol: "BTCUSDT",
    quantity: 0.5,
    requestedPrice: 42500.50  // Cena z ekranu
}

// Backend liczy:
totalValue = quantity × executionPrice  // ✅ Zaakceptowana cena z WebSocket'a
```

---

## 5. SERVICE LAYER - LOGIKA BIZNESOWA

### 5.1 `IPortfolioTransactionService` — Interfejs
**Plik:** `backend/TradingPlatform.Core/Interfaces/IPortfolioTransactionService.cs` (DO STWORZENIA)

```csharp
public interface IPortfolioTransactionService
{
    /// <summary>
    /// Kup kryptowalutę/akcję/CFD.
    /// Frontend przesyła: symbol, ilość, cenę którą widział
    /// Backend: waliduje cenę z WebSocket'a, wykonuje atomowo
    /// </summary>
    Task<CryptoTransactionDto> BuyCryptoAsync(
        Guid userId,
        string symbol,
        decimal quantity,
        decimal requestedPrice,
        decimal? tolerancePercent = 1m,
        CancellationToken ct = default);

    /// <summary>
    /// Sprzedaj kryptowalutę/akcję/CFD ze swojej pozycji.
    /// </summary>
    Task<CryptoTransactionDto> SellCryptoAsync(
        Guid userId,
        string symbol,
        decimal quantity,
        decimal requestedPrice,
        decimal? tolerancePercent = 1m,
        CancellationToken ct = default);

    /// <summary>
    /// Historia transakcji użytkownika (paged).
    /// </summary>
    Task<PaginatedResult<CryptoTransactionDto>> GetTransactionHistoryAsync(
        Guid userId,
        int limit = 50,
        int offset = 0,
        TransactionType? filterType = null,
        TransactionStatus? filterStatus = null,
        CancellationToken ct = default);

    /// <summary>
    /// Portfolio użytkownika z aktualnymi cenami i P&L.
    /// </summary>
    Task<PortfolioSummaryDto> GetPortfolioAsync(
        Guid userId,
        CancellationToken ct = default);
}
```

### 5.2 `PortfolioTransactionService` — Implementacja
**Plik:** `backend/TradingPlatform.Core/Services/PortfolioTransactionService.cs` (DO STWORZENIA)

**Dependencje (injection):**
```csharp
public sealed class PortfolioTransactionService : IPortfolioTransactionService
{
    private readonly IAccountRepository _accountRepository;
    private readonly IPositionRepository _positionRepository;  // DO STWORZENIA
    private readonly ICryptoTransactionRepository _transactionRepository;  // DO STWORZENIA
    private readonly IBinanceWebSocketService _binanceWebSocket;  // ISTNIEJĄCY
    private readonly IInstrumentService _instrumentService;  // ISTNIEJĄCY
    private readonly IAuditLogRepository _auditLog;  // ISTNIEJĄCY
    private readonly ILogger<PortfolioTransactionService> _logger;
    
    public PortfolioTransactionService(
        IAccountRepository accountRepository,
        IPositionRepository positionRepository,
        ICryptoTransactionRepository transactionRepository,
        IBinanceWebSocketService binanceWebSocket,
        IInstrumentService instrumentService,
        IAuditLogRepository auditLog,
        ILogger<PortfolioTransactionService> logger)
    {
        _accountRepository = accountRepository;
        _positionRepository = positionRepository;
        _transactionRepository = transactionRepository;
        _binanceWebSocket = binanceWebSocket;
        _instrumentService = instrumentService;
        _auditLog = auditLog;
        _logger = logger;
    }
```

#### 5.2.1 BUY Workflow — Szczegółowy flowchart

```
┌─────────────────────────────────────────────────────────────┐
│ BUY REQUEST: { userId, symbol, quantity, requestedPrice }  │
└──────────────────────┬──────────────────────────────────────┘
                       ↓
    ┌─────────────────────────────────────────┐
    │ 1. Pobierz Account i waliduj             │
    │    - czy userId istnieje                 │
    │    - czy account.AvailableBalance > 0    │
    │    - currency bazowa konta               │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 2. Waliduj Instrument                    │
    │    - czy symbol istnieje                 │
    │    - czy status == Approved              │
    │    - czy typ pasuje do pillar'u          │
    │    - czy nie jest zablokowany             │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 3. Pobierz cenę z WebSocket              │
    │    executionPrice =                      │
    │    await BinanceWS.GetLatestPrice()      │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 4. WALIDUJ CENĘ                          │
    │    diff = |requestedPrice - execPrice|   │
    │    tolerance = reqPrice × (tol% / 100)   │
    │                                           │
    │    if (diff > tolerance)                  │
    │       ❌ REJECT + Przyczynę              │
    │    else                                   │
    │       ✅ Zaakceptuj execPrice            │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 5. Oblicz wartość                        │
    │    totalValue = quantity × execPrice     │
    │    commission = totalValue × 0.1% (opcj) │
    │    finalValue = totalValue + commission  │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 6. Waliduj saldo                         │
    │    if (account.AvailableBalance < value) │
    │       ❌ REJECT "Brak środków"           │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 7. OTWÓRZ DB TRANSACTION                 │
    │                                           │
    │    7a. Pobierz Position (userId, symbol) │
    │        - jeśli istnieje: aktualizuj      │
    │        - jeśli nie: utwórz nową          │
    │                                           │
    │    7b. Oblicz nową średnią cenę:         │
    │        oldQty, oldPrice, oldValue        │
    │        newQty = oldQty + quantity        │
    │        newAvgPrice = (oldValue + value)/ │
    │                      newQty              │
    │        newPosition.Quantity = newQty     │
    │        newPosition.AvgOpenPrice = avg    │
    │                                           │
    │    7c. Aktualizuj Account:                │
    │        account.AvailableBalance -= value │
    │        (z concurrency check - RowVersion)│
    │                                           │
    │    7d. Utwórz CryptoTransactionEntity:   │
    │        - Type = Buy                      │
    │        - RequestedPrice = requestedPrice │
    │        - ExecutionPrice = execPrice      │
    │        - TotalValue = value              │
    │        - Status = Completed              │
    │        - RequestedAtUtc = now            │
    │        - ExecutedAtUtc = now             │
    │                                           │
    │    7e. Utwórz AuditLogEntity (opcjonalnie│
    │        - Action = "BUY"                  │
    │        - Amount = value                  │
    │        - Symbol = symbol                 │
    │                                           │
    │    7f. COMMIT TRANSACTION                │
    │        - jeśli DbUpdateConcurrencyEx:    │
    │          → Retry lub błąd                │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 8. Zwróć DTO                             │
    │    CryptoTransactionDto {                │
    │      id, symbol, type, quantity,         │
    │      requestedPrice, executionPrice,     │
    │      totalValue, status, executedAt      │
    │    }                                      │
    └─────────────────────────────────────────┘
```

#### 5.2.2 SELL Workflow — Szczegółowy flowchart

```
┌─────────────────────────────────────────────────────────────┐
│ SELL REQUEST: { userId, symbol, quantity, requestedPrice } │
└──────────────────────┬──────────────────────────────────────┘
                       ↓
    ┌─────────────────────────────────────────┐
    │ 1. Pobierz Account                       │
    │    - czy userId istnieje                 │
    │    - currency bazowa                     │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 2. Pobierz Position                      │
    │    - czy istnieje (userId, symbol)       │
    │    - czy Quantity >= requestedQuantity   │
    │    - jeśli Quantity=0: ❌ REJECT         │
    │      "Nie posiadasz tej pozycji"        │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 3. Pobierz cenę z WebSocket              │
    │    executionPrice =                      │
    │    await BinanceWS.GetLatestPrice()      │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 4. WALIDUJ CENĘ (jak w BUY)              │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 5. Oblicz wartość                        │
    │    totalValue = quantity × execPrice     │
    │    (minus prowizja jeśli dotyczy)        │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 6. OTWÓRZ DB TRANSACTION                 │
    │                                           │
    │    6a. Zmniejsz Position.Quantity:       │
    │        position.Quantity -= quantity     │
    │                                           │
    │        if (position.Quantity == 0):      │
    │          - ClosedAtUtc = now             │
    │          - ClosedPrice = execPrice       │
    │          - opcjonalnie: usuń lub archive │
    │                                           │
    │    6b. Zwiększ Account.AvailableBalance: │
    │        account.AvailableBalance += value │
    │        (z concurrency check)             │
    │                                           │
    │    6c. Utwórz CryptoTransactionEntity:   │
    │        - Type = Sell                     │
    │        - RequestedPrice = requestedPrice │
    │        - ExecutionPrice = execPrice      │
    │        - TotalValue = value              │
    │        - Status = Completed              │
    │                                           │
    │    6d. Utwórz AuditLogEntity             │
    │        - Action = "SELL"                 │
    │        - Amount = value                  │
    │                                           │
    │    6e. COMMIT TRANSACTION                │
    └──────────────┬──────────────────────────┘
                   ↓
    ┌─────────────────────────────────────────┐
    │ 7. Zwróć DTO                             │
    └─────────────────────────────────────────┘
```

#### 5.2.3 Walidacja ceny — Kluczowa metoda

```csharp
private async Task<ValidationResult> ValidatePriceAsync(
    string symbol,
    decimal requestedPrice,
    decimal tolerancePercent,
    CancellationToken ct)
{
    // Pobierz cenę z WebSocket'a (real-time)
    var executionPrice = await _binanceWebSocket.GetLatestPriceAsync(symbol, ct);
    
    if (!executionPrice.HasValue)
    {
        _logger.LogError("Nie można pobrać ceny z WebSocket dla {Symbol}", symbol);
        return ValidationResult.Fail("Nie udało się pobrać ceny z rynku. Spróbuj ponownie.");
    }
    
    // Wylicz różnicę
    var diff = Math.Abs(requestedPrice - executionPrice.Value);
    var tolerance = requestedPrice * (tolerancePercent / 100m);
    
    _logger.LogInformation(
        "Walidacja ceny: Symbol={Symbol}, Requested={Requested}, Execution={Execution}, Diff={Diff}, Tolerance={Tolerance}",
        symbol, requestedPrice, executionPrice, diff, tolerance);
    
    // Porównaj z tolerancją
    if (diff > tolerance)
    {
        var differencePercent = (diff / requestedPrice) * 100m;
        var rejectionReason = diff > requestedPrice ? "Cena spadła" : "Cena wzrosła";
        
        _logger.LogWarning(
            "Cena za bardzo się zmieniła. {Reason} o {Percent}%, limit {Limit}%",
            rejectionReason, differencePercent, tolerancePercent);
        
        return ValidationResult.Fail(
            $"Cena zmieniła się. Była: {requestedPrice:F2}, Teraz: {executionPrice.Value:F2}. " +
            $"Różnica: {diff:F2} ({differencePercent:F2}%), limit tolerancji: {tolerancePercent}%");
    }
    
    // OK - zwróć zaakceptowaną cenę
    return ValidationResult.Success(executionPrice.Value);
}
```

#### 5.2.4 Concurrency Control — Optimistic Locking

```csharp
private async Task UpdateAccountWithOptimisticLockingAsync(
    AccountEntity account,
    decimal deltaBalance,
    byte[] originalRowVersion,
    CancellationToken ct)
{
    // Zmień saldo
    account.AvailableBalance += deltaBalance;
    
    try
    {
        // EF Core będzie porównywać RowVersion
        await _accountRepository.UpdateWithConcurrencyCheckAsync(
            account,
            originalRowVersion,
            ct);
    }
    catch (DbUpdateConcurrencyException ex)
    {
        _logger.LogError(ex, "Concurrency conflict na Account {AccountId}", account.Id);
        throw new ConcurrencyException(
            "Saldo lub pozycja uległa zmianie. Spróbuj ponownie.", ex);
    }
}
```

### 5.3 Repozytorium dla nowych tabel

#### 5.3.1 `IPositionRepository` — DO STWORZENIA
**Plik:** `backend/TradingPlatform.Core/Interfaces/IPositionRepository.cs`

```csharp
public interface IPositionRepository
{
    Task<PositionEntity?> GetByAccountAndSymbolAsync(
        Guid accountId, 
        Guid instrumentId, 
        CancellationToken ct = default);
    
    Task<IEnumerable<PositionEntity>> GetByAccountIdAsync(
        Guid accountId, 
        CancellationToken ct = default);
    
    Task AddAsync(PositionEntity position, CancellationToken ct = default);
    
    Task UpdateAsync(PositionEntity position, CancellationToken ct = default);
    
    Task UpdateWithConcurrencyCheckAsync(
        PositionEntity position,
        byte[] expectedRowVersion,
        CancellationToken ct = default);
    
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

#### 5.3.2 `ICryptoTransactionRepository` — DO STWORZENIA
**Plik:** `backend/TradingPlatform.Core/Interfaces/ICryptoTransactionRepository.cs`

```csharp
public interface ICryptoTransactionRepository
{
    Task AddAsync(CryptoTransactionEntity transaction, CancellationToken ct = default);
    
    Task<CryptoTransactionEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    
    Task<IEnumerable<CryptoTransactionEntity>> GetByAccountIdAsync(
        Guid accountId,
        int limit = 50,
        int offset = 0,
        CancellationToken ct = default);
    
    Task<int> GetCountByAccountIdAsync(Guid accountId, CancellationToken ct = default);
    
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

### 5.4 Integracja w DI container

**Plik:** `backend/TradingPlatform.Data/Extensions/ServiceCollectionExtensions.cs` (ROZSZERZENIE)

```csharp
public static IServiceCollection AddTradingPlatformData(this IServiceCollection services)
{
    // ... istniejące ...
    
    // ✅ NOWE: Serwis portfolio
    services.AddScoped<IPortfolioTransactionService, PortfolioTransactionService>();
    
    // ✅ NOWE: Repozytoria
    services.AddScoped<IPositionRepository, SqlPositionRepository>();
    services.AddScoped<ICryptoTransactionRepository, SqlCryptoTransactionRepository>();
    
    return services;
}
```

---

## 6. WAŻNE ZASADY ARCHITEKTONICZNE

### 6.1 Multi-currency design
✅ System jest już przygotowany na multi-currency.

Każde `AccountEntity` ma swoją `Currency` (USD, EUR, PLN, GBP, etc.).
W przyszłości będzie można dodać subkonta dla użytkownika w innych walutach.

```
User
├── Main Account (Currency: USD, Pillar: General)
│   ├── Position: BTCUSDT
│   ├── Position: AAPL
│   └── Position: EURCFD
├── Subaccount (Currency: EUR, Pillar: General)  ← PRZYSZŁOŚĆ
│   ├── Position: EURUSD CFD
│   └── Position: GOLD
```

### 6.2 `PositionEntity` = uniwersalna pozycja
Nie tworzymy nowych tabel dla każdego instrumentu.

Jedna tabela `Positions` z nullable polami dla:
- Crypto: `Quantity`, `AverageOpenPrice`
- CFD: `Quantity`, `AverageOpenPrice`, `Leverage`, `StopPrice`, `TakeProfitPrice`
- Akcje: `Quantity`, `AverageOpenPrice`

Typ instrumentu określa z `InstrumentEntity.Type`.

### 6.3 `CryptoTransactionEntity` = obowiązkowy Trading Ledger
Bez tej tabeli nie będzie:
- Activity panelu dla użytkownika
- Statementów i raportów
- Historii transakcji
- Audytu
- Dokumentacji dla podatków

**Każda transakcja BUY/SELL musi być zalogowana!**

### 6.4 Frontend podaje cenę, Backend waliduje

❌ **BŁĘDY, które mogą się zdarzyć:**

1. **Frontend liczy totalValue** → może się myślić co do ceny
2. **Frontend wysyła hardcoded cenę** → nieaktualna
3. **Backend akceptuje każdą cenę** → użytkownik może być pokrzywdzony
4. **Brak tolerancji cenowej** → systemo będzie odrzucał prawidłowe zlecenia

✅ **PRAWIDŁOWO:**

1. **Frontend wysyła** `{ symbol, quantity, requestedPrice }`
   - `requestedPrice` = cena widoczna na ekranie (z wybraną walutą)
2. **Backend pobiera** ostatnią cenę z WebSocket'a (real-time)
3. **Backend waliduje** czy różnica jest w tolerancji
4. **Backend używa** `executionPrice` do rozliczenia (NIE `requestedPrice`!)
5. **Backend zwraca** dokładne informacje o walidacji

```csharp
// ✅ PRAWIDŁOWY FLOW
decimal requestedPrice = 42500.50m;  // Frontend
decimal executionPrice = 42501.00m;  // WebSocket
decimal tolerance = 1m;              // 1%

decimal diff = Math.Abs(requestedPrice - executionPrice);
decimal maxDiff = requestedPrice * (tolerance / 100m);

if (diff > maxDiff)
    REJECT();  // Cena się za bardzo zmieniła
else
    ACCEPT(executionPrice);  // Używaj actualnej ceny!
```

### 6.5 Atomiczność = jeden DB transaction
Wszystkie zmiany (Account balance, Position, CryptoTransaction, AuditLog) muszą być w jednej transakcji.

```
BEGIN TRANSACTION
  UPDATE Accounts SET AvailableBalance = ... WHERE Id = ...
  UPDATE Positions SET Quantity = ... WHERE Id = ...
  INSERT INTO CryptoTransactions VALUES (...)
  INSERT INTO AuditLogs VALUES (...)
COMMIT
```

Jeśli jedno się nie uda → ROLLBACK całej transakcji.

### 6.6 Concurrency Control = RowVersion
Chronimy się przed race conditions (dwukrotny BUY tego samego zlecenia).

```csharp
// Optimistic Locking z EF Core
builder.Property(x => x.RowVersion)
    .IsRowVersion();  // AUTO-increment przy każdej zmianie

// Przy aktualizacji
accountEntity.AvailableBalance -= totalValue;
await context.SaveChangesAsync();  // Jeśli RowVersion się nie zgadza
                                    // → DbUpdateConcurrencyException
```

Jeśli `DbUpdateConcurrencyException`:
- Retry z backoff (exponential)
- LUB zwróć użytkownikowi: "Saldo się zmieniło, spróbuj ponownie"

### 6.7 Tolerancja cenowa — Wartości zalecane

| Instrumenty | Tolerancja | Uzasadnienie |
|---|---|---|
| Kryptowaluta (BTCUSDT, ETHUSDT) | 1-2% | Wysoka zmienność, 24/7 |
| Akcje (AAPL, GOOGL) | 0.5% | Niska zmienność, sesja |
| CFD/Forex (EURUSD) | 0.5% | Dźwignia, dlatego niżej |
| Fiat (wpłata/wypłata) | 0.1% lub 1 grosz | Zero tolerancji praktycznie |

**Uwaga:** Tolerancja to **maksymalna akceptowalna zmiana**, nie zalecana zmiana!

### 6.8 Walidacja w backendzie — Multi-level

1. **Format/Type validation**
   - czy `quantity > 0`
   - czy `symbol` to string
   - czy `requestedPrice > 0`

2. **Business logic validation**
   - czy instrument istnieje i jest dostępny
   - czy konto ma saldo
   - czy pozycja istnieje (dla SELL)

3. **Price validation**
   - czy cena z WebSocket'a zgadza się z frontend'ową
   - czy różnica jest w tolerancji

4. **State validation**
   - czy `Account.RowVersion` się nie zmienił (concurrency)
   - czy `Position.RowVersion` się nie zmienił

### 6.9 Błędy — Standardowe HTTP codes

| Kod | Scenariusz | Komunikat |
|---|---|---|
| 400 | Price validation failed | "Cena zmieniła się..." |
| 400 | Insufficient balance | "Brak wystarczających środków..." |
| 400 | Insufficient quantity | "Brak wystarczającej ilości..." |
| 404 | Instrument not found | "Symbol nie istnieje" |
| 409 | Concurrency conflict | "Saldo uległo zmianie, spróbuj ponownie" |
| 500 | Server error | "Błąd systemu, skontaktuj się z supportem" |

---

## 7. PROPOZYCJA DOKŁADNYCH ENDPOINTÓW

### Tabela Endpointów z Walidacją Ceny

| Endpoint | Metoda | Cel | Request | Walidacja | Response (Success) |
|---|---|---|---|---|---|
| `/api/cryptoportfolio/buy` | POST | Kup instrument | `{ symbol, quantity, requestedPrice, tolerancePercent }` | Cena z WebSocket, saldo, instrument | 200: `CryptoTransactionDto` |
| `/api/cryptoportfolio/sell` | POST | Sprzedaj instrument | `{ symbol, quantity, requestedPrice, tolerancePercent }` | Cena z WebSocket, ilość, pozycja | 200: `CryptoTransactionDto` |
| `/api/cryptoportfolio/positions` | GET | Portfolio użytkownika | - | userId z JWT | 200: `PortfolioSummaryDto` |
| `/api/cryptoportfolio/transactions` | GET | Historia transakcji | `?limit=50&offset=0&type=&status=` | userId, paginacja | 200: `PaginatedResult<CryptoTransactionDto>` |

---

### Error Responses — Standaryzowane

#### Price Validation Failed (400)
```json
{
  "status": 400,
  "error": "PriceValidationFailed",
  "message": "Cena zmieniła się. Była: 42500.50, Teraz: 43100.00. Różnica: 600 PLN (1.4%), limit tolerancji: 1%",
  "details": {
    "requestedPrice": 42500.50,
    "executionPrice": 43100.00,
    "priceDifference": 599.50,
    "priceDifferencePercent": 1.41,
    "tolerancePercent": 1.0
  }
}
```

#### Insufficient Balance (400)
```json
{
  "status": 400,
  "error": "InsufficientBalance",
  "message": "Brak wystarczających środków. Potrzebujesz: 21250.50 USD, Masz: 5000.00 USD, Brakuje: 16250.50 USD"
}
```

#### Insufficient Quantity (400)
```json
{
  "status": 400,
  "error": "InsufficientQuantity",
  "message": "Brak wystarczającej ilości. Posiadasz: 0.5, Chcesz sprzedać: 1.5, Brakuje: 1.0"
}
```

#### Instrument Not Found (404)
```json
{
  "status": 404,
  "error": "InstrumentNotFound",
  "message": "Instrument INVALID nie istnieje w systemie lub jest zablokowany"
}
```

#### Position Not Found (404)
```json
{
  "status": 404,
  "error": "PositionNotFound",
  "message": "Nie posiadasz pozycji w INVALID"
}
```

#### Concurrency Conflict (409)
```json
{
  "status": 409,
  "error": "ConcurrencyConflict",
  "message": "Saldo lub pozycja uległa zmianie. Spróbuj ponownie.",
  "retryable": true
}
```

---

### Szczegółowe Request/Response Payloady

#### POST /api/cryptoportfolio/buy

**Request:**
```json
{
  "symbol": "BTCUSDT",
  "quantity": 0.5,
  "requestedPrice": 42500.50,
  "tolerancePercent": 1.0
}
```

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "accountId": "550e8400-e29b-41d4-a716-446655440111",
  "symbol": "BTCUSDT",
  "instrumentType": "Crypto",
  "type": "Buy",
  "quantity": 0.5,
  "requestedPrice": 42500.50,
  "executionPrice": 42501.00,
  "priceDifference": 0.50,
  "priceDifferencePercent": 0.001,
  "totalValue": 21250.50,
  "commission": 0.00,
  "currency": "USD",
  "status": "Completed",
  "requestedAt": "2026-06-09T14:23:00Z",
  "executedAt": "2026-06-09T14:23:45Z"
}
```

---

#### POST /api/cryptoportfolio/sell

**Request:**
```json
{
  "symbol": "ETHUSDT",
  "quantity": 1.5,
  "requestedPrice": 2450.75,
  "tolerancePercent": 1.0
}
```

**Response (200 OK):**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440001",
  "accountId": "550e8400-e29b-41d4-a716-446655440111",
  "symbol": "ETHUSDT",
  "type": "Sell",
  "quantity": 1.5,
  "requestedPrice": 2450.75,
  "executionPrice": 2450.00,
  "priceDifference": -0.75,
  "totalValue": 3675.00,
  "currency": "USD",
  "status": "Completed",
  "closedPositionId": null,
  "executedAt": "2026-06-09T14:24:30Z"
}
```

---

#### GET /api/cryptoportfolio/positions

**Response (200 OK):**
```json
{
  "baseCurrency": "USD",
  "totalPortfolioValue": 26150.00,
  "totalInvested": 25100.00,
  "totalProfitLoss": 1050.00,
  "totalProfitLossPercent": 4.18,
  "positions": [
    {
      "positionId": "550e8400-e29b-41d4-a716-446655440002",
      "symbol": "BTCUSDT",
      "instrumentType": "Crypto",
      "quantity": 0.5,
      "averageOpenPrice": 41000.00,
      "currentPrice": 42500.00,
      "totalValue": 21250.00,
      "profitLoss": 750.00,
      "profitLossPercent": 1.83,
      "openedAt": "2026-05-15T10:00:00Z",
      "updatedAt": "2026-06-09T14:00:00Z"
    },
    {
      "positionId": "550e8400-e29b-41d4-a716-446655440003",
      "symbol": "ETHUSDT",
      "instrumentType": "Crypto",
      "quantity": 2.0,
      "averageOpenPrice": 2300.00,
      "currentPrice": 2450.00,
      "totalValue": 4900.00,
      "profitLoss": 300.00,
      "profitLossPercent": 6.52,
      "openedAt": "2026-05-20T12:00:00Z",
      "updatedAt": "2026-06-09T14:00:00Z"
    }
  ],
  "lastUpdated": "2026-06-09T14:25:00Z"
}
```

---

#### GET /api/cryptoportfolio/transactions?limit=20&offset=0&type=Buy

**Response (200 OK):**
```json
{
  "transactions": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440004",
      "symbol": "BTCUSDT",
      "type": "Buy",
      "quantity": 0.5,
      "requestedPrice": 42500.50,
      "executionPrice": 42501.00,
      "priceDifference": 0.50,
      "totalValue": 21250.50,
      "currency": "USD",
      "status": "Completed",
      "requestedAt": "2026-06-09T14:23:00Z",
      "executedAt": "2026-06-09T14:23:45Z"
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440005",
      "symbol": "ETHUSDT",
      "type": "Buy",
      "quantity": 2.0,
      "requestedPrice": 2300.00,
      "executionPrice": 2300.50,
      "priceDifference": 0.50,
      "totalValue": 4601.00,
      "currency": "USD",
      "status": "Completed",
      "requestedAt": "2026-05-20T12:00:00Z",
      "executedAt": "2026-05-20T12:00:30Z"
    }
  ],
  "pagination": {
    "limit": 20,
    "offset": 0,
    "total": 47,
    "hasMore": true
  }
}
```

---

## 8. WNIOSKI — CO JEST, CO BRAKUJE

### Już istnieje ✅
- **AccountEntity** (`backend/TradingPlatform.Data/Entities/AccountEntity.cs`) — konto z multi-currency support
- **PositionEntity** (`backend/TradingPlatform.Data/Entities/PositionEntity.cs`) — pozycje, wymaga rozszerzenia
- **InstrumentEntity** — katalog instrumentów
- **IBinanceWebSocketService** (`backend/TradingPlatform.Core/Interfaces/IBinanceWebSocketService.cs`) — pobranie ceny real-time ✅
- **AuditLogEntity** — audit logging
- **AccountService** (`backend/TradingPlatform.Core/Services/AccountService.cs`) — operacje na kontach
- **AccountController** (`backend/TradingPlatform.Api/Controllers/AccountController.cs`) — GET endpointy

### Brakuje ❌
- **CryptoTransactionEntity** — tabela do logowania BUY/SELL
- **IPositionRepository** — repozytorium do pozycji
- **ICryptoTransactionRepository** — repozytorium do transakcji
- **IPortfolioTransactionService** — główny serwis biznesowy
- **CryptoPortfolioController** — kontroler do tradingu
- **Migracja EF Core** — zmiana schematu bazy
- **Frontend integr  acja** — nowe endpointy w frontendzie

---

## 9. DALSZE KROKI (PRZEGLĄD)

1. ✅ Rozszerzyć `PositionEntity` o nullable pola (StopPrice, Leverage, etc.)
2. ✅ Dodać `RowVersion` do `AccountEntity` i `PositionEntity`
3. ✅ Stwórz `CryptoTransactionEntity`
4. ✅ Stwórz interfejsy repozytorium (`IPositionRepository`, `ICryptoTransactionRepository`)
5. ✅ Stwórz implementacje SQL repozytorium
6. ✅ Stwórz `IPortfolioTransactionService` interfejs
7. ✅ Zaimplementuj `PortfolioTransactionService` z walidacją ceny
8. ✅ Stwórz `CryptoPortfolioController`
9. ✅ Zarejestruj w DI container
10. ✅ Migracja EF Core
11. ✅ Testy unitowe i integracyjne
12. ✅ Frontend integracja

---

## 10. ZATWIERDZENIE

**Auditor:** GitHub Copilot  
**Data:** 2026-06-09  
**Status:** ✅ **Ready to Implement (v2 - precyzyjny)**

**Kluczowe zmiany w v2:**
- ✅ Multi-currency architecture
- ✅ Uniwersalna PositionEntity dla Crypto/Stocks/CFD
- ✅ **Frontend podaje cenę, Backend waliduje z tolerancją**
- ✅ Atomiczne transakcje z optimistic locking
- ✅ Dokładne flowy BUY/SELL z flowchartami
- ✅ Kompletne payloady Request/Response
- ✅ Konkretne ścieżki plików i dependencje

---

## 11. PLAN IMPLEMENTACYJNY — FAZOWO

### FAZA 1: Rozszerzenie modeli i migracje (1-2 dni)

#### 1.1 Rozszerz `PositionEntity`
**Plik:** `backend/TradingPlatform.Data/Entities/PositionEntity.cs`

Dodaj nullable pola:
```csharp
public decimal? StopPrice { get; set; }
public decimal? TakeProfitPrice { get; set; }
public decimal? Leverage { get; set; }
public decimal? Commission { get; set; }
public decimal? ClosedPrice { get; set; }
public DateTimeOffset? ClosedAtUtc { get; set; }
public byte[] RowVersion { get; set; }  // ✅ Optimistic locking
```

#### 1.2 Dodaj `RowVersion` do `AccountEntity`
**Plik:** `backend/TradingPlatform.Data/Entities/AccountEntity.cs`

```csharp
public byte[] RowVersion { get; set; }  // ✅ SQL timestamp dla concurrency
```

#### 1.3 Stwórz `CryptoTransactionEntity`
**Plik:** `backend/TradingPlatform.Data/Entities/CryptoTransactionEntity.cs` (NOWY)

```csharp
public sealed class CryptoTransactionEntity
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public Guid InstrumentId { get; set; }
    public TransactionType Type { get; set; }
    public decimal Quantity { get; set; }
    public decimal RequestedPrice { get; set; }
    public decimal ExecutionPrice { get; set; }
    public decimal PriceDifference { get; set; }
    public decimal PriceTolerancePercent { get; set; } = 1m;
    public decimal TotalValue { get; set; }
    public string Currency { get; set; }
    public TransactionStatus Status { get; set; }
    public string? RejectionReason { get; set; }
    public DateTimeOffset RequestedAtUtc { get; set; }
    public DateTimeOffset? ExecutedAtUtc { get; set; }
    public DateTimeOffset? RejectedAtUtc { get; set; }
    
    public AccountEntity Account { get; set; } = null!;
    public InstrumentEntity Instrument { get; set; } = null!;
}
```

#### 1.4 Stwórz EF Configuration dla nowych entitów
**Pliki:**
- `backend/TradingPlatform.Data/Configurations/PositionEntityConfiguration.cs` (UPDATE)
- `backend/TradingPlatform.Data/Configurations/CryptoTransactionEntityConfiguration.cs` (NOWY)

#### 1.5 Dodaj do DbContext
**Plik:** `backend/TradingPlatform.Data/Context/TradingPlatformDbContext.cs`

```csharp
public DbSet<CryptoTransactionEntity> CryptoTransactions => Set<CryptoTransactionEntity>();
```

#### 1.6 Migracja EF Core
```bash
cd backend
dotnet ef migrations add AddPortfolioTransactionModels
dotnet ef database update
```

**Weryfikacja:**
- Tabela `Positions` ma nowe kolumny
- Tabela `CryptoTransactions` została utworzona
- Kolumny `RowVersion` w `Accounts` i `Positions`

---

### FAZA 2: Repozytorium Layer (1 dzień)

#### 2.1 Stwórz `IPositionRepository`
**Plik:** `backend/TradingPlatform.Core/Interfaces/IPositionRepository.cs` (NOWY)

```csharp
public interface IPositionRepository
{
    Task<PositionEntity?> GetByAccountAndInstrumentAsync(
        Guid accountId, 
        Guid instrumentId, 
        CancellationToken ct = default);
    
    Task<IEnumerable<PositionEntity>> GetByAccountIdAsync(
        Guid accountId, 
        CancellationToken ct = default);
    
    Task AddAsync(PositionEntity position, CancellationToken ct = default);
    Task UpdateAsync(PositionEntity position, CancellationToken ct = default);
    Task UpdateWithConcurrencyCheckAsync(
        PositionEntity position,
        byte[] expectedRowVersion,
        CancellationToken ct = default);
    
    Task SaveChangesAsync(CancellationToken ct = default);
}
```

#### 2.2 Stwórz `SqlPositionRepository`
**Plik:** `backend/TradingPlatform.Data/Repositories/SqlPositionRepository.cs` (NOWY)

```csharp
public sealed class SqlPositionRepository : IPositionRepository
{
    private readonly TradingPlatformDbContext _dbContext;
    
    public SqlPositionRepository(TradingPlatformDbContext dbContext)
    {
        _dbContext = dbContext;
    }
    
    public async Task<PositionEntity?> GetByAccountAndInstrumentAsync(
        Guid accountId, 
        Guid instrumentId, 
        CancellationToken ct = default)
    {
        return await _dbContext.Positions
            .FirstOrDefaultAsync(
                x => x.AccountId == accountId && x.InstrumentId == instrumentId,
                ct);
    }
    
    // ... pozostałe metody ...
}
```

#### 2.3 Stwórz `ICryptoTransactionRepository`
**Plik:** `backend/TradingPlatform.Core/Interfaces/ICryptoTransactionRepository.cs` (NOWY)

#### 2.4 Stwórz `SqlCryptoTransactionRepository`
**Plik:** `backend/TradingPlatform.Data/Repositories/SqlCryptoTransactionRepository.cs` (NOWY)

#### 2.5 Rozszerz `IAccountRepository` o concurrency support
**Plik:** `backend/TradingPlatform.Core/Interfaces/IAccountRepository.cs` (UPDATE)

```csharp
Task UpdateWithConcurrencyCheckAsync(
    Account account,
    byte[] expectedRowVersion,
    CancellationToken ct = default);
```

#### 2.6 Zaktualizuj `SqlAccountRepository`
**Plik:** `backend/TradingPlatform.Data/Repositories/SqlAccountRepository.cs` (UPDATE)

Dodaj metodę `UpdateWithConcurrencyCheckAsync()` z obsługą `DbUpdateConcurrencyException`.

#### 2.7 Zarejestruj w DI
**Plik:** `backend/TradingPlatform.Data/Extensions/ServiceCollectionExtensions.cs` (UPDATE)

```csharp
services.AddScoped<IPositionRepository, SqlPositionRepository>();
services.AddScoped<ICryptoTransactionRepository, SqlCryptoTransactionRepository>();
```

---

### FAZA 3: Service Layer (2 dni)

#### 3.1 Stwórz `IPortfolioTransactionService`
**Plik:** `backend/TradingPlatform.Core/Interfaces/IPortfolioTransactionService.cs` (NOWY)

(Patrz sekcja 5.1 wyżej)

#### 3.2 Stwórz `PortfolioTransactionService` — BUY workflow
**Plik:** `backend/TradingPlatform.Core/Services/PortfolioTransactionService.cs` (NOWY)

Zaimplementuj:
- `BuyCryptoAsync()` z logką walidacji ceny
- `ValidatePriceAsync()` helper
- Update `PositionEntity` z nową średnią ceną
- Atomiczne DB transaction
- Optimistic locking

#### 3.3 Zaimplementuj SELL workflow
W tej samej klasie:
- `SellCryptoAsync()`
- Reduce quantity
- Increase balance

#### 3.4 Zaimplementuj `GetPortfolioAsync()`
- Pobierz pozycje
- Pobierz aktualne ceny z WebSocket'a
- Oblicz P&L

#### 3.5 Zaimplementuj `GetTransactionHistoryAsync()`
- Paginacja
- Filtrowanie po typie/statusie

#### 3.6 Zarejestruj w DI
**Plik:** `backend/TradingPlatform.Data/Extensions/ServiceCollectionExtensions.cs` (UPDATE)

```csharp
services.AddScoped<IPortfolioTransactionService, PortfolioTransactionService>();
```

---

### FAZA 4: API Controller (1 dzień)

#### 4.1 Stwórz `CryptoPortfolioController`
**Plik:** `backend/TradingPlatform.Api/Controllers/CryptoPortfolioController.cs` (NOWY)

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class CryptoPortfolioController : ControllerBase
{
    private readonly IPortfolioTransactionService _portfolioService;
    private readonly IMapper _mapper;
    private readonly ILogger<CryptoPortfolioController> _logger;
    
    [HttpPost("buy")]
    public async Task<IActionResult> Buy(BuyCryptoRequest request, CancellationToken ct)
    {
        var userId = GetUserIdFromToken();
        try
        {
            var result = await _portfolioService.BuyCryptoAsync(
                userId,
                request.Symbol,
                request.Quantity,
                request.RequestedPrice,
                request.TolerancePercent,
                ct);
            
            return Ok(_mapper.Map<CryptoTransactionDto>(result));
        }
        catch (PriceValidationException ex)
        {
            return BadRequest(new { error = "PriceValidationFailed", message = ex.Message });
        }
        catch (InsufficientBalanceException ex)
        {
            return BadRequest(new { error = "InsufficientBalance", message = ex.Message });
        }
        // ... więcej error handling ...
    }
    
    [HttpPost("sell")]
    public async Task<IActionResult> Sell(SellCryptoRequest request, CancellationToken ct)
    {
        // ... similar ...
    }
    
    [HttpGet("positions")]
    public async Task<IActionResult> GetPositions(CancellationToken ct)
    {
        var userId = GetUserIdFromToken();
        var result = await _portfolioService.GetPortfolioAsync(userId, ct);
        return Ok(_mapper.Map<PortfolioSummaryDto>(result));
    }
    
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        [FromQuery] string? type = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var userId = GetUserIdFromToken();
        var result = await _portfolioService.GetTransactionHistoryAsync(
            userId, limit, offset, null, null, ct);
        return Ok(_mapper.Map<PaginatedResult<CryptoTransactionDto>>(result));
    }
    
    private Guid GetUserIdFromToken()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim is not null && Guid.TryParse(userIdClaim.Value, out var userId))
            return userId;
        throw new UnauthorizedAccessException("Invalid user ID in token");
    }
}
```

#### 4.2 Stwórz Request/Response DTOs
**Plik:** `backend/TradingPlatform.Core/Dtos/CryptoPortfolioDto.cs` (NOWY)

```csharp
public sealed record BuyCryptoRequest(
    string Symbol,
    decimal Quantity,
    decimal RequestedPrice,
    decimal? TolerancePercent = 1m);

public sealed record SellCryptoRequest(
    string Symbol,
    decimal Quantity,
    decimal RequestedPrice,
    decimal? TolerancePercent = 1m);

public sealed record CryptoTransactionDto(
    Guid Id,
    Guid AccountId,
    string Symbol,
    TransactionType Type,
    decimal Quantity,
    decimal RequestedPrice,
    decimal ExecutionPrice,
    decimal PriceDifference,
    decimal TotalValue,
    string Currency,
    TransactionStatus Status,
    string? RejectionReason,
    DateTimeOffset RequestedAtUtc,
    DateTimeOffset? ExecutedAtUtc);

public sealed record PortfolioPositionDto(
    Guid PositionId,
    string Symbol,
    string InstrumentType,
    decimal Quantity,
    decimal AverageOpenPrice,
    decimal CurrentPrice,
    decimal TotalValue,
    decimal ProfitLoss,
    decimal ProfitLossPercent,
    DateTimeOffset OpenedAtUtc,
    DateTimeOffset? UpdatedAtUtc);

public sealed record PortfolioSummaryDto(
    string BaseCurrency,
    decimal TotalPortfolioValue,
    decimal TotalInvested,
    decimal TotalProfitLoss,
    decimal TotalProfitLossPercent,
    IEnumerable<PortfolioPositionDto> Positions,
    DateTimeOffset LastUpdated);
```

#### 4.3 Zarejestruj Mapper profile
**Plik:** `backend/TradingPlatform.Core/Mapping/MappingProfile.cs` (UPDATE)

Dodaj mappingi dla nowych DTO.

---

### FAZA 5: Testy (1-2 dni)

#### 5.1 Unit Testy
**Plik:** `backend/TradingPlatform.Tests/PortfolioTransactionServiceTests.cs` (NOWY)

Testy dla:
- BUY workflow (happy path, insufficient balance, price validation failed)
- SELL workflow
- Average price calculation
- Concurrency conflict handling

#### 5.2 Integration Testy
**Plik:** `backend/TradingPlatform.Tests/CryptoPortfolioControllerTests.cs` (NOWY)

Testy dla endpointów API.

#### 5.3 Database Testy
Weryfikacja `CryptoTransactionEntity` persistence.

---

### FAZA 6: Frontend Integracja (1-2 dni)

#### 6.1 Nowy Hook: `usePortfolio`
**Plik:** `frontend/src/hooks/usePortfolio.ts` (NOWY)

```typescript
export const usePortfolio = () => {
  const [portfolio, setPortfolio] = useState<PortfolioSummaryDto | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  
  const fetchPortfolio = async () => {
    // GET /api/cryptoportfolio/positions
  };
  
  useEffect(() => {
    fetchPortfolio();
  }, []);
  
  return { portfolio, loading, error, refetch: fetchPortfolio };
};
```

#### 6.2 Nowy Hook: `useTransactionHistory`
**Plik:** `frontend/src/hooks/useTransactionHistory.ts` (NOWY)

#### 6.3 Nowy Hook: `useBuySell`
**Plik:** `frontend/src/hooks/useBuySell.ts` (NOWY)

```typescript
export const useBuySell = () => {
  const buy = async (symbol: string, quantity: number, requestedPrice: number) => {
    // POST /api/cryptoportfolio/buy
  };
  
  const sell = async (symbol: string, quantity: number, requestedPrice: number) => {
    // POST /api/cryptoportfolio/sell
  };
  
  return { buy, sell };
};
```

#### 6.4 Zaktualizuj API Config
**Plik:** `frontend/src/config/apiConfig.ts` (UPDATE)

```typescript
cryptoportfolio: {
  buy: '/cryptoportfolio/buy',
  sell: '/cryptoportfolio/sell',
  positions: '/cryptoportfolio/positions',
  transactions: '/cryptoportfolio/transactions',
}
```

#### 6.5 Komponenty UI
- `PortfolioPanel` — widok portfolio z pozycjami
- `BuySellForm` — formularz kupna/sprzedaży
- `TransactionHistory` — historia transakcji

---

## 12. TIMELINE I RESOURCES

| Faza | Zadanie | Czas | Dependencje |
|---|---|---|---|
| 1 | Modele + Migracje | 1-2 dni | - |
| 2 | Repozytorium | 1 dzień | Faza 1 |
| 3 | Service Layer | 2 dni | Faza 1,2 |
| 4 | API Controller | 1 dzień | Faza 3 |
| 5 | Testy | 1-2 dni | Faza 4 |
| 6 | Frontend | 1-2 dni | Faza 4 |
| **RAZEM** | | **7-10 dni** | |

---

## 13. DEFINICJA "GOTOWE"

✅ Portfolio Trading System jest **GOTOWE** gdy:

1. Backend:
   - ✅ `CryptoTransactionEntity` przechowuje historię BUY/SELL
   - ✅ `PortfolioTransactionService` waliduje cenę z WebSocket
   - ✅ Atomiczne DB transactions (BEGIN/COMMIT)
   - ✅ Optimistic locking (RowVersion)
   - ✅ API endpointy: `/buy`, `/sell`, `/positions`, `/transactions`
   - ✅ Testy pokrywają happy path + error scenarios
   - ✅ Error handling (400, 404, 409)

2. Frontend:
   - ✅ Hook `usePortfolio` pobiera portfolio
   - ✅ Hook `useBuySell` wysyła requestedPrice i quantity
   - ✅ UI pokazuje portfolio z P&L
   - ✅ UI pokazuje historię transakcji
   - ✅ Error messages z backend'u wyświetlane użytkownikowi

3. Dokumentacja:
   - ✅ README zaktualizowane
   - ✅ API dokumentacja (Swagger)
   - ✅ Architektura dokumentowa w `/audits`

---

## 14. NOTES I UWAGI

### Uwagi do implementacji:

1. **Tolerancja cenowa:**
   - Frontend wysyła dokładną cenę którą widział
   - Backend weryfikuje z WebSocket'a z 1% tolerancją (customizable)
   - Jeśli cena za muito spadnie/wzrośnie → REJECT

2. **RowVersion:**
   - Automatycznie aktualizowana przez SQL Server na każdą zmianę
   - EF Core rzuca `DbUpdateConcurrencyException` jeśli nie zgadza się
   - Użytkownikowi: "Saldo uległo zmianie, spróbuj ponownie"

3. **Średnia cena (Average Open Price):**
   ```
   newAvgPrice = (oldQty × oldPrice + newQty × newPrice) / (oldQty + newQty)
   ```
   - Ważne dla SELL (ile zysku/straty)

4. **Multi-currency:**
   - Każde konto ma swoją walutę
   - Transakcje w walucie konta
   - W przyszłości: cross-currency z exchange rate

5. **Audit & Compliance:**
   - Każda transakcja w `CryptoTransactionEntity`
   - Opcjonalnie w `AuditLogEntity` (admin/audit trail)
   - Tax compliance: raport dla CPA  

---

## 11. PLAN IMPLEMENTACYJNY (ETAPY)

### Etap 1 – Model danych i migracje
1. Dodać `CryptoTransactionEntity` w `backend/TradingPlatform.Data.Entities`:
   - `AccountId`, `InstrumentId`, `Quantity`, `ExecutedPrice`, `TotalValue`, `Currency`, `TransactionType`, `Status`, `ExecutedAtUtc`, `CreatedAtUtc`, `RowVersion`.
   - Relacje `AccountEntity` i `PositionEntity`.
2. Rozszerzyć `TradingPlatformDbContext` o `DbSet<CryptoTransactionEntity>`.
3. Dodać `TransactionType` (`Buy`, `Sell`) i `TransactionStatus` (`Pending`, `Completed`, `Failed`).
4. Utworzyć migrację EF Core i zweryfikować schemat w lokalnej bazie.

### Etap 2 – Serwis biznesowy i logika transakcyjna
1. Dodać interfejs `ICryptoPortfolioService` w `backend/TradingPlatform.Core.Interfaces` z metodami:
   - `Task<CryptoTransactionDto> BuyAsync(Guid userId, BuyCryptoRequest request, CancellationToken token)`,
   - `Task<CryptoTransactionDto> SellAsync(Guid userId, SellCryptoRequest request, CancellationToken token)`,
   - `Task<IEnumerable<PositionDto>> GetPositionsAsync(Guid userId, CancellationToken token)`,
   - `Task<PaginatedResult<CryptoTransactionDto>> GetTransactionsAsync(Guid userId, TransactionQuery query, CancellationToken token)`.
2. Zaimplementować `CryptoPortfolioService` w `backend/TradingPlatform.Core.Services`:
   - pobierz konto fiat i pozycję dla użytkownika,
   - zweryfikuj `quantity > 0` i wystarczający `AvailableBalance` (dla BUY) / `Quantity` (dla SELL),
   - pobierz bieżącą cenę rynkową z `IMarketDataService` lub istniejącego feedu,
   - oblicz `totalValue`, utwórz `CryptoTransactionEntity`, zaktualizuj `AccountEntity` i `PositionEntity`,
   - zapisz wszystkie zmiany w jednej transakcji bazodanowej,
   - uwzględnij `RowVersion` lub inny mechanizm optimistic concurrency.
3. W logice SELL:
   - zdejmij ilość z `PositionEntity.Quantity`,
   - zwiększ `AccountEntity.AvailableBalance` o `totalValue`,
   - jeśli `Quantity == 0`, pozostaw pozycję z zerowym saldem lub usuń ją zgodnie ze strategią.
4. Dodaj walidację backendową i zapisuj `AuditLog` / `CryptoTransactionEntity` dla każdej operacji.

### Etap 3 – API walleta i prezentacja
1. Dodać `CryptoPortfolioController` w `backend/TradingPlatform.Api.Controllers`.
2. Utworzyć endpointy:
   - `POST /api/cryptoportfolio/buy`,
   - `POST /api/cryptoportfolio/sell`,
   - `GET /api/cryptoportfolio/positions`,
   - `GET /api/cryptoportfolio/transactions`.
3. Przekazać `userId` z JWT claims w kontrolerze, aby użytkownik widział tylko swoje dane.
4. Odpowiadać prostymi DTO: pozycja z `currentPrice`, `avgPrice`, `totalValue`, `pnl`; transakcja z `symbol`, `type`, `qty`, `price`, `total`, `status`, `executedAt`.
5. Zarejestrować `ICryptoPortfolioService` w `backend/TradingPlatform.Api.Extensions.ServiceCollectionExtensions`.

### Etap 4 – Testy i walidacja jakości
1. Dodać unit testy dla `CryptoPortfolioService.BuyAsync()` i `SellAsync()`.
2. Dodać integracyjne testy kontrolera dla pełnego przepływu BUY/SELL.
3. Sprawdzić przypadki brzegowe: brak środków, zbyt mała/ujemna ilość, brak pozycji do sprzedaży.
4. Przetestować migrację i nowy schemat w lokalnej bazie lub w kontenerze SQL.

### Etap 5 – Frontend i końcowe podłączenie
1. W frontendzie wywołać nowe endpointy walleta zamiast mechanizmów konwersji w chartach.
2. Dodać przycisk `Portfel` obok widocznego salda fiat, który przenosi użytkownika do panelu portfolio.
3. W widoku portfolio dodać przyciski `Buy` i `Sell` przy każdej pozycji oraz globalny przycisk otwierający formularz transakcji.
4. Na stronie pojedynczej kryptowaluty (`CryptoPage`) dodać dedykowane przyciski `Buy` i `Sell` oraz formularz z szybką walidacją ilości.
5. Zachować `CryptoController` tylko jako źródło historycznych candle / wykresów.
6. Upewnić się, że wycena portfela i tooltipy w `frontend/src/components/crypto/CryptoChart.tsx` są oparte na cenie backendowej/marketowej.
7. Dokumentować w `README` / `audits` nowe API i zachowanie walleta.

---

## 12. Najważniejsze zasady do wdrożenia
- `PositionEntity` pozostaje walutą crypto wallet.
- `AccountEntity` pozostaje fiat wallet.
- `CryptoTransactionEntity` daje audit + historię BUY/SELL.
- Backend sam pobiera cenę rynkową i sam ocenia transakcję.
- Transakcje muszą być atomiczne i chronione przed concurrency.
- Frontend tylko prezentuje i tworzy żądanie, nie decyduje o cenie.

## 13. Co należy zrobić dalej
W tym projekcie trzeba zaimplementować cały mechanizm kupna i sprzedaży kryptowalut w backendzie oraz front-end:
- pełny serwis `CryptoPortfolioService` z BUY/SELL workflow,
- `CryptoTransactionEntity` jako trading ledger,
- kontroler `CryptoPortfolioController` z endpointami `POST /api/cryptoportfolio/buy`, `POST /api/cryptoportfolio/sell`, `GET /api/cryptoportfolio/positions`, `GET /api/cryptoportfolio/transactions`,
- walidację ilości i salda fiat,
- pobieranie ceny z rynku / cache na backendzie i liczenie `totalValue` po stronie serwera,
- atomiczną obsługę transakcji w jednej transakcji DB,
- mechanizmy concurrency (RowVersion lub lock),
- UI: przycisk `Portfel` obok salda, przyciski `Buy`/`Sell` w portfolio oraz na `CryptoPage`,
- frontendowy formularz kupna/sprzedaży, który komunikuje się z nowymi endpointami walleta.

To jest kluczowe: bez tego system nie ma rzeczywistego kupna kryptowaluty — obecny kod ma tylko fundamenty, ale nie ma pełnej logiki transakcyjnej.

## 14. Podsumowanie braków na końcu audytu
W skrócie, w audycie brakuje nadal pełnego potwierdzenia implementacji transakcyjnego mechanizmu kupna/sprzedaży crypto oraz frontendowego interfejsu walleta.

Najważniejsze brakujące elementy:
- nie ma jeszcze prawdziwego BUY/SELL flow w backendzie,
- nie ma prawdziwej historii transakcji (`CryptoTransactionEntity`),
- nie ma API walleta do pozycji i transakcji,
- nie ma frontendowego panelu portfolio z przyciskami `Portfel`, `Buy`, `Sell`,
- nie ma gwarancji atomiczności i kontroli concurrency.

Dopóki te elementy nie będą zaimplementowane, projekt pozostaje planem architektonicznym, a nie działającym systemem kupna kryptowalut.

