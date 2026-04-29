# 🧭 FAZA 3 DECISION ANALYSIS — Instrument Lifecycle Engine

**Data:** 22 kwietnia 2026  
**Status:** STRATEGIC CHECKPOINT  
**Decision Point:** GO/NO-GO na Instrument Workflow Engine

---

## 📊 OBECNY STAN (FAZA 2 COMPLETE)

### ✅ Co mamy
- **Backend CRUD:** 100% implementowałem (Create, Read, Update, Delete, Block, Unblock)
- **Audit Trail:** CreatedBy, ModifiedBy, CreatedAtUtc, ModifiedAtUtc, RowVersion
- **Status Enum:** InstrumentStatus (Draft, PendingApproval, Approved, Rejected, Blocked, Archived)
- **Admin Model:** AdminRequest już istnieje (do two-step approval)
- **Authorization:** JWT + AdminId tracking konsekwentnie
- **Docker:** Backend running i stabilny

### ⚠️ Co BRAKUJE w FAZIE 2 (ale jest już infrastruktura)
- **Workflow Rules:** Jakie przejścia statusów są dozwolone
- **Enforcement:** Która logika waliduje przejścia (centralnie w Service)
- **Domain Events:** Czy emitujemy zdarzenia przy zmianach statusu
- **Frontend:** Instrument CRUD UI (TypeScript types, hooks, services — wszystko to stub)

---

## 🔍 ANALIZA AUDYTÓW

### Audit 1: InstrumentsAUDIT.md
**Konkluzja:** Backend jest spójny, frontend pusty, ale infrastruktura AdminRequest gotowa

**Wniosek:**
```
Backend jest gotowy na workflow engine.
Frontend może czekać na workflow logiku w backend.
```

### Audit 2: IMPLEMENTATION_TODO.md
**Status:** Głównie auth/2FA checklist, ale nie ma nic o workflow

**Wniosek:**
```
Workflow engine nie jest zaplanowany jako priority.
To jest NOWY feature, który ja proponuję.
```

### Audit 3: ENTERPRISE_IMPLEMENTATION_COMPLETE.md
**Najważniejsze:** HTTP Client architecture już gotowa

**Wniosek:**
```
Frontend infrastruktura jest solidna.
Czeka na domeny biznesowej (workflow rules).
```

---

## 🎯 BIZNESOWE WYMAGANIA (z audytów)

### Z InstrumentsAUDIT:
```
Problem: "kto może zmienić status i w jakich warunkach?"
Odpowiedź: BRAKUJE!

Czyli muszę zapytać: co to są REGUŁY dla Instruments?
```

### Z AdminRequest Model:
```csharp
public sealed record AdminRequest(
    Guid Id,
    Guid InstrumentId,
    Guid RequestedByAdminId,
    Guid? ApprovedByAdminId,
    string Action,           // ← "block", "unblock", "approve", "reject"?
    string Reason,
    string Status,           // ← "pending", "approved", "rejected"?
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? ApprovedAtUtc);
```

**Wniosek:**
```
AdminRequest istnieje → dwa-etapowy approval już jest zaplanowany.
Muszę go aktywować i powiązać ze statusami Instrument.
```

---

## 🏛️ ARCHITEKTURALNE IMPLIKACJE

### Opcja A: WORKFLOW ENGINE (moja rekomendacja)
```
┌─────────────────────────────────────────┐
│   Instrument State Machine              │
├─────────────────────────────────────────┤
│                                         │
│  Draft  ─→  PendingApproval  ─→ Approved
│    ↓           ↓                 ↓
│    └─→ Rejected (can retry)    Blocked
│                                 ↓
│                            Archived
│                                         │
│  Rules:                                 │
│  • Draft → PendingApproval: by creator │
│  • PendingApproval → Approved: by other│
│  • PendingApproval → Rejected: by other│
│  • * → Blocked: by ANY admin           │
│  • Approved → Archived: by creator     │
│                                         │
│  Enforcement: IInstrumentService layer │
│  Audit: AdminRequest per transition    │
│  Events: INotification/EventBus        │
│                                         │
└─────────────────────────────────────────┘
```

**Korzyści:**
- ✅ Centralna reguła w Service
- ✅ Audit trail via AdminRequest
- ✅ Skalowalne (łatwo dodać nowe statusy)
- ✅ DDD-compliant (domain logic in Service, not Controller)
- ✅ Backend-driven (frontend nie musi znać reguł)

**Koszt:**
- ~200-300 linii kodu w IInstrumentService
- 2-3 nowe methods: ApproveAsync, RejectAsync, ArchiveAsync
- 1 nueva AdminRequest integration w service

### Opcja B: CRUD + FRONTEND (NO WORKFLOW)
```
Post.../api/admin/instruments → Status = Draft (always)
PUT.../api/admin/instruments/{id} → Update any field
PATCH.../api/admin/instruments/{id}/block → IsBlocked = true
```

**Korzyści:**
- ✅ Szybko (bez logiki)
- ✅ Prosty frontend

**Wady:**
- ❌ Każdy admin może zmienić każdy instrument
- ❌ Brak approval flow
- ❌ Status enum nie ma celu
- ❌ AdminRequest nigdy się nie aktywuje
- ❌ HACK: status nie ma reguł (to jest właśnie ten "debt")

---

## 📈 WIZJA VS RZECZYWISTOŚĆ

### Wizja (z enum):
```csharp
public enum InstrumentStatus
{
    Draft = 1,              // Admin edytuje, nie publikowany
    PendingApproval = 2,    // ← NIGDY SIĘ NIE UŻYWA
    Approved = 3,           // ← ZAWSZE SIĘ TWORZY (seed data)
    Rejected = 4,           // ← NIGDY SIĘ NIE UŻYWA
    Blocked = 5,            // ← OK (via PATCH /block)
    Archived = 6            // ← NIGDY SIĘ NIE UŻYWA
}
```

**Problem:**
```
4 ze 6 statusów to MARTWY KOD.
To jest architektoniczny DEBT.
```

### Rzeczywistość (bez workflow):
```
Każdy instrument tworzy się w Draft
Admin może go zmienić bezpośrednio
Nikt nie zatwierdza (workflow=brak)
Seed data ma Approved (hardcoded)
```

**Problem:**
```
Enum jest "futureproof" (gotowy na workflow),
ale niemożliwe jest go osiągnąć bez FAZY 3.
```

---

## 🎲 RYZYKO ZDECYZJI

### Jeśli idę w WORKFLOW (FAZA 3) TERAZ:
✅ **Korzyści:**
- System staje się procesowy (nie tylko CRUD)
- Status enum ma sens
- AdminRequest aktywuje się
- Audit trail kompletny
- Skalowalne na inne entity (accounts, users, etc)
- Enterprise-grade flow

❌ **Ryzyka:**
- +300 linii kodu w service layer
- +2-3 API endpoints (POST /approve, /reject)
- Frontend musi wiedzieć o workflow
- Baza danych: RowVersion musi być tracked (jest!)

### Jeśli POMIŃ WORKFLOW (czysty CRUD):
✅ **Korzyści:**
- Szybko do frontendu
- Prostsze testy

❌ **Ryzyka:**
- Status enum = martwy kod
- Brak approval flow
- Każdy admin może wszystko
- Trudne do rozszerzenia później
- DEBT: ktoś będzie musiał refactor

---

## 💡 MOJA REKOMENDACJA

### 🎯 IDŹCIE W WORKFLOW (FAZA 3)

**Dlaczego:**

1. **Status Enum istnieje — nie jest przypadkowy**
   - Ktoś zaplanował PendingApproval, Rejected, Archived
   - To nie jest "just in case" — to jest **architektura przyszłości**

2. **AdminRequest Model istnieje — czeka na aktywację**
   - Nie musisz tworzyć nic nowego
   - Tylko włączyć istniejącą infrastrukturę

3. **Audit Trail już jest (CreatedBy, ModifiedBy)**
   - Concurrency control (RowVersion) jest
   - To **naturalny fundament** dla workflow

4. **Backend jest stabilny**
   - FAZA 2 = 100% complete
   - Możesz spokojnie dodać workflow bez refactor

5. **Frontend czeka**
   - Hooks są puste (stub)
   - Lepiej czekają na domeny biznesowej niż robić hacks

---

## 🗺️ FAZA 3 PLAN (rough outline)

### CZĘŚĆ 1: Service Layer Rules (Day 1)
```csharp
// IInstrumentService nowe methods:
Task<InstrumentDto> ApproveAsync(Guid id, Guid approvedBy, ct);
Task<InstrumentDto> RejectAsync(Guid id, string reason, Guid approvedBy, ct);
Task<InstrumentDto> ArchiveAsync(Guid id, Guid archivedBy, ct);

// Walidacja reguł:
- Draft → PendingApproval: requester = creator
- PendingApproval → Approved: requester ≠ creator  
- PendingApproval → Rejected: requester ≠ creator
- Approved → Blocked: ANY admin
- Approved → Archived: ANY admin
- Rejected → Draft: creator only (retry)
```

### CZĘŚĆ 2: Admin Controller Integration (Day 1-2)
```csharp
[HttpPost("instruments/{id}/approve")]
[HttpPost("instruments/{id}/reject")]
[HttpPost("instruments/{id}/archive")]
// + AdminRequest logging per transition
```

### CZĘŚĆ 3: AdminService Update (Day 2)
```csharp
// Link AdminRequest ↔ Instrument transitions
// Logging + event hooks
```

### CZĘŚĆ 4: Integration Tests (Day 2-3)
```csharp
[Test] ApproveFlow_ValidTransition_Success
[Test] RejectFlow_InvalidTransition_Throws
[Test] AuditTrail_CreatedAfterApprove
```

---

## ✅ FINAL DECISION

### GO: FAZA 3 — Instrument Lifecycle Engine

**Timeline:** 3 days (Days 3-5)
1. State Machine Rules + Service Methods
2. Controller Integration + AdminRequest Logging
3. Tests + Docker Validation

**Rationale:**
- Infrastructure już istnieje (enum, model, audit fields)
- Backend jest stabilny (zero debt)
- Status enum ma sens (nie martwy kod)
- Skalowalne na cały system
- Enterprise-grade архитектура

**After FAZA 3:**
- System jest **procesowy** (nie tylko CRUD)
- Frontend może zacząć z pełnym kontekstem biznesowym
- Workflow template dla przyszłych entity

---

## 📌 NEXT IMMEDIATE ACTION

Twoja odpowiedź na pytania:

1. **Czy zgadzasz się z WORKFLOW ENGINE?** (Y/N)
2. **Czy Status enum odzwierciedla RZECZYWISTE** reguły biznesowe? (czy mam coś zmodyfikować?)
3. **Czy AdminRequest "Action" field powinien mieć enum?** (co są możliwe actions?)
4. **Czy chcesz Domain Events** (event bus pattern)? (czy direct logging wystarczy?)

Czekam na Twoją decyzję → idziemy w FAZĘ 3 🚀
