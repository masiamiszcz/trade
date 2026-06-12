# LISTA DO ZROBIENIA: Buying Service Implementation

## � Struktura Projektu - Mapa Plików

```
trading_project/                                  ← ROOT FOLDER
├── backend/
│   ├── TradingPlatform.Api/                     ← API Layer
│   │   ├── Controllers/                         ← API Endpoints
│   │   │   ├── AccountController.cs             ✓ ISTNIEJĄCY
│   │   │   ├── TradingController.cs             🆕 DO STWORZENIA
│   │   │   └── ... (inne)
│   │   ├── Models/                              ← Response Models
│   │   ├── Program.cs                           ✓ ISTNIEJĄCY - DO EDYCJI (DI)
│   │   ├── Extensions/                          ← Service Extensions
│   │   └── appsettings.json                     ✓ ISTNIEJĄCY
│   │
│   ├── TradingPlatform.Core/                    ← Business Logic
│   │   ├── Interfaces/
│   │   │   ├── IAccountService.cs               ✓ ISTNIEJĄCY
│   │   │   ├── ITradingService.cs               🆕 DO STWORZENIA
│   │   │   ├── IAccountTransferRepository.cs    🆕 DO STWORZENIA
│   │   │   ├── IPositionRepository.cs           🆕 DO STWORZENIA
│   │   │   └── ... (inne)
│   │   ├── Services/
│   │   │   ├── AccountService.cs                ✓ ISTNIEJĄCY - DO EDYCJI
│   │   │   ├── TradingService.cs                🆕 DO STWORZENIA
│   │   │   └── ... (inne)
│   │   ├── Models/
│   │   │   ├── AuditLog.cs                      ✓ ISTNIEJĄCY - DO EDYCJI
│   │   │   └── ... (inne)
│   │   ├── Dtos/                                ← DTOs
│   │   │   ├── BuyOrderRequest.cs               🆕 DO STWORZENIA
│   │   │   ├── SellOrderRequest.cs              🆕 DO STWORZENIA
│   │   │   ├── TransferRequest.cs               🆕 DO STWORZENIA
│   │   │   ├── LockRequest.cs                   🆕 DO STWORZENIA
│   │   │   └── TransactionDto.cs                🆕 DO STWORZENIA
│   │   └── ... (inne)
│   │
│   ├── TradingPlatform.Data/                    ← Data Access Layer
│   │   ├── Repositories/
│   │   │   ├── SqlAccountRepository.cs          ✓ ISTNIEJĄCY - DO EDYCJI
│   │   │   ├── IAccountTransferRepository.cs    🆕 DO STWORZENIA
│   │   │   ├── SqlAccountTransferRepository.cs  🆕 DO STWORZENIA
│   │   │   ├── IPositionRepository.cs           🆕 DO STWORZENIA
│   │   │   ├── SqlPositionRepository.cs         🆕 DO STWORZENIA
│   │   │   ├── IAuditLogRepository.cs           ✓ ISTNIEJĄCY - DO EDYCJI
│   │   │   └── ... (inne)
│   │   ├── Entities/
│   │   │   ├── AccountEntity.cs                 ✓ ISTNIEJĄCY
│   │   │   ├── AccountTransferEntity.cs         ✓ ISTNIEJĄCY - DO EDYCJI
│   │   │   ├── PositionEntity.cs                ✓ ISTNIEJĄCY - DO EDYCJI
│   │   │   ├── AuditLogEntity.cs                ✓ ISTNIEJĄCY
│   │   │   └── ... (inne)
│   │   ├── Configurations/                      ← EF Configurations
│   │   │   ├── AccountTransferEntityConfiguration.cs      ✓ ISTNIEJĄCY - DO EDYCJI
│   │   │   ├── PositionEntityConfiguration.cs             ✓ ISTNIEJĄCY - DO EDYCJI
│   │   │   └── ... (inne)
│   │   ├── Context/
│   │   │   └── TradingPlatformDbContext.cs      ✓ ISTNIEJĄCY
│   │   ├── Migrations/                          ← EF Migrations
│   │   │   └── [TIMESTAMP]_AddTradingServiceSupport.cs    🆕 AUTO-GENERATED
│   │   ├── UnitOfWork/                          🆕 NOWY FOLDER
│   │   │   ├── IUnitOfWork.cs                   🆕 DO STWORZENIA
│   │   │   └── UnitOfWork.cs                    🆕 DO STWORZENIA
│   │   ├── Services/                            ← Optional Data Services
│   │   └── ... (inne)
│   │
│   └── TradingPlatform.Tests/                   ← Tests
│       ├── Integration/
│       │   ├── TradingServiceTests.cs           🆕 DO STWORZENIA
│       │   └── TradingControllerTests.cs        🆕 DO STWORZENIA
│       └── ... (inne)
│
├── frontend/
│   ├── src/
│   │   ├── hooks/
│   │   │   ├── useTransactions.ts               ✓ ISTNIEJĄCY - DO EDYCJI
│   │   │   └── ... (inne)
│   │   ├── components/
│   │   │   └── user/
│   │   │       ├── TransactionHistory.tsx       ✓ ISTNIEJĄCY - DO EDYCJI
│   │   │       └── ... (inne)
│   │   ├── services/
│   │   └── ... (inne)
│   ├── package.json                             ✓ ISTNIEJĄCY
│   └── ... (inne)
│
├── docker/
│   ├── docker-compose.yaml                      ✓ ISTNIEJĄCY - DO EDYCJI
│   └── ... (inne)
│
└── audits/
    ├── buyingservice/
    │   ├── audit.md                             ✓ ISTNIEJĄCY
    ├── BUYING_SERVICE_IMPLEMENTATION_PLAN.md    ✓ WŁAŚNIE CZYTASZ
    └── IMPLEMENTATION_VALIDATION.md             🆕 DO STWORZENIA
```

**Legenda:**
- ✓ ISTNIEJĄCY - plik już istnieje
- 🆕 DO STWORZENIA - trzeba utworzyć od zera
- DO EDYCJI - edytować istniejący plik
- AUTO-GENERATED - utworzony automatycznie przez EF

---

## �📋 Plan Implementacji Trading Service

---

## FAZA 1: Core Service Layer (Backend Services)

### 1. Create Trading Service Interface
**Plik:** `backend/TradingPlatform.Core/Interfaces/ITradingService.cs`

```csharp
public interface ITradingService
{
    Task<Result> ExecuteDepositAsync(Guid accountId, decimal amount, string currency);
    Task<Result> ExecuteWithdrawalAsync(Guid accountId, decimal amount, string currency);
    Task<Result> ExecuteInternalTransferAsync(Guid fromAccountId, Guid toAccountId, decimal amount, string currency);
    Task<Result> ExecuteBuyAsync(Guid accountId, Guid instrumentId, int quantity, decimal price);
    Task<Result> ExecuteSellAsync(Guid accountId, Guid instrumentId, int quantity, decimal price);
    Task<Result> ReserveFundsAsync(Guid accountId, decimal amount);
    Task<Result> ReleaseFundsAsync(Guid accountId, decimal amount);
    Task<Result> LockAccountFundsAsync(Guid accountId, decimal amount);
    Task<List<TransactionDto>> GetTransactionHistoryAsync(Guid accountId, int pageSize = 50);
}
```

### 2. Implement Trading Service
**Plik:** `backend/TradingPlatform.Core/Services/TradingService.cs`

```csharp
public class TradingService : ITradingService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAccountRepository _accountRepository;
    private readonly IAccountTransferRepository _transferRepository;
    private readonly IPositionRepository _positionRepository;
    private readonly IAuditLogRepository _auditRepository;
    private readonly ILogger<TradingService> _logger;

    // Implement all interface methods:
    // - Validate account, instrument, funds
    // - Use BeginTransactionAsync() for atomic operations
    // - Update AvailableBalance, ReservedBalance
    // - Create AccountTransferEntity records
    // - Update PositionEntity (Quantity, ReservedQuantity, AverageOpenPrice)
    // - Log audit events
}
```

---

## FAZA 2: Repository Layer

### 3. Create Account Transfer Repository
**Plik:** `backend/TradingPlatform.Data/Repositories/IAccountTransferRepository.cs` (Interface)
**Plik:** `backend/TradingPlatform.Data/Repositories/SqlAccountTransferRepository.cs` (Implementation)

```csharp
public interface IAccountTransferRepository
{
    Task<IEnumerable<AccountTransferEntity>> GetByAccountIdAsync(Guid accountId);
    Task<IEnumerable<AccountTransferEntity>> GetByAccountIdPaginatedAsync(Guid accountId, int pageNumber, int pageSize);
    Task AddAsync(AccountTransferEntity transfer);
    Task UpdateAsync(AccountTransferEntity transfer);
}
```

### 4. Create Position Repository
**Plik:** `backend/TradingPlatform.Data/Repositories/IPositionRepository.cs` (Interface)
**Plik:** `backend/TradingPlatform.Data/Repositories/SqlPositionRepository.cs` (Implementation)

```csharp
public interface IPositionRepository
{
    Task<PositionEntity> GetByAccountAndInstrumentAsync(Guid accountId, Guid instrumentId);
    Task<IEnumerable<PositionEntity>> GetByAccountIdAsync(Guid accountId);
    Task AddAsync(PositionEntity position);
    Task UpdateAsync(PositionEntity position);
}
```

### 5. Extend Account Repository
**Plik:** `backend/TradingPlatform.Data/Repositories/SqlAccountRepository.cs`

- Add method: `UpdateBalanceAsync(Guid accountId, decimal availableBalance, decimal reservedBalance)`
- Add transaction support methods

---

## FAZA 3: API Controllers

### 6. Create Trading Controller
**Plik:** `backend/TradingPlatform.Api/Controllers/TradingController.cs`

```csharp
[ApiController]
[Route("api/trade")]
[Authorize]
public class TradingController : ControllerBase
{
    private readonly ITradingService _tradingService;

    [HttpPost("buy")]
    public async Task<IActionResult> Buy([FromBody] BuyOrderRequest request)
    {
        // Extract user ID from claims
        // Call _tradingService.ExecuteBuyAsync()
        // Return 200 or error
    }

    [HttpPost("sell")]
    public async Task<IActionResult> Sell([FromBody] SellOrderRequest request)
    {
        // Extract user ID from claims
        // Call _tradingService.ExecuteSellAsync()
        // Return 200 or error
    }
}
```

### 7. Extend Account Controller
**Plik:** `backend/TradingPlatform.Api/Controllers/AccountController.cs`

```csharp
[HttpPost("transfer")]
public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
{
    // Call _tradingService.ExecuteInternalTransferAsync()
}

[HttpPost("lock")]
public async Task<IActionResult> Lock([FromBody] LockRequest request)
{
    // Call _tradingService.LockAccountFundsAsync()
}

[HttpGet("transactions")]
public async Task<IActionResult> GetTransactions([FromQuery] int pageSize = 50)
{
    // Call _tradingService.GetTransactionHistoryAsync()
}
```

---

## FAZA 4: DTOs i Models

### 8. Create Request/Response DTOs
**Plik:** `backend/TradingPlatform.Api/DTOs/BuyOrderRequest.cs`

```csharp
public class BuyOrderRequest
{
    public Guid InstrumentId { get; set; }
    public int Quantity { get; set; }
    public decimal Price { get; set; }
}
```

**Plik:** `backend/TradingPlatform.Api/DTOs/SellOrderRequest.cs`
**Plik:** `backend/TradingPlatform.Api/DTOs/TransferRequest.cs`
**Plik:** `backend/TradingPlatform.Api/DTOs/LockRequest.cs`
**Plik:** `backend/TradingPlatform.Api/DTOs/TransactionDto.cs`

```csharp
public class TransactionDto
{
    public Guid Id { get; set; }
    public Guid AccountId { get; set; }
    public string TransactionType { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

---

## FAZA 5: Extend Entity Model

### 9. Update AccountTransferEntity
**Plik:** `backend/TradingPlatform.Data/Entities/AccountTransferEntity.cs`

- Add new TransferType values: `BUY`, `SELL`, `LOCK`, `UNLOCK`
- Add fields: `InstrumentId` (nullable), `OrderDetails` (JSON)

### 10. Update PositionEntity
**Plik:** `backend/TradingPlatform.Data/Entities/PositionEntity.cs`

- Verify fields: `Quantity`, `ReservedQuantity`, `AverageOpenPrice`
- Add: `LastUpdateTime`

---

## FAZA 6: Unit of Work Pattern

### 11. Create Unit of Work
**Plik:** `backend/TradingPlatform.Data/UnitOfWork/IUnitOfWork.cs` (Interface)
**Plik:** `backend/TradingPlatform.Data/UnitOfWork/UnitOfWork.cs` (Implementation)

```csharp
public interface IUnitOfWork
{
    IAccountRepository Accounts { get; }
    IAccountTransferRepository Transfers { get; }
    IPositionRepository Positions { get; }
    Task BeginTransactionAsync();
    Task CommitAsync();
    Task RollbackAsync();
    Task SaveChangesAsync();
}
```

---

## FAZA 7: Dependency Injection

### 12. Register Services in DI Container
**Plik:** `backend/TradingPlatform.Api/Program.cs` (w `ConfigureServices`)

```csharp
services.AddScoped<ITradingService, TradingService>();
services.AddScoped<IUnitOfWork, UnitOfWork>();
services.AddScoped<IAccountTransferRepository, SqlAccountTransferRepository>();
services.AddScoped<IPositionRepository, SqlPositionRepository>();
```

---

## FAZA 8: Database Migrations

### 13. Create EF Core Migration
**Terminal:**
```bash
cd backend/TradingPlatform.Data
dotnet ef migrations add AddTradingServiceSupport --project ../TradingPlatform.Data --startup-project ../TradingPlatform.Api
```

**Automatycznie tworzy:** `backend/TradingPlatform.Data/Migrations/[TIMESTAMP]_AddTradingServiceSupport.cs`

---

## FAZA 9: Audit Logging Enhancement

### 14. Extend Audit Logging
**Plik:** `backend/TradingPlatform.Core/Models/AuditLog.cs`

- Add Action values: `BUY_EXECUTED`, `SELL_EXECUTED`, `TRANSFER_COMPLETED`, `FUNDS_RESERVED`, `FUNDS_RELEASED`
- Add field: `TransactionId`

### 15. Update Audit Logger
**Plik:** `backend/TradingPlatform.Data/Repositories/AuditLogRepository.cs`

```csharp
public async Task LogTransactionAsync(
    Guid userId, 
    string action, 
    Guid entityId, 
    object details)
{
    // Create AuditLogEntity
    // Persist to database
}
```

---

## FAZA 10: Frontend Integration

### 16. Update useTransactions Hook
**Plik:** `frontend/src/hooks/useTransactions.ts`

```typescript
import { useQuery } from '@tanstack/react-query';

export function useTransactions(accountId: string) {
  return useQuery({
    queryKey: ['transactions', accountId],
    queryFn: async () => {
      const response = await fetch(`/api/account/transactions?pageSize=50`, {
        headers: { 'Authorization': `Bearer ${token}` }
      });
      return response.json();
    }
  });
}
```

### 17. Update TransactionHistory Component
**Plik:** `frontend/src/components/user/TransactionHistory.tsx`

- Replace mock data with real hook
- Remove TODO comment
- Add loading/error states

---

## FAZA 11: Integration Tests

### 18. Create Integration Tests
**Plik:** `backend/TradingPlatform.Tests/Integration/TradingServiceTests.cs`

```csharp
[TestClass]
public class TradingServiceTests
{
    [TestMethod]
    public async Task ExecuteBuyAsync_ShouldUpdateBalance_AndCreateTransferRecord()
    {
        // Arrange
        // Act
        // Assert
    }

    [TestMethod]
    public async Task ExecuteSellAsync_ShouldUpdatePosition_AndLogAudit()
    {
        // ...
    }

    [TestMethod]
    public async Task ReserveFundsAsync_ShouldPreventOverdrawn()
    {
        // ...
    }

    [TestMethod]
    public async Task GetTransactionHistoryAsync_ShouldReturnPaginated()
    {
        // ...
    }
}
```

### 19. Create API Tests
**Plik:** `backend/TradingPlatform.Tests/Integration/TradingControllerTests.cs`

```csharp
[TestClass]
public class TradingControllerTests
{
    [TestMethod]
    public async Task PostBuy_WithValidRequest_ShouldReturn200()
    {
        // ...
    }

    [TestMethod]
    public async Task PostTransfer_WithInsufficientFunds_ShouldReturn400()
    {
        // ...
    }
}
```

---

## FAZA 12: Docker & Deployment

### 20. Update Docker Compose
**Plik:** `docker/docker-compose.yaml`

- Ensure database migrations run on startup
- Verify backend service rebuilds with new code

**Terminal:**
```bash
cd docker
docker compose down
docker compose up --build
```

---

## FAZA 13: Documentation & Validation

### 21. Update API Documentation
**Plik:** `backend/API_DOCUMENTATION.md` (create if not exists)

- Document all new endpoints
- Include request/response examples
- List error codes

### 22. Validate Against Audit Checklist
**Plik:** `audits/buyingservice/IMPLEMENTATION_VALIDATION.md` (create new)

- [x] ITradingService interface created
- [x] TradingService implemented
- [x] Repositories created
- [x] API endpoints exposed
- [x] DTOs defined
- [x] Entity model updated
- [x] Unit of Work pattern implemented
- [x] DI configured
- [x] Migrations applied
- [x] Audit logging extended
- [x] Frontend updated
- [x] Integration tests pass
- [x] E2E tests pass
- [x] Docker builds successfully

---

## 📊 Implementation Timeline

| Faza | Zadanie | Szacowana Czas | Status |
|------|---------|----------------|--------|
| 1 | ITradingService + Implementation | 2-3h | ⏳ |
| 2 | Repositories | 1-2h | ⏳ |
| 3 | API Controllers | 1-2h | ⏳ |
| 4 | DTOs | 30min | ⏳ |
| 5 | Entity Extensions | 30min | ⏳ |
| 6 | Unit of Work | 1h | ⏳ |
| 7 | DI Configuration | 15min | ⏳ |
| 8 | Database Migration | 15min | ⏳ |
| 9 | Audit Logging | 30min | ⏳ |
| 10 | Frontend Integration | 1h | ⏳ |
| 11 | Integration Tests | 2-3h | ⏳ |
| 12 | Docker Validation | 30min | ⏳ |
| 13 | Documentation | 1h | ⏳ |
| **RAZEM** | | **~15-17h** | |

---

## ⚠️ Krytyczne Decyzje

1. **Atomic Transactions**: Wszystkie operacje multi-entity używają `BeginTransactionAsync()` + rollback
2. **Balance Validation**: Zawsze sprawdzaj `AvailableBalance >= Amount` przed operacją
3. **Audit Trail**: Każda transakcja musi być zloggowana z `Action`, `EntityId`, `Details`
4. **Error Handling**: Zwracaj `Result<T>` z szczegółowymi error messages
5. **Authorization**: Wszystkie endpointy wymagają `[Authorize]` i user ID z JWT

---

## ✅ Definition of Done

- [ ] Wszystkie pliki stworzone i edytowane
- [ ] Kod kompiluje się bez błędów
- [ ] Migracje bazy danych zastosowane
- [ ] Unit testy: 100% pass
- [ ] Integration testy: 100% pass
- [ ] E2E testy na frontendie: pass
- [ ] Docker build successful
- [ ] Code review approved
- [ ] Deployed to staging
- [ ] Manual testing on staging completed

---

**Wersja:** 1.0  
**Data:** 2026-06-12  
**Status:** ⏳ Ready for Implementation
