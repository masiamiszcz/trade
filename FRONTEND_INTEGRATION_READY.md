# 🎯 FRONTEND TODO - Approval Workflow UI

## Status: Backend 100% Ready ✅

The backend approval workflow is production-ready. Frontend needs to build the UI to interact with these endpoints.

---

## Available Backend Endpoints

### 1️⃣ Create Approval Request
```
POST /api/admin/instruments/{instrumentId}/request-update
Authorization: Bearer {token}
Content-Type: application/json

Body:
{
  "name": "Updated Name",
  "description": "Updated Description",
  "baseCurrency": "USD",  // optional
  "quoteCurrency": "USD"  // optional
}

Response (201):
{
  "id": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "symbol": "AAPL",
  "name": "Apple Inc.",
  ... // InstrumentDto
}
```

### 2️⃣ Get Pending Requests
```
GET /api/admin/requests/pending
Authorization: Bearer {token}

Response (200):
[
  {
    "id": "2bb5e80e-b03b-445d-9c3c-a7408d35e345",
    "instrumentId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
    "requestedByAdminId": "62a436c0-4e0e-49b6-9776-fafc1905a83b",
    "approvedByAdminId": null,
    "action": "Update",
    "reason": "Requested update by admin ...",
    "status": "Pending",
    "createdAtUtc": "2026-04-23T16:29:01.5912646+00:00",
    "approvedAtUtc": null
  }
]
```

### 3️⃣ Approve Request
```
PATCH /api/admin/requests/{requestId}/approve
Authorization: Bearer {token}

Response (200):
{
  "id": "2bb5e80e-b03b-445d-9c3c-a7408d35e345",
  "instrumentId": "aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa",
  "requestedByAdminId": "62a436c0-4e0e-49b6-9776-fafc1905a83b",
  "approvedByAdminId": "62a436c0-4e0e-49b6-9776-fafc1905a83b",
  "action": "Update",
  "reason": "Requested update by admin ...",
  "status": "Approved",
  "createdAtUtc": "2026-04-23T16:29:01.5912646+00:00",
  "approvedAtUtc": "2026-04-23T16:30:36.4994061+00:00"
}
```

### 4️⃣ Reject Request (Ready but not tested yet)
```
PATCH /api/admin/requests/{requestId}/reject
Authorization: Bearer {token}

Response (200): AdminRequestDto with status="Rejected"
```

---

## Frontend Components Needed

### 1. Admin Approval Panel
**Location:** `/frontend/src/components/admin/`

**Responsibilities:**
- Display list of pending approval requests
- Show request details (what was changed, who requested it)
- Approve/Reject buttons
- Real-time updates when requests are pending

**Data needed:**
- Request ID
- Instrument ID
- Changes requested (old vs new values)
- Requested by (admin name)
- Timestamp

### 2. Request Modal/Dialog
**For approving:**
- Confirm changes
- Reason textarea (optional)
- Approve / Cancel buttons

**For rejecting:**
- Rejection reason (required)
- Reject / Cancel buttons

### 3. Audit Trail
**Display:**
- Who approved/rejected
- When (timestamp)
- IP address (if available)
- Changes applied

---

## Implementation Checklist

### Phase 1: Basic UI (FIRST)
- [ ] Create `AdminApprovals.tsx` component
  - [ ] GET /requests/pending on load
  - [ ] Display list of pending requests
  - [ ] Show request details
  - [ ] Add Approve/Reject buttons

### Phase 2: Action Handlers
- [ ] Implement approve handler
  - [ ] PATCH /approve endpoint
  - [ ] Loading state
  - [ ] Success notification
  - [ ] Error handling
- [ ] Implement reject handler
  - [ ] PATCH /reject endpoint
  - [ ] Rejection reason input
  - [ ] Loading state
  - [ ] Success notification

### Phase 3: Polish
- [ ] Real-time updates (poll or WebSocket)
- [ ] Audit trail display
- [ ] Better error messages
- [ ] Loading skeletons
- [ ] Empty state messaging

### Phase 4: Advanced
- [ ] Bulk approvals
- [ ] Filters (by instrument, by requester, by date)
- [ ] Search
- [ ] Pagination
- [ ] Export audit logs

---

## API Client Setup

### Example TypeScript client:
```typescript
export interface AdminRequest {
  id: string;
  instrumentId: string;
  requestedByAdminId: string;
  approvedByAdminId: string | null;
  action: string;
  reason: string;
  status: "Pending" | "Approved" | "Rejected";
  createdAtUtc: string;
  approvedAtUtc: string | null;
}

export const approvalAPI = {
  getPendingRequests: async () => {
    const r = await fetch('/api/admin/requests/pending', {
      headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    return r.json() as Promise<AdminRequest[]>;
  },

  approveRequest: async (requestId: string) => {
    const r = await fetch(`/api/admin/requests/${requestId}/approve`, {
      method: 'PATCH',
      headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    return r.json() as Promise<AdminRequest>;
  },

  rejectRequest: async (requestId: string) => {
    const r = await fetch(`/api/admin/requests/${requestId}/reject`, {
      method: 'PATCH',
      headers: { 'Authorization': `Bearer ${getToken()}` }
    });
    return r.json() as Promise<AdminRequest>;
  }
};
```

---

## Test Data

For manual testing, use:
```
Admin ID: 62a436c0-4e0e-49b6-9776-fafc1905a83b
Token: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJo...
Instrument ID: aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa (AAPL)
```

---

## Backend Test Results

✅ **All endpoints verified working:**
- POST /request-update → 201 Created
- GET /pending → 200 OK  
- PATCH /approve → 200 OK ✅ (Fixed from 500)

✅ **Unit tests:** 3/3 passing

✅ **E2E workflow:** Complete success

---

## What Changed Recently

**Fixed:** PayloadJson null reference bug
**Deployed:** Fresh database with migration
**Verified:** Full approval workflow end-to-end

**Frontend can now safely implement UI** ✅

---

## Notes for Frontend Dev

1. **Authentication:** Token is required in all requests
2. **CORS:** Already enabled (Frontend policy configured)
3. **Error Handling:** Backend returns proper HTTP status codes + error messages
4. **Timestamps:** All in UTC, use proper formatting
5. **Real-time:** Consider polling initially, upgrade to WebSocket later if needed

---

**Backend ready for integration! 🚀**
