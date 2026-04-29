# 🛠️ INSTRUMENTS ENDPOINT - KOMPLETNY AUDIT
**Data auditu:** 22 kwietnia 2026  
**Status:** WYMAGA DZIAŁAŃ - Niekompletna implementacja frontendu

---

## 📊 PODSUMOWANIE WYKONAWCZE

| Komponent | Status | Ocena |
|-----------|--------|-------|
| **Backend - Model** | ✅ GOTOWY | Model Instrument z polem Type ✓ |
| **Backend - Enumeracja** | ✅ GOTOWY | InstrumentType z Stock, Crypto, Cfd ✓ |
| **Backend - Baza danych** | ✅ GOTOWY | InstrumentEntity ze wszystkimi polami ✓ |
| **Backend - Serwis** | ✅ GOTOWY | InstrumentService z CRUD operacjami ✓ |
| **Backend - Repository** | ✅ GOTOWY | IInstrumentRepository implementacja ✓ |
| **Backend - Admin Controller** | ✅ GOTOWY | /api/admin/instruments endpoint ✓ |
| **Backend - Instruments Controller** | ⚠️ CZĘŚCIOWO | GET endpoints mają [AllowAnonymous] ✗ |
| **Backend - Autoryzacja** | ❌ WYMAGA PRACY | Admin endpoint OK, ale GET bez auth ✗ |
| **Frontend - Hook** | ❌ PUSTY | useInstruments() to stub bez funkcji ✗ |
| **Frontend - Komponenty** | ⚠️ CZĘŚCIOWO | UI gotowy, ale bez logiki ✗ |
| **Frontend - API Service** | ❌ BRAKUJE | Nie ma instrumentsService ✗ |
| **Frontend - Types** | ⚠️ NIEZGODNY | Różne od backendu (description, status, createdBy) ✗ |

---

## 🔍 SZCZEGÓŁOWA ANALIZA

### ✅ BACKEND - CO MAMY

#### 1. **Model & Enumeracja** (`TradingPlatform.Core/Models/Instrument.cs`)
```csharp
public sealed record Instrument(
    Guid Id,
    string Symbol,
    string Name,
    InstrumentType Type,              // ✓ Stock, Crypto, Cfd, Etf, Forex
    AccountPillar Pillar,
    string BaseCurrency,
    string QuoteCurrency,
    bool IsActive,
    bool IsBlocked,
    DateTimeOffset CreatedAtUtc);
```

**Status:** ✅ Model zawiera wszystkie potrzebne pola  
**Uwaga:** Brakuje pola dla opisania instrumentu (description)

#### 2. **InstrumentType Enumeration** (`TradingEnums.cs`)
```csharp
public enum InstrumentType
{
    Stock = 1,      // ✓ Akcje
    Crypto = 2,     // ✓ Kryptowaluty
    Cfd = 3,        // ✓ CFD
    Etf = 4,        // Ma ETF (dodatkowy)
    Forex = 5       // Ma Forex (dodatkowy)
}
```

**Status:** ✅ Zawiera wymagane typy + dodatkowe  
**Opcja:** Można użyć jako jest (bardziej uniwersalne)

#### 3. **Database Entity** (`InstrumentEntity.cs`)
```csharp
public sealed class InstrumentEntity
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public InstrumentType Type { get; set; }
    public AccountPillar Pillar { get; set; }
    public string BaseCurrency { get; set; } = "USD";
    public string QuoteCurrency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; } = false;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public ICollection<PositionEntity> Positions { get; set; } = [];
}
```

**Status:** ✅ Prawidłowa struktura  
**Brakuje:** 
- `Description` - opis instrumentu (dla admina)
- `CreatedBy` (Guid) - kto utworzył
- `ModifiedAtUtc` - kiedy ostatnio zmieniono
- `ModifiedBy` (Guid) - kto zmienił

#### 4. **DTO** (`InstrumentDto.cs`)
```csharp
public sealed record InstrumentDto(
    Guid Id,
    string Symbol,
    string Name,
    string Type,                    // ← Konwertowany do string
    string Pillar,
    string BaseCurrency,
    string QuoteCurrency,
    bool IsActive,
    bool IsBlocked,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateInstrumentRequest(
    string Symbol,
    string Name,
    string Type,
    string Pillar,
    string BaseCurrency,
    string QuoteCurrency);

public sealed record UpdateInstrumentRequest(
    string Name,
    bool IsActive);
```

**Status:** ⚠️ Brakuje pól  
**Problemy:**
- Brak `Description` w request/response
- UpdateInstrumentRequest zbyt ograniczony (tylko Name i IsActive)
- Brak informacji o adminach (CreatedBy, ModifiedBy)

#### 5. **Service** (`InstrumentService.cs`)
```csharp
public sealed class InstrumentService : IInstrumentService
{
    // ✓ GetByIdAsync
    // ✓ GetBySymbolAsync
    // ✓ GetAllAsync
    // ✓ GetAllActiveAsync
    // ✓ CreateAsync
    // ✓ UpdateAsync
    // ✓ BlockAsync
    // ✓ UnblockAsync
    // ✓ DeleteAsync
}
```

**Status:** ✅ Pełna implementacja CRUD + block/unblock

#### 6. **Repository** (`IInstrumentRepository.cs`)
```csharp
public interface IInstrumentRepository
{
    Task<Instrument?> GetByIdAsync(Guid id, ...);
    Task<Instrument?> GetBySymbolAsync(string symbol, ...);
    Task<IEnumerable<Instrument>> GetAllAsync(...);
    Task<IEnumerable<Instrument>> GetAllActiveAsync(...);
    Task AddAsync(Instrument instrument, ...);
    Task UpdateAsync(Instrument instrument, ...);
    Task DeleteAsync(Guid id, ...);
    Task SaveChangesAsync(...);
}
```

**Status:** ✅ Interfejs kompletny

#### 7. **Admin Controller Endpoints** (`AdminController.cs`)
```
✅ GET    /api/admin/instruments          - Lista wszystkich do zarządzania
✅ POST   /api/admin/instruments/{id}/request-block   - Wniosek o blokadę
✅ POST   /api/admin/instruments/{id}/request-unblock - Wniosek o odblokadę
```

**Status:** ✅ Autoryzacja: [Authorize(Roles = "Admin")]  
**Uwaga:** Te endpointy mają prawidłową ochronę!

---

### ⚠️ BACKEND - PROBLEMY

#### ❌ Problem 1: InstrumentsController - [AllowAnonymous] na GET
**Lokalizacja:** `TradingPlatform.Api/Controllers/InstrumentsController.cs`

```csharp
[HttpGet]                           // ← PROBLEM!
[AllowAnonymous]                    // ← PUBLICZNY DOSTĘP!
public async Task<ActionResult<IEnumerable<InstrumentDto>>> GetAll(...)
{
    // Wszyscy mogą pobrać listę wszystkich instrumentów
}

[HttpGet("active")]                 // ← PROBLEM!
[AllowAnonymous]                    // ← PUBLICZNY DOSTĘP!
public async Task<ActionResult<IEnumerable<InstrumentDto>>> GetAllActive(...)
{
    // Wszyscy mogą pobrać listę aktywnych
}
```

**Wpływ:** Zwykli użytkownicy mogą zobaczyć WSZYSTKIE instrumenty (włącznie z zablokowanymi)  
**Rozwiązanie:** Frontend powinien używać `/api/admin/instruments` zamiast `/api/instruments`

#### ❌ Problem 2: Brakujące pola w Entity
**Brakuje w `InstrumentEntity`:**
- `Description` (string?) - opis dla admina
- `CreatedBy` (Guid) - który admin utworzył
- `ModifiedAtUtc` (DateTimeOffset?) - kiedy zmieniono
- `ModifiedBy` (Guid?) - kto zmienił

**Wpływ:** Tracking zmian i audyt jest ograniczony

#### ❌ Problem 3: UpdateInstrumentRequest zbyt prosty
```csharp
public sealed record UpdateInstrumentRequest(
    string Name,
    bool IsActive);
```

**Brakuje:**
- `Description`
- `BaseCurrency`, `QuoteCurrency`
- `Pillar`
- Status do zatwierdzenia (draft, pending, approved, rejected)

#### ❌ Problem 4: Brak Admin Approval Request dla nowych instrumentów
Teraz:
- Admin może utworzyć instrument bezpośrednio (POST)

Powinno być:
- Admin tworzy instrument w statusie `draft`
- Admin wysyła do zatwierdzenia przez innego admina
- System tworzy `AdminRequest` do zatwierdzenia
- Inny admin zatwierdza/odrzuca

---

### ❌ FRONTEND - CO BRAKUJE

#### 1. **Hook useInstruments** - PUSTY
**Plik:** `frontend/src/hooks/admin/useInstruments.ts`

```typescript
export const useInstruments = () => {
  const [instruments, setInstruments] = useState<Instrument[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageSize, setPageSize] = useState(10);

  // ← TUTAJ НИЧЕГО NIE MA!
  // Brakuje:
  // - fetchInstruments()
  // - addInstrument()
  // - updateInstrument()
  // - deleteInstrument()
  // - submitForApproval()
  
  return { instruments, totalCount, ... };
};
```

**Status:** ❌ CAŁKOWICIE PUSTY  
**Wymagane funkcje:**
```typescript
const addInstrument = async (data: Partial<Instrument>) => { };
const updateInstrument = async (id: string, data: Partial<Instrument>) => { };
const deleteInstrument = async (id: string) => { };
const submitForApproval = async (id: string, reason?: string) => { };
const fetchInstruments = async (page: number, pageSize: number) => { };
```

#### 2. **API Service** - BRAKUJE
**Brakuje pliku:** `frontend/src/services/admin/instrumentsService.ts`

Powinien zawierać:
```typescript
export const instrumentsService = {
  getAll: async (page?: number, pageSize?: number) => { },
  getById: async (id: string) => { },
  create: async (data: CreateInstrumentRequest) => { },
  update: async (id: string, data: UpdateInstrumentRequest) => { },
  delete: async (id: string) => { },
  submitForApproval: async (id: string, reason?: string) => { },
  block: async (id: string) => { },
  unblock: async (id: string) => { },
};
```

#### 3. **Types** - NIEZGODNE
**Plik:** `frontend/src/types/admin.ts`

```typescript
export interface Instrument {
  id: string;
  name: string;
  symbol: string;
  type: InstrumentType;           // ← OK
  description?: string;            // ← BACKEND BRAK!
  status: InstrumentStatus;         // ← BACKEND BRAK!
  createdBy: string;                // ← BACKEND BRAK!
  createdAt: string;                // ← BACKEND: CreatedAtUtc
  submittedAt?: string;             // ← BACKEND BRAK!
  rejectionReason?: string;         // ← BACKEND BRAK!
  isActive: boolean;                // ← OK
}

export type InstrumentType = 'Forex' | 'Commodity' | 'Crypto' | 'Stock' | 'CFD';
// ← Niezgodne z backendem! Backend ma: Stock, Crypto, Cfd, Etf, Forex
```

**Status:** ⚠️ Prawie wszystkie pola się nie zgadzają

#### 4. **Component InstrumentsContent** - BEZ LOGIKI
**Plik:** `InstrumentsContent.tsx`

```typescript
export const InstrumentsContent: React.FC = () => {
  const {
    instruments,
    totalCount,
    currentPage,
    totalPages,
    loading,
    error,
    page,
    pageSize,
    setPage,
    setPageSize,
    addInstrument,              // ← undefined!
    updateInstrument,           // ← undefined!
    deleteInstrument,           // ← undefined!
    submitForApproval           // ← undefined!
  } = useInstruments();
  
  // Komponenta oczekuje tych funkcji, ale hook ich nie zwraca!
};
```

**Status:** ⚠️ UI jest gotowy, ale backend logiki brakuje

#### 5. **Modal InstrumentModal** - INCOMPLETED
**Plik:** `InstrumentModal.tsx`

```typescript
export const InstrumentModal: React.FC<InstrumentModalProps> = ({ 
  isOpen, 
  onClose, 
  onSave 
}) => {
  if (!isOpen) return null;

  return (
    <div className="modal">
      <div className="modal-content">
        <h2>Instrument</h2>
        <button onClick={onClose}>Close</button>
        {/* ← BRAK FORMULARZA! */}
      </div>
    </div>
  );
};
```

**Status:** ❌ Struktura HTML OK, ale brakuje formularza

---

## 🗂️ STRUKTURA BAZY DANYCH

### Obecna struktura InstrumentEntity:
```
✅ Id (Guid, PK)
✅ Symbol (string) - np. "AAPL"
✅ Name (string) - np. "Apple Inc."
✅ Type (InstrumentType) - Stock, Crypto, Cfd, etc.
✅ Pillar (AccountPillar) - kategoryzacja
✅ BaseCurrency (string) - np. "USD"
✅ QuoteCurrency (string) - np. "USD"
✅ IsActive (bool)
✅ IsBlocked (bool)
✅ CreatedAtUtc (DateTimeOffset)
⚠️ Relationships: ICollection<PositionEntity> (związek z pozycjami)
```

### Brakujące pola (rekomendowane):
```
❌ Description (string?) - opis dla admina
❌ CreatedBy (Guid) - admin który utworzył
❌ ModifiedAtUtc (DateTimeOffset?) - ostatnia zmiana
❌ ModifiedBy (Guid?) - admin który zmienił
❌ Status (ApprovalStatus) - draft/pending/approved
❌ RejectionReason (string?) - jeśli odrzucono
```

---

## 📋 FLOW - CO POWINNO BYĆ

### Workflow tłu instrumentów dla ADMINA:

1. **Admin wyświetla listę**
   ```
   Frontend GET /api/admin/instruments
   ↓
   Backend: Zwraca wszystkie (draft, pending, approved, rejected, blocked)
   ↓
   Frontend: Wyświetla w tabeli z filtrami
   ```

2. **Admin tworzy nowy instrument**
   ```
   Frontend: Modal z formularzem
   User: Wypełnia: Nazwa, Symbol, Typ (Stock/Crypto/CFD), Waluty, Opis
   Click: "Zapisz"
   ↓
   Frontend POST /api/admin/instruments (body: CreateInstrumentRequest)
   ↓
   Backend: Tworzy instrument w statusie DRAFT
   ↓
   Frontend: Wyświetla komunikat sukcesu
   ```

3. **Admin wysyła do zatwierdzenia**
   ```
   Frontend: Klik przycisk "Wyślij" na instrumencie (status=draft/rejected)
   ↓
   Modal: Prosi o opcjonalny powód
   Click: "Wyślij"
   ↓
   Frontend POST /api/admin/instruments/{id}/submit-approval
   ↓
   Backend: 
   - Zmienia status na PENDING
   - Tworzy AdminRequest dla innego admina
   ↓
   Frontend: Komunikat "Wysłano do zatwierdzenia"
   ```

4. **Admin edytuje instrument (jeśli draft)**
   ```
   Frontend: Klik "Edytuj" 
   ↓
   Modal: Przedpełniony formularz
   User: Edytuje pola
   Click: "Zapisz"
   ↓
   Frontend PUT /api/admin/instruments/{id} (body: UpdateInstrumentRequest)
   ↓
   Backend: Aktualizuje instrument
   ↓
   Frontend: Komunikat sukcesu
   ```

5. **Admin usuwa instrument (jeśli draft)**
   ```
   Frontend: Klik "Usuń"
   ↓
   Confirm: "Napewno chcesz usunąć?"
   Click: "Tak"
   ↓
   Frontend DELETE /api/admin/instruments/{id}
   ↓
   Backend: Usuwa instrument
   ↓
   Frontend: Komunikat sukcesu, odśwież listę
   ```

6. **Admin blokuje instrument**
   ```
   Frontend: Klik "Zablokuj" (jeśli approved)
   ↓
   Modal: Prosi o powód
   Click: "Zablokuj"
   ↓
   Frontend PATCH /api/admin/instruments/{id}/block
   ↓
   Backend: 
   - Zmienia IsBlocked = true
   - Tworzy AdminRequest dla audytu
   ↓
   Frontend: Komunikat sukcesu
   ```

---

## 🚨 KLUCZOWE WYMAGANIA DO IMPLEMENTACJI

### 1. BACKEND - Wymagane działania:
- [ ] Dodać `Description` do `InstrumentEntity`
- [ ] Dodać `CreatedBy`, `ModifiedAtUtc`, `ModifiedBy` do entity
- [ ] Zaktualizować DTO, aby zawierały nowe pola
- [ ] Rozszerzyć `UpdateInstrumentRequest` (dodać Description, waluty)
- [ ] **WAŻNE:** Zmienić endpoint `/api/instruments` - usunąć [AllowAnonymous] z GET
  - Albo: Utrzymać [AllowAnonymous] dla regularnych userów, ale oddzielny admin endpoint
  - Rekomendacja: `/api/admin/instruments` dla admina (już istnieje!), `/api/instruments/active` dla userów
- [ ] Dodać status approval flow (draft→pending→approved)
- [ ] Zintegrować z AdminRequest dla zatwierdzania nowych/zmiennych instrumentów

### 2. FRONTEND - Wymagane działania:
- [ ] Stworzyć `instrumentsService.ts` z wszystkimi metodami API
- [ ] Implementować hook `useInstruments()` z pełną logiką
- [ ] Uzupełnić `InstrumentModal.tsx` formularzem
- [ ] Zaktualizować types (`admin.ts`) aby zgadzały się z backendem
- [ ] Dodać paginację do listy instrumentów
- [ ] Dodać filtry: typ, status, aktywny/zablokowany
- [ ] Zintegrować z tokenem autoryzacji (JWT z super_admin/is_admin claim)

### 3. BEZPIECZEŃSTWO:
- [ ] Weryfikacja tokena: czy user ma `is_super_admin` lub `role=Admin`
- [ ] Backend: [Authorize(Roles = "Admin")] na wszystkich admin endpointach ✅ (już mamy)
- [ ] Frontend: Sprawdzenie roli przed wyświetleniem sekcji
- [ ] Logging wszystkich operacji admina (już jest AuditLog system)

### 4. DANE:
- [ ] 3 typy: Stock, Crypto, CFD - ✅ (już mamy w enum)
- [ ] Pododdziały (Pillar) - ✅ (już mamy)
- [ ] Waluty - ✅ (BaseCurrency, QuoteCurrency)

---

## 📝 ENDPOINT MAPA

### Admin Instruments Endpoints:
```
✅ GET    /api/admin/instruments
   Zwraca: IEnumerable<InstrumentDto> (wszystkie, dla zarządzania)
   Auth: [Authorize(Roles = "Admin")]
   Implementacja: AdminController.GetAllInstruments()

✅ POST   /api/admin/instruments
   Body: CreateInstrumentRequest (bez id, automatycznie DRAFT)
   Zwraca: InstrumentDto
   Auth: [Authorize(Roles = "Admin")]
   Implementacja: Trzeba dodać do AdminController

? PUT    /api/admin/instruments/{id}
   Body: UpdateInstrumentRequest (bez statusu!)
   Zwraca: InstrumentDto
   Auth: [Authorize(Roles = "Admin")]
   Implementacja: Trzeba dodać do AdminController

? POST   /api/admin/instruments/{id}/submit-approval
   Body: { reason?: string }
   Zwraca: AdminRequestDto
   Auth: [Authorize(Roles = "Admin")]
   Implementacja: Trzeba dodać do AdminController (lub AdminAuthController)

? DELETE /api/admin/instruments/{id}
   Auth: [Authorize(Roles = "Admin")]
   Implementacja: Trzeba dodać do AdminController

✅ PATCH  /api/admin/instruments/{id}/block
   Body: { reason: string }
   Zwraca: InstrumentDto
   Auth: [Authorize(Roles = "Admin")]
   Implementacja: AdminService.CreateBlockRequestAsync()

✅ PATCH  /api/admin/instruments/{id}/unblock
   Body: { reason: string }
   Zwraca: InstrumentDto
   Auth: [Authorize(Roles = "Admin")]
   Implementacja: AdminService.CreateUnblockRequestAsync()
```

### Regular Instruments Endpoints:
```
✅ GET    /api/instruments/active
   Zwraca: IEnumerable<InstrumentDto> (tylko active & unblocked)
   Auth: [AllowAnonymous] ✓
   Implementacja: InstrumentsController.GetAllActive()

✅ GET    /api/instruments/{id}
   Zwraca: InstrumentDto
   Auth: [AllowAnonymous] ✓
   Implementacja: InstrumentsController.GetById()

✅ GET    /api/instruments/symbol/{symbol}
   Zwraca: InstrumentDto
   Auth: [AllowAnonymous] ✓
   Implementacja: InstrumentsController.GetBySymbol()
```

---

## 🎯 PODSUMOWANIE - STAN VS WYMAGANIA

### Backend (70% GOTOWY)
- ✅ Model, enum, entity - KOMPLETNE
- ✅ Service, Repository - KOMPLETNE
- ✅ Admin endpoints (blok/odblok) - KOMPLETNE
- ⚠️ Admin GET endpoint - ISTNIEJE ale umiejscowienie
- ❌ CRUD endpointy dla admina - CZĘŚCIOWO (POST, PUT, DELETE trzeba dodać)
- ❌ Status approval workflow - BRAKUJE
- ❌ Description, audit fields - BRAKUJE w entity

### Frontend (5% GOTOWY)
- ✅ UI komponenty (styling) - KOMPLETNE
- ❌ Hook - PUSTY
- ❌ API Service - BRAKUJE
- ❌ Types - NIEZGODNE
- ❌ Modal form - NIEKOMPLETNE
- ❌ Logika - BRAKUJE

---

## 🔐 BEZPIECZEŃSTWO - CHECKLIST

```
✅ Backend endpoint ma [Authorize(Roles = "Admin")]
✅ Logowanie operacji admina (AuditLog system)
❌ Frontend musi sprawdzać JWT token (claim: is_super_admin / role=Admin)
❌ Frontend musi blokować dostęp do sekcji Admin dla zwykłych userów
❌ CSRF protection - sprawdzić czy jest
```

---

## 📌 NASTĘPNE KROKI (PLAN IMPLEMENTACJI)

### Faza 1: Przygotowanie Backendu
1. Migracja: Dodać Description, CreatedBy, ModifiedAtUtc, ModifiedBy
2. Zaktualizować InstrumentDto
3. Rozszerzyć UpdateInstrumentRequest
4. Dodać endpointy CRUD do AdminController

### Faza 2: Frontend Hook & Service
1. Stworzyć instrumentsService.ts
2. Implementować useInstruments hook
3. Dodać obsługę błędów i loading

### Faza 3: Frontend UI
1. Uzupełnić InstrumentModal formularzem
2. Zaktualizować types
3. Dodać paginację, filtry
4. Zintegrować z API

### Faza 4: Integracja & Testing
1. End-to-end test całego workflow'u
2. Sprawdzenie bezpieczeństwa
3. Testowanie na branchach: Stock, Crypto, CFD

---

## 🏁 WNIOSKI

**GOTOWE DO DRUKU:**
- Backend ma solidne fundamenty (model, serwis, repo)
- Admin controller istnieje i ma ochronę

**WYMAGA NATYCHMIASTOWEGO DZIAŁANIA:**
- Frontend hook i service są niemal od zera
- Types nie zgadzają się z backendem
- Modal nie ma formularza
- Brakuje pól w DB entity (dla audytu i trackingu)

**REKOMENDACJA:**
Priorytet: Frontend service + hook (szybko dołożyć 4-6h pracy)  
Potem: Backend migracja DB + nowe endpointy (2-3h)  
Wreszcie: Integration & Testing (2-3h)
