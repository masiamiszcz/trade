# 🔄 FAZA 3 STATE MACHINE RULES

**Status:** CANONICAL DEFINITION  
**Date:** April 22, 2026  
**Version:** 1.0

---

## 📊 STATE DIAGRAM

```
┌──────────────────────────────────────────────────────────────┐
│           INSTRUMENT LIFECYCLE STATE MACHINE                  │
└──────────────────────────────────────────────────────────────┘

                        ┌─────────────┐
                        │    DRAFT    │  ← Created (admin)
                        └──────┬──────┘
                               │
                      RequestApproval by Creator
                               │
                               ▼
                    ┌──────────────────────┐
                    │  PENDING_APPROVAL    │  ← Waiting for review
                    └──────┬──────────┬────┘
                           │          │
           Approve by      │          │      Reject by
           Non-Creator     │          │      Non-Creator
                           │          │
                    ▼      │          ▼
        ┌──────────────┐   │   ┌────────────┐
        │   APPROVED   │───┤   │ REJECTED   │
        └──────┬───────┘   │   └──────┬─────┘
               │           │          │
            ┌──┴───────────┘          │
            │                         │
         Block by ANY      RetrySubmit by Creator
         (admin/trader)               │
            │                         │
            ▼                         ▼
        ┌─────────┐         ┌──────────────┐
        │ BLOCKED │         │ Back to DRAFT│
        └────┬────┘         └──────────────┘
             │
        Unblock by ANY
             │
             ▼
        ┌─────────────┐
        │  APPROVED   │
        └──────┬──────┘
               │
           Archive by ANY
               │
               ▼
        ┌──────────────┐
        │   ARCHIVED   │  ← Final state
        └──────────────┘
```

---

## 🎯 VALID TRANSITIONS (ALL RULES)

### Transition 1: Draft → PendingApproval
- **Name:** RequestApproval
- **Initiator:** Admin (creator or any admin delegating)
- **Preconditions:**
  - Current Status = Draft
  - Description not empty (required)
  - Symbol not empty (required)
- **Postconditions:**
  - Status = PendingApproval
  - ModifiedBy = requesterAdminId
  - ModifiedAtUtc = now
  - RowVersion incremented (optimistic lock)
  - AdminRequest created:
    - Action = "RequestApproval"
    - Status = "pending" (awaiting approval)
    - RequestedByAdminId = requesterAdminId
    - ApprovedByAdminId = null (not yet approved)
    - ApprovedAtUtc = null

### Transition 2: PendingApproval → Approved
- **Name:** Approve
- **Initiator:** Admin (MUST BE DIFFERENT from creator)
- **Preconditions:**
  - Current Status = PendingApproval
  - ApprovedByAdminId ≠ CreatedBy (SELF-APPROVAL BLOCKED)
  - Reason can be optional
- **Postconditions:**
  - Status = Approved
  - ModifiedBy = approvedByAdminId
  - ModifiedAtUtc = now
  - RowVersion incremented
  - AdminRequest updated:
    - Action = "Approve"
    - Status = "approved"
    - ApprovedByAdminId = approvedByAdminId
    - ApprovedAtUtc = now
- **Error:** If approvedByAdminId == CreatedBy → throw InvalidOperationException("Self-approval not allowed")

### Transition 3: PendingApproval → Rejected
- **Name:** Reject
- **Initiator:** Admin (MUST BE DIFFERENT from creator)
- **Preconditions:**
  - Current Status = PendingApproval
  - RejectionReason required (min 10 chars)
  - ApprovedByAdminId ≠ CreatedBy (SELF-APPROVAL BLOCKED)
- **Postconditions:**
  - Status = Rejected
  - ModifiedBy = approvedByAdminId
  - ModifiedAtUtc = now
  - RowVersion incremented
  - AdminRequest updated:
    - Action = "Reject"
    - Status = "rejected"
    - ApprovedByAdminId = approvedByAdminId
    - ApprovedAtUtc = now
    - Reason = rejectionReason (saved for audit)
- **Error:** If approvedByAdminId == CreatedBy → throw InvalidOperationException("Self-approval not allowed")

### Transition 4: Rejected → Draft (Retry)
- **Name:** RetrySubmission
- **Initiator:** Admin (creator or any admin)
- **Preconditions:**
  - Current Status = Rejected
  - Can retry unlimited times
- **Postconditions:**
  - Status = Draft
  - ModifiedBy = adminId
  - ModifiedAtUtc = now
  - RowVersion incremented
  - AdminRequest created:
    - Action = "RetrySubmission"
    - Status = "pending"
    - Reason = "Resubmitting after rejection"

### Transition 5: Approved → Blocked
- **Name:** Block
- **Initiator:** Admin (any - no restriction)
- **Preconditions:**
  - Current Status = Approved (or any non-Archived)
  - IsBlocked = false
- **Postconditions:**
  - Status = Blocked (unchanged, but IsBlocked = true)
  - ModifiedBy = blockedByAdminId
  - ModifiedAtUtc = now
  - RowVersion incremented
  - AdminRequest created:
    - Action = "Block"
    - Status = "approved" (informational - action succeeded)
    - RequestedByAdminId = blockedByAdminId
- **Note:** Blocking is administrative action, not a status transition per se

### Transition 6: Blocked → Approved (Unblock)
- **Name:** Unblock
- **Initiator:** Admin (any)
- **Preconditions:**
  - Current Status = Blocked
  - IsBlocked = true
- **Postconditions:**
  - Status = Approved (back to original)
  - IsBlocked = false
  - ModifiedBy = unblockedByAdminId
  - ModifiedAtUtc = now
  - RowVersion incremented
  - AdminRequest created:
    - Action = "Unblock"
    - Status = "approved"
    - RequestedByAdminId = unblockedByAdminId

### Transition 7: Approved → Archived
- **Name:** Archive
- **Initiator:** Admin (any)
- **Preconditions:**
  - Current Status = Approved (only from approved state)
  - Instrument has been approved for at least 24 hours (optional business rule)
- **Postconditions:**
  - Status = Archived
  - ModifiedBy = archivedByAdminId
  - ModifiedAtUtc = now
  - RowVersion incremented
  - AdminRequest created:
    - Action = "Archive"
    - Status = "approved"
    - RequestedByAdminId = archivedByAdminId

---

## ⛔ ILLEGAL TRANSITIONS (BLOCKED)

| From | To | Reason |
|------|-----|--------|
| Draft | Approved | Must go through PendingApproval |
| Draft | Rejected | Cannot reject draft |
| Draft | Blocked | Must be approved first |
| PendingApproval | Draft | No direct revert (only via Rejected→Draft) |
| PendingApproval | Archived | Must be approved first |
| Rejected | Approved | Must go through Draft→PendingApproval |
| Rejected | Blocked | Must be approved first |
| Rejected | Archived | Must be approved first |
| Blocked | Archived | Must unblock first |
| Blocked | Rejected | Invalid state combination |
| Archived | * | Archived is final state (no transitions out) |

---

## 🔐 AUTHORIZATION RULES

### By Role
- **Admin (Super):** Can execute all transitions
- **Admin (Regular):** Can execute all transitions
- **Trader/User:** Cannot initiate any workflow transitions (blocked by [Authorize(Roles="Admin")])

### By Relationship to Instrument
- **Creator (CreatedBy):** 
  - ✅ Can RequestApproval (Draft → PendingApproval)
  - ❌ CANNOT Approve own submission (self-approval blocked)
  - ✅ Can RetrySubmission after rejection
  - ✅ Can Block/Unblock/Archive (as any admin)

- **Non-Creator Admin:**
  - ✅ Can Approve/Reject submissions
  - ✅ Can Block/Unblock/Archive
  - ✅ Can RequestApproval on behalf (delegated workflow)

---

## 📋 DATA MODEL: AdminRequest Integration

```csharp
public sealed record AdminRequest(
    Guid Id,                                    // PK
    Guid InstrumentId,                         // FK to Instrument
    Guid RequestedByAdminId,                   // Who initiated
    Guid? ApprovedByAdminId,                   // Who approved/rejected (null = pending)
    AdminRequestActionType Action,             // Create|RequestApproval|Approve|Reject|Block|Unblock|Archive|RetrySubmission
    string Reason,                             // Context/notes
    AdminRequestStatus Status,                 // Pending|Approved|Rejected (for workflow state)
    DateTimeOffset CreatedAtUtc,               // When requested
    DateTimeOffset? ApprovedAtUtc              // When approved/rejected
);

public enum AdminRequestActionType
{
    Create = 1,
    RequestApproval = 2,
    Approve = 3,
    Reject = 4,
    Block = 5,
    Unblock = 6,
    Archive = 7,
    RetrySubmission = 8
}

public enum AdminRequestStatus
{
    Pending = 1,      // Awaiting action
    Approved = 2,     // Action succeeded
    Rejected = 3      // Action failed/declined
}
```

---

## 🎯 SERVICE LAYER METHODS (IInstrumentService)

```csharp
// Existing (FAZA 2)
Task<InstrumentDto> GetByIdAsync(Guid id, CancellationToken ct);
Task<InstrumentDto> CreateAsync(CreateInstrumentRequest req, Guid adminId, CancellationToken ct);
Task<InstrumentDto> UpdateAsync(Guid id, UpdateInstrumentRequest req, Guid adminId, CancellationToken ct);
Task<InstrumentDto> BlockAsync(Guid id, Guid adminId, CancellationToken ct);
Task<InstrumentDto> UnblockAsync(Guid id, Guid adminId, CancellationToken ct);
Task DeleteAsync(Guid id, Guid adminId, CancellationToken ct);

// New (FAZA 3)
Task<InstrumentDto> RequestApprovalAsync(Guid id, Guid adminId, CancellationToken ct);
Task<InstrumentDto> ApproveAsync(Guid id, Guid approvedByAdminId, CancellationToken ct);
Task<InstrumentDto> RejectAsync(Guid id, string rejectionReason, Guid rejectedByAdminId, CancellationToken ct);
Task<InstrumentDto> RetrySubmissionAsync(Guid id, Guid adminId, CancellationToken ct);
Task<InstrumentDto> ArchiveAsync(Guid id, Guid adminId, CancellationToken ct);
```

---

## 🧪 VALIDATION IMPLEMENTATION

All transitions validated in Service layer **before** any database operation:

```csharp
private void ValidateTransition(
    InstrumentStatus currentStatus,
    InstrumentStatus targetStatus,
    Guid? approverAdminId,
    Guid creatorAdminId)
{
    // Check if transition is legal
    if (!IsTransitionAllowed(currentStatus, targetStatus))
        throw new InvalidOperationException($"Cannot transition from {currentStatus} to {targetStatus}");
    
    // Check self-approval
    if (IsApprovalTransition(targetStatus) && approverAdminId == creatorAdminId)
        throw new InvalidOperationException("Self-approval not allowed");
    
    // Additional business logic...
}

private bool IsTransitionAllowed(
    InstrumentStatus from,
    InstrumentStatus to) =>
    (from, to) switch
    {
        (InstrumentStatus.Draft, InstrumentStatus.PendingApproval) => true,
        (InstrumentStatus.PendingApproval, InstrumentStatus.Approved) => true,
        (InstrumentStatus.PendingApproval, InstrumentStatus.Rejected) => true,
        (InstrumentStatus.Rejected, InstrumentStatus.Draft) => true,
        (InstrumentStatus.Approved, InstrumentStatus.Blocked) => true,
        (InstrumentStatus.Blocked, InstrumentStatus.Approved) => true,
        (InstrumentStatus.Approved, InstrumentStatus.Archived) => true,
        _ => false
    };
```

---

## 📌 FUTURE-PROOF ARCHITECTURE

This design allows future additions **without refactoring core**:

```
Current (FAZA 3):          Future (FAZA 4+):
┌──────────────────┐      ┌──────────────────────────┐
│ Service Layer    │      │ Service Layer            │
│ (Validation)     │  →   │ + Event Bus Integration  │
└──────────────────┘      └──────────────────────────┘
                                    ↓
                          ┌──────────────────────────┐
                          │ Event Handlers (optional)│
                          │ - Notifications          │
                          │ - Analytics              │
                          │ - External APIs          │
                          └──────────────────────────┘
```

No domain changes needed — just add event publishing in service layer.

---

## ✅ READY FOR IMPLEMENTATION

Architecture is **production-ready**, **scalable**, **auditable**, **compliant with DDD**.

Next: Extend IInstrumentService + InstrumentService implementation.
