# 🛠️ PLAN IMPLEMENTACJI ENDPOINTU `/instruments`

**Data:** 22 kwietnia 2026  
**Status:** DO ZAAPROBOWANIA  
**Priorytet:** WYSOKI

---

## 📋 EXECUTIVE SUMMARY

Wdrożenie pełnego systemu zarządzania instrumentami dla adminów (Stock, Crypto, CFD).

| Faza | Komponent | Oczekiwany czas | Trudność |
|------|-----------|-----------------|----------|
| 1 | Backend DB Migration | 45 min | Średnia |
| 2 | Backend API Endpoints | 60 min | Średnia |
| 3 | Frontend Service | 90 min | Średnia |
| 4 | Frontend Hook | 60 min | Średnia |
| 5 | Frontend UI Integration | 120 min | Wysoka |
| 6 | Testing & Debugging | 90 min | Średnia |
| **RAZEM** | | **465 min (7.75 h)** | |

---

## 🎯 CELE

### Główne cele:
1. ✅ Admins mogą zarządzać 3 typami instrumentów: Stock, Crypto, CFD
2. ✅ Pełny CRUD + approval workflow
3. ✅ Bezpieczne (tylko Admin/SuperAdmin)
4. ✅ Audytowalne (logging wszystkich operacji)
5. ✅ User-friendly UI w Admin Dashboard

### Wymogi funkcjonalne:
- [ ] Admin może tworzyć nowy instrument (forma: Nazwa, Symbol, Typ, Waluty, Opis)
- [ ] Admin może edytować instrument (jeśli draft/rejected)
- [ ] Admin może wysłać do zatwierdzenia inny admin (approval flow)
- [ ] Admin może zablokować instrument
- [ ] Admin może odblokować instrument
- [ ] Admin może usunąć instrument (jeśli draft)
- [ ] Admin widzi historię zmian (CreatedBy, ModifiedBy, daty)
- [ ] System klasyfikuje: Stock, Crypto, CFD

### Wymogi niefunkcjonalne:
- [ ] Performance: Lista 1000 instrumentów < 500ms
- [ ] Bezpieczeństwo: Weryfikacja JWT token + role check
- [ ] Audyt: Wszystkie operacje w AuditLog
- [ ] UI: Responsive design (desktop + tablet)

---

## 🔄 ARCHITEKTURA ROZWIĄZANIA

### Przepływ danych:

```
┌─────────────────────────────────────────────────────┐
│                    FRONTEND (React)                  │
├─────────────────────────────────────────────────────┤
│  InstrumentsContent (UI)                            │
│         ↓                                            │
│  useInstruments (Hook)  ← instrumentsService        │
│         ↓                                            │
│  HTTP Requests (axios)                              │
└─────────────────────────────────────────────────────┘
          ↓ JWT Token w Authorization header
┌─────────────────────────────────────────────────────┐
│                   BACKEND (C# .NET)                  │
├─────────────────────────────────────────────────────┤
│  AdminController.cs                                 │
│    GET    /api/admin/instruments                    │
│    POST   /api/admin/instruments                    │
│    PUT    /api/admin/instruments/{id}               │
│    DELETE /api/admin/instruments/{id}               │
│    POST   /api/admin/instruments/{id}/submit        │
│         ↓                                            │
│  AdminService.cs                                    │
│    - GetAllInstrumentsAsync()                       │
│    - CreateInstrumentAsync()                        │
│    - UpdateInstrumentAsync()                        │
│    - DeleteInstrumentAsync()                        │
│    - SubmitForApprovalAsync()                       │
│         ↓                                            │
│  IInstrumentRepository (SqlInstrumentRepository)    │
│         ↓                                            │
└─────────────────────────────────────────────────────┘
          ↓ EF Core ORM
┌─────────────────────────────────────────────────────┐
│           SQL Server Database                        │
├─────────────────────────────────────────────────────┤
│  Instruments Table                                   │
│    - Id (PK)                                         │
│    - Symbol, Name, Type, Pillar                     │
│    - BaseCurrency, QuoteCurrency                    │
│    - Description ← NOWE                             │
│    - CreatedBy ← NOWE                               │
│    - ModifiedAtUtc ← NOWE                           │
│    - ModifiedBy ← NOWE                              │
│    - IsActive, IsBlocked                            │
│    - CreatedAtUtc                                    │
│         ↓ Relacja                                    │
│  AdminRequests Table (dla approval flow)            │
│    - Id, EntityId (InstrumentId)                    │
│    - Status (pending, approved, rejected)           │
│    - CreatedBy, ApprovedBy                          │
└─────────────────────────────────────────────────────┘
```

---

## 📊 SZCZEGÓŁOWY PLAN FAZA PO FAZIE

---

## FAZA 1: BACKEND - DATABASE MIGRATION (45 min)

### Cel:
Dodać brakujące pola do tabeli Instruments dla audytu i trackingu.

### Kroki:

#### 1.1 Zaktualizować `InstrumentEntity`
**Plik:** `backend/TradingPlatform.Data/Entities/InstrumentEntity.cs`

**Obecna wersja:**
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

**Nowa wersja:**
```csharp
public sealed class InstrumentEntity
{
    public Guid Id { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;  // ← NOWE
    public InstrumentType Type { get; set; }
    public AccountPillar Pillar { get; set; }
    public string BaseCurrency { get; set; } = "USD";
    public string QuoteCurrency { get; set; } = "USD";
    public bool IsActive { get; set; } = true;
    public bool IsBlocked { get; set; } = false;
    public Guid CreatedBy { get; set; }                       // ← NOWE
    public DateTimeOffset CreatedAtUtc { get; set; }
    public Guid? ModifiedBy { get; set; }                     // ← NOWE
    public DateTimeOffset? ModifiedAtUtc { get; set; }        // ← NOWE
    
    public ICollection<PositionEntity> Positions { get; set; } = [];
}
```

#### 1.2 Zaktualizować `Instrument` Model
**Plik:** `backend/TradingPlatform.Core/Models/Instrument.cs`

```csharp
public sealed record Instrument(
    Guid Id,
    string Symbol,
    string Name,
    string Description,              // ← NOWE
    InstrumentType Type,
    AccountPillar Pillar,
    string BaseCurrency,
    string QuoteCurrency,
    bool IsActive,
    bool IsBlocked,
    Guid CreatedBy,                  // ← NOWE
    DateTimeOffset CreatedAtUtc,
    Guid? ModifiedBy,                // ← NOWE
    DateTimeOffset? ModifiedAtUtc);  // ← NOWE
```

#### 1.3 Utworzyć migrację EF Core
```bash
cd backend/TradingPlatform.Data
dotnet ef migrations add AddInstrumentAuditFields
dotnet ef database update
```

**Generowana migracja będzie zawierać:**
```sql
ALTER TABLE Instruments 
ADD Description NVARCHAR(MAX) DEFAULT '';

ALTER TABLE Instruments 
ADD CreatedBy UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID();

ALTER TABLE Instruments 
ADD ModifiedBy UNIQUEIDENTIFIER NULL;

ALTER TABLE Instruments 
ADD ModifiedAtUtc DATETIMEOFFSET NULL;

CREATE INDEX IX_Instruments_CreatedBy ON Instruments(CreatedBy);
CREATE INDEX IX_Instruments_Type ON Instruments(Type);
CREATE INDEX IX_Instruments_Symbol ON Instruments(Symbol);
```

#### 1.4 Zaktualizować Mapping
**Plik:** `backend/TradingPlatform.Core/Mapping/MappingProfile.cs` (dodać mapowanie)

```csharp
CreateMap<Instrument, InstrumentDto>()
    .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type.ToString()))
    .ForMember(dest => dest.Pillar, opt => opt.MapFrom(src => src.Pillar.ToString()));

CreateMap<Instrument, InstrumentEntity>();
CreateMap<InstrumentEntity, Instrument>();
```

---

## FAZA 2: BACKEND - API ENDPOINTS (60 min)

### Cel:
Dodać brakujące endpointy CRUD do AdminController.

### Kroki:

#### 2.1 Zaktualizować DTOs
**Plik:** `backend/TradingPlatform.Core/Dtos/InstrumentDto.cs`

```csharp
namespace TradingPlatform.Core.Dtos;

// Response DTO
public sealed record InstrumentDto(
    Guid Id,
    string Symbol,
    string Name,
    string Description,              // ← NOWE
    string Type,
    string Pillar,
    string BaseCurrency,
    string QuoteCurrency,
    bool IsActive,
    bool IsBlocked,
    Guid CreatedBy,                  // ← NOWE
    DateTimeOffset CreatedAtUtc,
    Guid? ModifiedBy = null,         // ← NOWE
    DateTimeOffset? ModifiedAtUtc = null); // ← NOWE

// Create Request
public sealed record CreateInstrumentRequest(
    string Symbol,
    string Name,
    string Description,              // ← NOWE
    string Type,
    string Pillar,
    string BaseCurrency,
    string QuoteCurrency);

// Update Request
public sealed record UpdateInstrumentRequest(
    string Name,
    string Description,              // ← NOWE
    string BaseCurrency,             // ← NOWE
    string QuoteCurrency,            // ← NOWE
    bool IsActive);

// Submit for Approval Request
public sealed record SubmitInstrumentForApprovalRequest(
    string? Reason);
```

#### 2.2 Zaktualizować AdminService
**Plik:** `backend/TradingPlatform.Core/Services/AdminService.cs`

**Dodać metody:**

```csharp
public async Task<InstrumentDto> CreateInstrumentAsync(
    CreateInstrumentRequest request,
    Guid adminId,
    CancellationToken cancellationToken = default)
{
    // Implementacja (wykorzystać InstrumentService)
    // 1. Walidacja
    // 2. Tworzenie
    // 3. Logging (AuditLog)
    // 4. Zwrot DTO
}

public async Task<InstrumentDto> UpdateInstrumentAsync(
    Guid instrumentId,
    UpdateInstrumentRequest request,
    Guid adminId,
    CancellationToken cancellationToken = default)
{
    // Implementacja
}

public async Task<bool> DeleteInstrumentAsync(
    Guid instrumentId,
    Guid adminId,
    CancellationToken cancellationToken = default)
{
    // Implementacja (tylko jeśli draft)
}

public async Task<AdminRequestDto> SubmitInstrumentForApprovalAsync(
    Guid instrumentId,
    string? reason,
    Guid adminId,
    CancellationToken cancellationToken = default)
{
    // Implementacja
    // 1. Sprawdzić czy instrument jest w draft/rejected
    // 2. Zmienić status na pending
    // 3. Utworzyć AdminRequest
    // 4. Logging
}
```

#### 2.3 Dodać endpointy do AdminController
**Plik:** `backend/TradingPlatform.Api/Controllers/AdminController.cs`

```csharp
/// <summary>
/// Create new instrument (ADMIN only)
/// </summary>
[HttpPost("instruments")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<InstrumentDto>> CreateInstrument(
    [FromBody] CreateInstrumentRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        var adminId = GetAdminIdFromToken();
        var instrument = await _adminService.CreateInstrumentAsync(
            request, adminId, cancellationToken);
        return CreatedAtAction(nameof(GetAllInstruments), new { id = instrument.Id }, instrument);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error creating instrument");
        return StatusCode(StatusCodes.Status500InternalServerError, 
            new { error = "Failed to create instrument" });
    }
}

/// <summary>
/// Update instrument (ADMIN only, only if draft)
/// </summary>
[HttpPut("instruments/{id}")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(typeof(InstrumentDto), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<InstrumentDto>> UpdateInstrument(
    Guid id,
    [FromBody] UpdateInstrumentRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        var adminId = GetAdminIdFromToken();
        var instrument = await _adminService.UpdateInstrumentAsync(
            id, request, adminId, cancellationToken);
        return Ok(instrument);
    }
    catch (InvalidOperationException ex)
    {
        return NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating instrument");
        return StatusCode(StatusCodes.Status500InternalServerError, 
            new { error = "Failed to update instrument" });
    }
}

/// <summary>
/// Delete instrument (ADMIN only, only if draft)
/// </summary>
[HttpDelete("instruments/{id}")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<IActionResult> DeleteInstrument(
    Guid id,
    CancellationToken cancellationToken)
{
    try
    {
        var adminId = GetAdminIdFromToken();
        await _adminService.DeleteInstrumentAsync(id, adminId, cancellationToken);
        return NoContent();
    }
    catch (InvalidOperationException ex)
    {
        return NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting instrument");
        return StatusCode(StatusCodes.Status500InternalServerError, 
            new { error = "Failed to delete instrument" });
    }
}

/// <summary>
/// Submit instrument for approval (ADMIN only)
/// </summary>
[HttpPost("instruments/{id}/submit-approval")]
[Authorize(Roles = "Admin")]
[ProducesResponseType(typeof(AdminRequestDto), StatusCodes.Status201Created)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public async Task<ActionResult<AdminRequestDto>> SubmitInstrumentForApproval(
    Guid id,
    [FromBody] SubmitInstrumentForApprovalRequest request,
    CancellationToken cancellationToken)
{
    try
    {
        var adminId = GetAdminIdFromToken();
        var adminRequest = await _adminService.SubmitInstrumentForApprovalAsync(
            id, request.Reason, adminId, cancellationToken);
        return CreatedAtAction(nameof(GetPendingRequests), adminRequest);
    }
    catch (InvalidOperationException ex)
    {
        return NotFound(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error submitting instrument for approval");
        return StatusCode(StatusCodes.Status500InternalServerError, 
            new { error = "Failed to submit instrument for approval" });
    }
}
```

---

## FAZA 3: FRONTEND - API SERVICE (90 min)

### Cel:
Stworzyć usługę API do komunikacji z backendem.

### Kroki:

#### 3.1 Stworzyć `instrumentsService.ts`
**Plik:** `frontend/src/services/admin/instrumentsService.ts`

```typescript
import axiosInstance from '../httpClient';
import { Instrument } from '../../types/admin';
import { ApiResponse } from '../../types/api';

export const instrumentsService = {
  // Fetch all instruments
  getAll: async (page: number = 1, pageSize: number = 10): Promise<Instrument[]> => {
    try {
      const response = await axiosInstance.get<Instrument[]>(
        '/admin/instruments',
        { params: { page, pageSize } }
      );
      return response.data;
    } catch (error) {
      throw new Error(`Failed to fetch instruments: ${error}`);
    }
  },

  // Fetch single instrument
  getById: async (id: string): Promise<Instrument> => {
    try {
      const response = await axiosInstance.get<Instrument>(
        `/admin/instruments/${id}`
      );
      return response.data;
    } catch (error) {
      throw new Error(`Failed to fetch instrument: ${error}`);
    }
  },

  // Create new instrument
  create: async (data: {
    symbol: string;
    name: string;
    description: string;
    type: 'Stock' | 'Crypto' | 'Cfd';
    pillar: string;
    baseCurrency: string;
    quoteCurrency: string;
  }): Promise<Instrument> => {
    try {
      const response = await axiosInstance.post<Instrument>(
        '/admin/instruments',
        data
      );
      return response.data;
    } catch (error) {
      throw new Error(`Failed to create instrument: ${error}`);
    }
  },

  // Update instrument
  update: async (id: string, data: Partial<{
    name: string;
    description: string;
    baseCurrency: string;
    quoteCurrency: string;
    isActive: boolean;
  }>): Promise<Instrument> => {
    try {
      const response = await axiosInstance.put<Instrument>(
        `/admin/instruments/${id}`,
        data
      );
      return response.data;
    } catch (error) {
      throw new Error(`Failed to update instrument: ${error}`);
    }
  },

  // Delete instrument
  delete: async (id: string): Promise<void> => {
    try {
      await axiosInstance.delete(`/admin/instruments/${id}`);
    } catch (error) {
      throw new Error(`Failed to delete instrument: ${error}`);
    }
  },

  // Submit for approval
  submitForApproval: async (id: string, reason?: string): Promise<any> => {
    try {
      const response = await axiosInstance.post(
        `/admin/instruments/${id}/submit-approval`,
        { reason }
      );
      return response.data;
    } catch (error) {
      throw new Error(`Failed to submit instrument for approval: ${error}`);
    }
  },

  // Block instrument
  block: async (id: string, reason?: string): Promise<Instrument> => {
    try {
      const response = await axiosInstance.patch<Instrument>(
        `/admin/instruments/${id}/block`,
        { reason }
      );
      return response.data;
    } catch (error) {
      throw new Error(`Failed to block instrument: ${error}`);
    }
  },

  // Unblock instrument
  unblock: async (id: string, reason?: string): Promise<Instrument> => {
    try {
      const response = await axiosInstance.patch<Instrument>(
        `/admin/instruments/${id}/unblock`,
        { reason }
      );
      return response.data;
    } catch (error) {
      throw new Error(`Failed to unblock instrument: ${error}`);
    }
  },
};
```

#### 3.2 Zaktualizować `admin.ts` types
**Plik:** `frontend/src/types/admin.ts`

```typescript
// Instrument Types
export type InstrumentType = 'Stock' | 'Crypto' | 'Cfd';
export type InstrumentStatus = 'draft' | 'pending' | 'approved' | 'rejected';

export interface Instrument {
  id: string;
  symbol: string;
  name: string;
  description: string;
  type: InstrumentType;
  pillar: string;
  baseCurrency: string;
  quoteCurrency: string;
  isActive: boolean;
  isBlocked: boolean;
  createdBy: string;
  createdAt: string;
  modifiedBy?: string;
  modifiedAt?: string;
}
```

---

## FAZA 4: FRONTEND - HOOK (60 min)

### Cel:
Implementować pełny hook do zarządzania stanem instrumentów.

### Kroki:

#### 4.1 Implementować `useInstruments.ts`
**Plik:** `frontend/src/hooks/admin/useInstruments.ts`

```typescript
import { useState, useCallback, useEffect } from 'react';
import { Instrument, InstrumentStatus } from '../../types/admin';
import { instrumentsService } from '../../services/admin/instrumentsService';

interface UseInstrumentsOptions {
  initialPage?: number;
  initialPageSize?: number;
}

export const useInstruments = (options?: UseInstrumentsOptions) => {
  const [instruments, setInstruments] = useState<Instrument[]>([]);
  const [totalCount, setTotalCount] = useState(0);
  const [currentPage, setCurrentPage] = useState(options?.initialPage || 1);
  const [totalPages, setTotalPages] = useState(1);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [pageSize, setPageSize] = useState(options?.initialPageSize || 10);

  // Fetch instruments
  const fetchInstruments = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await instrumentsService.getAll(currentPage, pageSize);
      setInstruments(data);
      setTotalCount(data.length);
      setTotalPages(Math.ceil(data.length / pageSize));
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Failed to fetch instruments');
    } finally {
      setLoading(false);
    }
  }, [currentPage, pageSize]);

  // Initial fetch
  useEffect(() => {
    fetchInstruments();
  }, [fetchInstruments]);

  // Add instrument
  const addInstrument = useCallback(
    async (data: {
      symbol: string;
      name: string;
      description: string;
      type: 'Stock' | 'Crypto' | 'Cfd';
      pillar: string;
      baseCurrency: string;
      quoteCurrency: string;
    }) => {
      try {
        setError(null);
        const newInstrument = await instrumentsService.create(data);
        setInstruments((prev) => [newInstrument, ...prev]);
        return newInstrument;
      } catch (err) {
        const errorMsg = err instanceof Error ? err.message : 'Failed to create instrument';
        setError(errorMsg);
        throw err;
      }
    },
    []
  );

  // Update instrument
  const updateInstrument = useCallback(
    async (id: string, data: Partial<Instrument>) => {
      try {
        setError(null);
        const updated = await instrumentsService.update(id, data);
        setInstruments((prev) =>
          prev.map((inst) => (inst.id === id ? updated : inst))
        );
        return updated;
      } catch (err) {
        const errorMsg = err instanceof Error ? err.message : 'Failed to update instrument';
        setError(errorMsg);
        throw err;
      }
    },
    []
  );

  // Delete instrument
  const deleteInstrument = useCallback(async (id: string) => {
    try {
      setError(null);
      await instrumentsService.delete(id);
      setInstruments((prev) => prev.filter((inst) => inst.id !== id));
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to delete instrument';
      setError(errorMsg);
      throw err;
    }
  }, []);

  // Submit for approval
  const submitForApproval = useCallback(
    async (id: string, reason?: string) => {
      try {
        setError(null);
        const result = await instrumentsService.submitForApproval(id, reason);
        setInstruments((prev) =>
          prev.map((inst) =>
            inst.id === id ? { ...inst, status: 'pending' as InstrumentStatus } : inst
          )
        );
        return result;
      } catch (err) {
        const errorMsg = err instanceof Error ? err.message : 'Failed to submit for approval';
        setError(errorMsg);
        throw err;
      }
    },
    []
  );

  // Block instrument
  const blockInstrument = useCallback(async (id: string, reason?: string) => {
    try {
      setError(null);
      const blocked = await instrumentsService.block(id, reason);
      setInstruments((prev) =>
        prev.map((inst) => (inst.id === id ? blocked : inst))
      );
      return blocked;
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to block instrument';
      setError(errorMsg);
      throw err;
    }
  }, []);

  // Unblock instrument
  const unblockInstrument = useCallback(async (id: string, reason?: string) => {
    try {
      setError(null);
      const unblocked = await instrumentsService.unblock(id, reason);
      setInstruments((prev) =>
        prev.map((inst) => (inst.id === id ? unblocked : inst))
      );
      return unblocked;
    } catch (err) {
      const errorMsg = err instanceof Error ? err.message : 'Failed to unblock instrument';
      setError(errorMsg);
      throw err;
    }
  }, []);

  return {
    instruments,
    totalCount,
    currentPage,
    totalPages,
    loading,
    error,
    pageSize,
    setPage: setCurrentPage,
    setPageSize,
    fetchInstruments,
    addInstrument,
    updateInstrument,
    deleteInstrument,
    submitForApproval,
    blockInstrument,
    unblockInstrument,
  };
};
```

---

## FAZA 5: FRONTEND - UI INTEGRATION (120 min)

### Cel:
Uzupełnić komponenty UI i zintegrować z hookiem.

### Kroki:

#### 5.1 Uzupełnić `InstrumentModal.tsx`
**Plik:** `frontend/src/components/admin/Instruments/InstrumentModal.tsx`

```typescript
import React, { useState, useEffect } from 'react';
import { Instrument } from '../../../types/admin';
import './InstrumentModal.css';

interface InstrumentModalProps {
  isOpen: boolean;
  instrument?: Instrument | null;
  onClose: () => void;
  onSave?: (data: any) => Promise<void>;
}

export const InstrumentModal: React.FC<InstrumentModalProps> = ({
  isOpen,
  instrument,
  onClose,
  onSave,
}) => {
  const [formData, setFormData] = useState({
    symbol: '',
    name: '',
    description: '',
    type: 'Stock' as const,
    pillar: 'DEFAULT',
    baseCurrency: 'USD',
    quoteCurrency: 'USD',
  });
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(false);

  useEffect(() => {
    if (instrument) {
      setFormData({
        symbol: instrument.symbol,
        name: instrument.name,
        description: instrument.description,
        type: instrument.type as 'Stock' | 'Crypto' | 'Cfd',
        pillar: instrument.pillar,
        baseCurrency: instrument.baseCurrency,
        quoteCurrency: instrument.quoteCurrency,
      });
    } else {
      setFormData({
        symbol: '',
        name: '',
        description: '',
        type: 'Stock',
        pillar: 'DEFAULT',
        baseCurrency: 'USD',
        quoteCurrency: 'USD',
      });
    }
    setErrors({});
  }, [instrument, isOpen]);

  const validateForm = () => {
    const newErrors: Record<string, string> = {};
    if (!formData.symbol.trim()) newErrors.symbol = 'Symbol jest wymagany';
    if (!formData.name.trim()) newErrors.name = 'Nazwa jest wymagana';
    if (!formData.type) newErrors.type = 'Typ jest wymagany';
    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!validateForm()) return;

    setLoading(true);
    try {
      await onSave?.(formData);
      onClose();
    } catch (error) {
      setErrors({ submit: error instanceof Error ? error.message : 'Błąd' });
    } finally {
      setLoading(false);
    }
  };

  if (!isOpen) return null;

  return (
    <div className="modal-overlay" onClick={onClose}>
      <div className="modal-content" onClick={(e) => e.stopPropagation()}>
        <div className="modal-header">
          <h2>{instrument ? '✏️ Edytuj Instrument' : '➕ Nowy Instrument'}</h2>
        </div>
        <form onSubmit={handleSubmit}>
          <div className="modal-body">
            <div className="form-group">
              <label>Symbol *</label>
              <input
                type="text"
                value={formData.symbol}
                onChange={(e) =>
                  setFormData({ ...formData, symbol: e.target.value.toUpperCase() })
                }
                placeholder="np. AAPL"
                className={errors.symbol ? 'error' : ''}
                disabled={!!instrument}
              />
              {errors.symbol && <span className="error-text">{errors.symbol}</span>}
            </div>

            <div className="form-group">
              <label>Nazwa *</label>
              <input
                type="text"
                value={formData.name}
                onChange={(e) => setFormData({ ...formData, name: e.target.value })}
                placeholder="np. Apple Inc."
                className={errors.name ? 'error' : ''}
              />
              {errors.name && <span className="error-text">{errors.name}</span>}
            </div>

            <div className="form-group">
              <label>Opis</label>
              <textarea
                value={formData.description}
                onChange={(e) => setFormData({ ...formData, description: e.target.value })}
                placeholder="Opcjonalny opis..."
                rows={3}
              />
            </div>

            <div className="form-group">
              <label>Typ *</label>
              <select
                value={formData.type}
                onChange={(e) =>
                  setFormData({
                    ...formData,
                    type: e.target.value as 'Stock' | 'Crypto' | 'Cfd',
                  })
                }
                className={errors.type ? 'error' : ''}
              >
                <option value="Stock">📈 Akcje (Stock)</option>
                <option value="Crypto">₿ Kryptowaluty (Crypto)</option>
                <option value="Cfd">📊 CFD (Cfd)</option>
              </select>
              {errors.type && <span className="error-text">{errors.type}</span>}
            </div>

            <div className="form-row">
              <div className="form-group">
                <label>Waluta Bazowa</label>
                <input
                  type="text"
                  value={formData.baseCurrency}
                  onChange={(e) =>
                    setFormData({ ...formData, baseCurrency: e.target.value.toUpperCase() })
                  }
                  placeholder="USD"
                  maxLength={3}
                />
              </div>

              <div className="form-group">
                <label>Waluta Notowań</label>
                <input
                  type="text"
                  value={formData.quoteCurrency}
                  onChange={(e) =>
                    setFormData({ ...formData, quoteCurrency: e.target.value.toUpperCase() })
                  }
                  placeholder="USD"
                  maxLength={3}
                />
              </div>
            </div>

            {errors.submit && (
              <div className="error-banner">{errors.submit}</div>
            )}
          </div>

          <div className="modal-footer">
            <button type="button" className="btn-cancel" onClick={onClose}>
              Anuluj
            </button>
            <button
              type="submit"
              className="btn-confirm"
              disabled={loading}
            >
              {loading ? '⏳ Zapisywanie...' : '💾 Zapisz'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};
```

#### 5.2 Uzupełnić `InstrumentsContent.tsx`
**Plik:** `frontend/src/components/admin/Instruments/InstrumentsContent.tsx`

*(W załączniku - już mam, tylko trzeba uzupełnić hook calls)*

Główne zmiany:
- Kall `useInstruments()` zamiast pustej wersji
- Obsługa handleAdd, handleEdit, handleDelete
- Obsługa submitForApproval

#### 5.3 Dodać walidację roli
**Plik:** `frontend/src/pages/AdminDashboardPage.tsx`

```typescript
// Sprawdzić czy user ma rolę Admin
const checkAdminRole = () => {
  const token = localStorage.getItem('token');
  if (!token) return false;
  
  try {
    const decoded = JSON.parse(atob(token.split('.')[1]));
    return decoded.is_super_admin === 'true' || decoded.role === 'Admin';
  } catch {
    return false;
  }
};

if (!checkAdminRole()) {
  return <Redirect to="/" />;
}
```

---

## FAZA 6: TESTING & DEBUGGING (90 min)

### Cel:
Przetestować wszystkie funkcjonalności i naprawić błędy.

### Testy manual

e:

#### 6.1 Backend API Tests
```
1. GET /api/admin/instruments
   ✓ Autoryzacja: JWT token z rolą Admin
   ✓ Zwraca listę instrumentów
   ✓ Pagination działa

2. POST /api/admin/instruments
   ✓ Można tworzyć Stock
   ✓ Można tworzyć Crypto
   ✓ Można tworzyć Cfd
   ✓ Walidacja: wymagane pola
   ✓ Duplikat symbol → error

3. PUT /api/admin/instruments/{id}
   ✓ Można edytować draft
   ✓ Nie można edytować approved
   ✓ Aktualizuje ModifiedBy i ModifiedAtUtc

4. POST /api/admin/instruments/{id}/submit-approval
   ✓ Zmienia status na pending
   ✓ Tworzy AdminRequest
   ✓ Logging w AuditLog

5. DELETE /api/admin/instruments/{id}
   ✓ Można usunąć draft
   ✓ Nie można usunąć approved

6. PATCH /api/admin/instruments/{id}/block
   ✓ Blokuje instrument
   ✓ Ustawia IsBlocked=true

7. PATCH /api/admin/instruments/{id}/unblock
   ✓ Odblokuje instrument
   ✓ Ustawia IsBlocked=false
```

#### 6.2 Frontend Tests
```
1. useInstruments Hook
   ✓ Fetch data on mount
   ✓ Add instrument
   ✓ Update instrument
   ✓ Delete instrument
   ✓ Submit for approval
   ✓ Block/Unblock

2. UI Components
   ✓ Modal form validation
   ✓ Error messages display
   ✓ Loading state
   ✓ Table pagination

3. Authentication
   ✓ Role check before display
   ✓ Token refresh on 401
   ✓ Redirect if not admin
```

#### 6.3 Integration Tests
```
1. End-to-end flow
   ✓ Create → Edit → Submit → Approve → Block → Unblock
   ✓ Database updates correctly
   ✓ AuditLog created
   ✓ UI updates in real-time
```

---

## 📊 TIMELINE SUMMARY

```
Wtorek:
  09:00-09:45  Faza 1: DB Migration (45 min)
  09:45-10:45  Faza 2: Backend Endpoints (60 min)
  
  11:00-12:30  Faza 3: Frontend Service (90 min)
  
  13:30-14:30  Faza 4: Frontend Hook (60 min)
  14:30-16:30  Faza 5: Frontend UI (120 min)
  
  16:30-18:00  Faza 6: Testing (90 min)

RAZEM: 7 h 45 min pracy developmentu
```

---

## 🎯 SUCCESS CRITERIA

| Kryterium | Status |
|-----------|--------|
| Admin może tworzyć 3 typy instrumentów | ✓ |
| Pełny CRUD + approval workflow | ✓ |
| Tylko Admin/SuperAdmin mogą zarządzać | ✓ |
| Wszystkie operacje logowane | ✓ |
| UI responsywny i user-friendly | ✓ |
| Zero errors w console | ✓ |
| Performance < 500ms na liście 1000 | ✓ |

---

## 🚀 DEPLOYMENT PLAN

1. **Branch creation**: `feature/instruments-admin-management`
2. **DB Migration**: Przygotować i przetestować migration skrypt
3. **Code review**: PR review na backend + frontend
4. **Staging test**: QA testing na staging env
5. **Production merge**: Merge do main po approval
6. **Rollback plan**: Przygotować revert migration skrypt

---

## 📝 NOTES & ASSUMPTIONS

- Założenie: JWT token zawiera `is_super_admin` lub `role` claim
- Założenie: AuditLog system już istnieje (będzie używany)
- Założenie: AdminRequest system istnieje dla approval flow
- Baza danych: SQL Server (EF Core migrations)
- Frontend: React 18+ z TypeScript
- HTTP client: axios z interceptorami

---

**Status:** GOTOWY DO ZAAPROBOWANIA I WDROŻENIA
