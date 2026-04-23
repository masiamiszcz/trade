# APPROVALS SYSTEM AUDIT

**Date:** April 22, 2026  
**Status:** ⚠️ PARTIAL (Backend ✅ | Frontend ⚠️)  
**Priority:** HIGH - System is partially broken due to endpoint mismatch

---

## 📋 EXECUTIVE SUMMARY

The trading platform has implemented a **sophisticated two-tier approval system** with full backend logic, but the **frontend is disconnected** due to critical issues:

| Layer | Status | Assessment |
|-------|--------|------------|
| **Backend Models** | ✅ COMPLETE | Instrument status machine + AdminRequest model fully implemented |
| **Backend Services** | ✅ COMPLETE | ApprovalService + InstrumentService with permission checks |
| **Backend Endpoints** | ✅ COMPLETE | All approval endpoints implemented with validation |
| **Backend Audit Trail** | ✅ COMPLETE | Full logging of all operations with IP tracking |
| **Self-Approval Prevention** | ✅ COMPLETE | Enforced in service layer (400 error thrown) |
| **Frontend Types** | ✅ COMPLETE | All TypeScript interfaces exist |
| **Frontend Services** | ⚠️ PARTIAL | Endpoints wrong (404 errors in production) |
| **Frontend Hooks** | ⚠️ PARTIAL | `useAdminRequests` hook is stub only |
| **Frontend UI Components** | ❌ MISSING | Approvals tab + instrument approval buttons not functional |

---

## 🏗️ BACKEND ARCHITECTURE

### **TWO APPROVAL SYSTEMS**

#### **System 1: Block/Unblock Requests** (One-step approval)
```
Purpose: Urgent instrument blocking/unblocking
Flow:   Admin Request → Different Admin Reviews → Approve/Reject
Model:  AdminRequest (InstrumentId, Action: Block|Unblock, Status: Pending|Approved|Rejected)
Audit:  Full logging with IP address
```

#### **System 2: Instrument Status Workflow** (Multi-step approval)
```
Purpose: Create/modify instruments through approval chain
Flow:   Draft → PendingApproval → Approved → Archived
        ↓                ↓
      Rejected ─→ Draft (retry)
Model:  Instrument.Status enum (6 states)
Audit:  AdminRequest records for transitions
```

### **KEY BACKEND IMPLEMENTATION DETAILS**

**File:** [backend/TradingPlatform.Core/Models/AdminRequest.cs]
- `id` (Guid)
- `instrumentId` (Guid)
- `requestedByAdminId` (Guid)
- `approvedByAdminId?` (Guid)
- `action` (enum: Block|Unblock|Create|RequestApproval|Approve|Reject|Archive|RetrySubmission)
- `status` (enum: Pending|Approved|Rejected)
- `reason?` (string, for rejections)
- `createdAtUtc`, `approvedAtUtc?`

**File:** [backend/TradingPlatform.Core/Models/Instrument.cs]
- `status` (enum: Draft|PendingApproval|Approved|Rejected|Blocked|Archived)
- `createdBy` (AdminId)
- `isBlocked` (bool)
- `rowVersion` (for optimistic locking)

### **BACKEND ENDPOINTS**

#### **Instrument Workflow Endpoints** (AdminController)
```
POST   /api/admin/instruments/{id}/request-approval      → Draft → PendingApproval
POST   /api/admin/instruments/{id}/approve               → PendingApproval → Approved
POST   /api/admin/instruments/{id}/reject                → PendingApproval → Rejected
POST   /api/admin/instruments/{id}/retry-submission      → Rejected → Draft
POST   /api/admin/instruments/{id}/archive               → Approved → Archived
```

#### **Block/Unblock Request Endpoints** (AdminController)
```
POST   /api/admin/instruments/{id}/request-block         → Create AdminRequest (Block)
POST   /api/admin/instruments/{id}/request-unblock       → Create AdminRequest (Unblock)
PATCH  /api/admin/requests/{requestId}/approve           → Approve request
PATCH  /api/admin/requests/{requestId}/reject            → Reject request
```

#### **Query Endpoints**
```
GET    /api/admin/requests                               → All requests (paginated)
GET    /api/admin/requests/pending                       → Pending requests only
GET    /api/admin/requests/{requestId}                   → Single request details
```

### **SELF-APPROVAL PREVENTION (Backend)**

**Implementation:** [InstrumentService.cs] line 280, [AdminService.cs] line 260
```csharp
if (request.RequestedByAdminId == approvedByAdminId)
    throw new InvalidOperationException("An admin cannot approve their own request");
```

**Behavior:**
- ✅ Regular admin: BLOCKED from approving own requests (400 BadRequest)
- ✅ Super admin: Currently BLOCKED (same logic applies to all)
- 🔴 **ISSUE:** No role-based exception for SuperAdmin (per requirements, SuperAdmin should be able to)

---

## 🖥️ FRONTEND STATUS

### **WHAT EXISTS & WORKS**

✅ **Types** ([frontend/src/types/admin.ts])
- Instrument, AdminRequest, InstrumentStatus, AdminRequestStatus, AdminRequestActionType
- All enums match backend perfectly

✅ **API Service** ([frontend/src/services/admin/instrumentsService.ts])
- Methods exist: `requestApproval()`, `approve()`, `reject()`, `retrySubmission()`, `archive()`
- Methods exist: `getAllAdminRequests()`, `getPendingAdminRequests()`

✅ **useInstruments Hook** ([frontend/src/hooks/admin/useInstruments.ts])
- Fully implemented with all workflow methods
- Manages loading/error states
- Used by InstrumentsContent component

✅ **Dashboard Component** ([frontend/src/components/admin/Dashboard/DashboardContent.tsx])
- Displays pending/approved/rejected counts
- Pulls real data from API

✅ **UI Components Exist**
- [ApprovalsContent.tsx] - Tab component for approvals
- [ApprovalActionModal.tsx] - Modal for approve/reject with comment
- [InstrumentsContent.tsx] - Instruments table with status display

### **WHAT'S BROKEN / INCOMPLETE**

🔴 **CRITICAL: Endpoint Mismatch**
**File:** [frontend/src/services/admin/instrumentsService.ts] lines 191, 201, 213

Current URLs:
```typescript
getAllAdminRequests()  →  '/admin/admin-requests'              // ❌ WRONG
getPendingAdminRequests()  →  '/admin/admin-requests/pending'  // ❌ WRONG
getAdminRequestById()  →  `/admin/admin-requests/${id}`         // ❌ WRONG
```

Correct URLs (per backend):
```typescript
getAllAdminRequests()  →  '/admin/requests'              // ✅ CORRECT
getPendingAdminRequests()  →  '/admin/requests/pending'  // ✅ CORRECT
getAdminRequestById()  →  `/admin/requests/${id}`         // ✅ CORRECT
```

**Impact:** All requests to fetch admin requests return 404 in production

---

⚠️ **useAdminRequests Hook - STUB ONLY**
**File:** [frontend/src/hooks/admin/useAdminRequests.ts]

Lines 23-25:
```typescript
approveRequest: async (requestId: string) => {
  console.log('TODO: Implement approval');  // ❌ NOT IMPLEMENTED
  // No actual API call
},
rejectRequest: async (requestId: string, comment?: string) => {
  console.log('TODO: Implement rejection');  // ❌ NOT IMPLEMENTED
  // No actual API call
},
```

**Impact:** ApprovalsTab component cannot actually approve/reject requests

---

❌ **No Approval UI in Instruments Table**
**File:** [frontend/src/components/admin/Instruments/InstrumentsContent.tsx]

- Instruments with `status === 'PendingApproval'` are shown
- ❌ No "Approve" button for pending instruments
- ❌ No "Reject" button with modal
- Users must navigate elsewhere (non-existent approvals UI)

**Impact:** Cannot approve/reject instruments through primary workflow UI

---

❌ **ApprovalsContent Component Not Functional**
**File:** [frontend/src/components/admin/Approvals/ApprovalsContent.tsx]

Structure exists but:
- ❌ Calls stub hook methods
- ❌ No loading of pending requests
- ❌ No filtering/display logic
- ❌ Modal is prepared but not wired

**Impact:** Approvals tab shows nothing, users see empty state

---

⚠️ **No Self-Approval Check on Frontend**
- No client-side validation before showing approve button
- Users can attempt to approve own requests
- Backend throws error (400), but UX is poor
- No disabled button state, no warning message

**Impact:** Poor UX - user clicks, gets error message

---

## 🔐 PERMISSION CHECKS

### **Backend Permission Enforcement** ✅

**Implemented in:**
- [AdminService.cs] ApproveRequestAsync() - line 260
- [InstrumentService.cs] ApproveAsync() - line 280
- [InstrumentService.cs] RejectAsync() - line 318

**Logic:**
```
IF currentAdmin.id === requestCreatorId:
  IF currentAdmin.role !== 'SUPER_ADMIN':
    THROW InvalidOperationException("Self-approval not allowed")
  ELSE:
    ALLOW (but currently SUPER_ADMIN also blocked - bug)
```

**Current Behavior:**
- ✅ Regular admin: Cannot approve own requests
- ❌ Super admin: Currently ALSO cannot approve own requests (wrong - spec says should allow)

**Issue:** No role-based exception in service logic

---

### **Frontend Permission Enforcement** ❌

Currently:
- ❌ No check if `currentAdmin.id === request.requestedBy`
- ❌ Approve button always shown (if modal implemented)
- ❌ Backend will reject with error

**Missing:** Client-side check to disable button with message:
```typescript
const canApproveThisRequest = 
  currentAdmin.id !== request.requestedByAdminId || 
  currentAdmin.role === 'SUPER_ADMIN';
```

---

## 📊 DATA FLOW ANALYSIS

### **Current Block/Unblock Flow** (partially working)
```
1. Admin clicks "Request Block"
   ↓
2. POST /api/admin/instruments/{id}/request-block
   ↓
3. Creates AdminRequest with Status=Pending ✅
   ↓
4. Frontend list needs: GET /api/admin/requests/pending
   → ❌ ENDPOINT MISMATCH: calls '/admin/admin-requests/pending' 
   → Result: 404 error
   ↓
5. Different admin cannot see pending requests (fail)
   ↓
6. Cannot PATCH /api/admin/requests/{id}/approve ❌
```

**Status:** Broken at step 4

---

### **Current Instrument Approval Flow** (partial)
```
1. Admin creates instrument
   ↓
2. POST /api/admin/instruments (Status=Draft) ✅
   ↓
3. Admin requests approval
   ↓
4. POST /api/admin/instruments/{id}/request-approval ✅
   (Status: Draft → PendingApproval) ✅
   ↓
5. Different admin needs to approve
   ↓
6. UI: Must click "Approve" on instrument in table
   → ❌ NO BUTTON EXISTS
   → User has no way to approve ❌
   ↓
7. POST /api/admin/instruments/{id}/approve
   → Backend ready ✅, frontend cannot call ❌
```

**Status:** Broken at step 6 (no UI)

---

## 🎯 PROBLEM SUMMARY

| Problem | Severity | Component | Impact |
|---------|----------|-----------|--------|
| Endpoint mismatch (`/admin/admin-requests` → `/admin/requests`) | 🔴 CRITICAL | Frontend service | 404 errors, cannot fetch requests |
| `useAdminRequests` hook not implemented | 🔴 CRITICAL | Frontend hook | Approvals UI cannot function |
| No approve/reject buttons in instruments table | 🔴 CRITICAL | Frontend UI | Cannot approve pending instruments |
| ApprovalsContent component is stub | 🔴 CRITICAL | Frontend component | Approvals tab is non-functional |
| No self-approval check frontend | 🟡 MEDIUM | Frontend validation | Poor UX (backend error instead of disabled button) |
| Super admin cannot approve own requests | 🟡 MEDIUM | Backend service | Violates spec (should be allowed for super admin) |
| No request type filtering UI | 🟡 MEDIUM | Frontend UI | Cannot distinguish block vs unblock requests |
| No confirmation dialogs | 🟡 MEDIUM | Frontend UX | Destructive actions not confirmed |

---

## ✅ WHAT WORKS

- ✅ Backend models & migrations (both approval systems)
- ✅ Self-approval prevention logic (though super admin exception missing)
- ✅ All backend endpoints with proper validation
- ✅ Audit logging (full trail with IP)
- ✅ Instrument state transitions (Draft → PendingApproval → Approved → Archived)
- ✅ Block/unblock request flow (API layer only)
- ✅ Error handling & validation
- ✅ Dashboard counts widget (pending/approved/rejected)

---

## 🔧 SOLUTIONS & RECOMMENDATIONS

### **PRIORITY 1: CRITICAL FIXES** (Required for system to work)

#### **1.1 Fix Endpoint URLs**
**File:** `frontend/src/services/admin/instrumentsService.ts`

Replace lines 191, 201, 213:
```typescript
// BEFORE
const response = await httpClient.fetch<AdminRequest[]>({
  url: '/admin/admin-requests',  // ❌ WRONG

// AFTER
const response = await httpClient.fetch<AdminRequest[]>({
  url: '/admin/requests',  // ✅ CORRECT
```

**Lines to change:**
- Line 191: `'/admin/admin-requests'` → `'/admin/requests'`
- Line 201: `'/admin/admin-requests/pending'` → `'/admin/requests/pending'`
- Line 213: `'/admin/admin-requests/${id}'` → `'/admin/requests/${id}'`

**Effort:** 5 minutes  
**Impact:** Enables all admin request fetching

---

#### **1.2 Implement useAdminRequests Hook**
**File:** `frontend/src/hooks/admin/useAdminRequests.ts`

Replace stub methods with actual API calls:

```typescript
approveRequest: async (requestId: string, comment?: string) => {
  try {
    setLoading(true);
    setError(null);
    const response = await httpClient.fetch<AdminRequest>({
      url: `/admin/requests/${requestId}/approve`,
      method: 'PATCH',
      body: { comment },
    });
    // Refetch after approval
    await fetchPendingRequests();
    return response;
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    setError(error);
    throw error;
  } finally {
    setLoading(false);
  }
},

rejectRequest: async (requestId: string, comment: string) => {
  try {
    setLoading(true);
    setError(null);
    const response = await httpClient.fetch<AdminRequest>({
      url: `/admin/requests/${requestId}/reject`,
      method: 'PATCH',
      body: { comment },
    });
    // Refetch after rejection
    await fetchPendingRequests();
    return response;
  } catch (err) {
    const error = err instanceof Error ? err : new Error(String(err));
    setError(error);
    throw error;
  } finally {
    setLoading(false);
  }
},
```

**Effort:** 20 minutes  
**Impact:** Enables actual approval/rejection operations

---

#### **1.3 Add Approve/Reject UI to Instruments Table**
**File:** `frontend/src/components/admin/Instruments/InstrumentsContent.tsx`

Add action column for PendingApproval instruments:

```typescript
// Add new column
{
  key: 'actions',
  label: 'Akcje',
  width: '15%',
  render: (_, row: Instrument) => {
    if (row.status === 'PendingApproval') {
      return (
        <div className="action-buttons">
          <button 
            onClick={() => handleApprove(row.id)}
            className="btn-approve"
            disabled={loading}
          >
            Zatwierdź
          </button>
          <button 
            onClick={() => setSelectedRejectId(row.id)}
            className="btn-reject"
            disabled={loading}
          >
            Odrzuć
          </button>
        </div>
      );
    }
    return '--';
  },
}
```

**Effort:** 30 minutes  
**Impact:** Users can approve/reject from primary UI

---

#### **1.4 Connect ApprovalsContent Component**
**File:** `frontend/src/components/admin/Approvals/ApprovalsContent.tsx`

Replace stub with real data:

```typescript
useEffect(() => {
  const loadRequests = async () => {
    try {
      const data = await adminRequestsService.getPendingAdminRequests();
      setPendingRequests(data || []);
    } catch (err) {
      setError(err instanceof Error ? err : new Error(String(err)));
    }
  };
  loadRequests();
}, []);
```

**Effort:** 20 minutes  
**Impact:** Approvals tab becomes functional

---

### **PRIORITY 2: IMPORTANT IMPROVEMENTS** (Better UX)

#### **2.1 Add Self-Approval Check**
**File:** `frontend/src/components/admin/Instruments/InstrumentsContent.tsx`

```typescript
const canApproveInstrument = (instrument: Instrument): boolean => {
  if (!currentAdmin) return false;
  return currentAdmin.id !== instrument.createdBy || 
         currentAdmin.role === 'SUPER_ADMIN';
};

// In render:
<button
  disabled={!canApproveInstrument(row)}
  title={currentAdmin?.id === row.createdBy ? "Nie możesz zatwierdzić własnych zmian" : ""}
>
  Zatwierdź
</button>
```

**Effort:** 15 minutes  
**Impact:** Better UX - button disabled with tooltip instead of API error

---

#### **2.2 Add Confirmation Dialogs**
**File:** `frontend/src/components/admin/Approvals/ApprovalsContent.tsx`

```typescript
const handleApprove = (requestId: string) => {
  if (window.confirm('Czy na pewno chcesz zatwierdź ć tę zmianę?')) {
    approveRequest(requestId);
  }
};
```

**Effort:** 10 minutes  
**Impact:** Prevent accidental approvals

---

#### **2.3 Add Toast Notifications**
After successful approval/rejection:

```typescript
showToast('Zmiana zatwierdzona', 'success');
```

**Effort:** 10 minutes  
**Impact:** User feedback on operation success

---

### **PRIORITY 3: BACKEND FIX** (Permissions logic)

#### **3.1 Allow Super Admin to Approve Own Requests**
**File:** `backend/TradingPlatform.Core/Services/AdminService.cs` line 260

Replace:
```csharp
// BEFORE
if (request.RequestedByAdminId == approvedByAdminId)
    throw new InvalidOperationException("An admin cannot approve their own request");

// AFTER
var currentAdmin = await _context.AdminAccounts.FirstOrDefaultAsync(a => a.Id == approvedByAdminId);
if (request.RequestedByAdminId == approvedByAdminId && currentAdmin?.Role != AdminRole.SuperAdmin)
    throw new InvalidOperationException("An admin cannot approve their own request");
```

Also in [InstrumentService.cs] lines 280, 318 - same pattern.

**Effort:** 5 minutes  
**Impact:** Super admin can approve own requests (per spec)

---

## 📋 IMPLEMENTATION ROADMAP

**Phase 1 (Today):** Fix critical endpoint + implement hooks
1. Fix endpoint URLs (5 min)
2. Implement useAdminRequests hook (20 min)
3. Test in ApprovalsTab (10 min)

**Phase 2 (Today):** Add UI buttons
4. Add approve/reject to instruments table (30 min)
5. Wire up ApprovalActionModal (20 min)
6. Test full approval flow (15 min)

**Phase 3 (Optional):** Polish
7. Add self-approval check (15 min)
8. Add confirmation dialogs (10 min)
9. Add toast notifications (10 min)
10. Backend super admin fix (5 min)

**Total Time:** 2-3 hours for core functionality

---

## 🧪 TESTING CHECKLIST

- [ ] Get pending requests: `GET /api/admin/requests/pending` returns data
- [ ] Approve request: `PATCH /api/admin/requests/{id}/approve` succeeds
- [ ] Reject request: `PATCH /api/admin/requests/{id}/reject` succeeds
- [ ] Self-approval prevented (regular admin cannot approve own request)
- [ ] Instrument table shows approve/reject buttons for PendingApproval items
- [ ] Clicking approve changes status to Approved
- [ ] Clicking reject shows modal with comment field
- [ ] ApprovalsTab loads and displays pending requests
- [ ] Dashboard counts update after approval
- [ ] Audit log records all operations

---

## 📌 SUMMARY

**Current State:**
- Backend: ✅ Production-ready
- Frontend: ❌ Broken (3 critical issues)

**To Make Work:**
- Fix 3 endpoint URLs (5 min)
- Implement 1 hook (20 min)
- Add UI buttons (30 min)
- Test (15 min)
- **Total: ~1 hour for MVP**

**System Design Quality:** ⭐⭐⭐⭐⭐ Excellent
- Proper two-tier approval design
- Full audit trail
- Self-approval prevention
- State machine for instruments

**Implementation Status:** 60% (backend done, frontend incomplete)

---

**Recommendation:** Implement Priority 1 fixes immediately to unlock approval functionality. System architecture is solid; just needs frontend connection.

