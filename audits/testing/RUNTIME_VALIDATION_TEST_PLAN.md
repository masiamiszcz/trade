/**
 * RUNTIME VALIDATION TEST PLAN
 * Simulating HTTP requests and expected responses
 * Testing implemented user management system
 * 
 * Focus: Validate actual behavior matches design
 * Scope: Login flow, Block/Delete/Approval workflows, Security, Data visibility
 */

// ============================================================================
// 1. LOGIN FLOW TESTS
// ============================================================================

/**
 * TEST 1.1: Login as ACTIVE user
 * Expected: 200 + JWT token
 */
POST /api/auth/login
Content-Type: application/json

{
  "username": "john_active",
  "password": "SecurePass123!"
}

EXPECTED RESPONSE (200 OK):
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "d1234567-89ab-cdef-0123-456789abcdef",
  "username": "john_active",
  "role": "User",
  "twoFactorEnabled": false
}

VALIDATION:
✅ Token issued
✅ User can proceed with authenticated requests
✅ Claims contain correct userId, username, role


/**
 * TEST 1.2: Login as BLOCKED user
 * Expected: 200 + token + block info in response
 * 
 * ISSUE TO CHECK:
 * - Does UserAuthService check user.IsBlocked during login?
 * - If user is blocked, should login succeed but return block info?
 * - Or should login fail with specific error?
 * 
 * CURRENT EXPECTED BEHAVIOR (based on typical platform design):
 * - Login succeeds (User exists, password correct)
 * - Response includes block information for UI to display
 * - Token issued with "blocked" claim
 * - Frontend blocks operations client-side
 */
POST /api/auth/login
Content-Type: application/json

{
  "username": "john_blocked",
  "password": "SecurePass123!"
}

EXPECTED RESPONSE (200 OK - with block info):
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "d2234567-89ab-cdef-0123-456789abcdef",
  "username": "john_blocked",
  "isBlocked": true,
  "blockedUntilUtc": "2026-04-29T14:30:00Z",
  "blockReason": "Suspected fraudulent activity - pending review",
  "message": "Your account has been restricted. Please contact support."
}

VALIDATION CHECKLIST:
❓ Is UserAuthService checking user.IsBlocked property?
❓ Does login response include block details?
❓ Is JWT token issued for blocked user (with blocked claim)?
❓ Can blocked user access APIs (token valid but operations blocked)?
❓ Does frontend display block reason to user?

⚠️ POTENTIAL ISSUE:
- If login returns 401 for blocked user → user sees generic "Invalid credentials"
- User won't know account is blocked vs password wrong
- Consider returning 403 Forbidden with block reason for better UX


/**
 * TEST 1.3: Login as DELETED user
 * Expected: 401 Unauthorized - generic message
 * 
 * REASON:
 * - Deleted user Status = Deleted, not in active user pool
 * - Should behave like "user doesn't exist"
 * - Generic 401 prevents username enumeration attacks
 */
POST /api/auth/login
Content-Type: application/json

{
  "username": "john_deleted",
  "password": "SecurePass123!"
}

EXPECTED RESPONSE (401 Unauthorized):
{
  "error": "Invalid credentials",
  "message": "Username or password is incorrect"
}

VALIDATION CHECKLIST:
✅ GetByUsernameAndPasswordAsync must filter by Status != Deleted
❓ Is UserRepository.GetByUsernameAsync checking Status?
❓ Does UserAuthService explicitly check user.IsDeleted before auth?
⚠️ POTENTIAL ISSUE:
- If deletion is just soft-delete, repository queries must filter
- Otherwise deleted user can still login


// ============================================================================
// 2. BLOCK FLOW TESTS
// ============================================================================

/**
 * TEST 2.1: Admin blocks active user
 * Expected: 200 + user Status = Blocked
 */
POST /api/admin/users/d1234567-89ab-cdef-0123-456789abcdef/block
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "reason": "Violation of trading terms - excessive bot activity",
  "durationMs": 172800000,  // 48 hours
  "isPermanent": false
}

EXPECTED RESPONSE (200 OK):
{
  "id": "d1234567-89ab-cdef-0123-456789abcdef",
  "userName": "john_active",
  "email": "john@example.com",
  "firstName": "John",
  "lastName": "Doe",
  "role": "User",
  "status": "Blocked",
  "blockReason": "Violation of trading terms - excessive bot activity",
  "blockedUntilUtc": "2026-04-29T14:30:00Z",
  "isBlocked": true,
  "createdAtUtc": "2026-01-15T10:00:00Z"
}

VALIDATION CHECKLIST:
✅ Status changed to "Blocked"
✅ BlockReason stored
✅ BlockedUntilUtc set to now + durationMs
✅ AuditLog created with AdminId, reason, timestamp
❓ Response includes new Status and block details


/**
 * TEST 2.2: Blocked user attempts to login
 * Expected: 200 + block info (user exists, blocked)
 */
POST /api/auth/login
Content-Type: application/json

{
  "username": "john_active",
  "password": "SecurePass123!"
}

EXPECTED RESPONSE (200 OK):
{
  "token": "...",
  "userId": "d1234567-89ab-cdef-0123-456789abcdef",
  "isBlocked": true,
  "blockedUntilUtc": "2026-04-29T14:30:00Z",
  "blockReason": "Violation of trading terms - excessive bot activity",
  "message": "Your account has been temporarily restricted until 2026-04-29"
}

VALIDATION CHECKLIST:
✅ Login still works (user exists, credentials valid)
✅ Response includes block details
✅ System indicates block is temporary


/**
 * TEST 2.3: Blocked user tries to place trade
 * Expected: 403 Forbidden or blocked response
 */
POST /api/trading/orders
Authorization: Bearer <blocked_user_token>
Content-Type: application/json

{
  "symbol": "BTC/USD",
  "side": "BUY",
  "quantity": 0.5
}

EXPECTED RESPONSE (403 Forbidden):
{
  "error": "Account restricted",
  "message": "Your account is currently blocked. Please contact support for details."
}

VALIDATION CHECKLIST:
❓ Is trading API checking user.IsBlocked?
❓ Are all APIs (not just login) validating block status?
⚠️ CRITICAL ISSUE:
- If only login checks block, user can still trade
- Every protected endpoint must check user.Status


/**
 * TEST 2.4: Admin unblocks user
 * Expected: 200 + Status = Active
 */
POST /api/admin/users/d1234567-89ab-cdef-0123-456789abcdef/unblock
Authorization: Bearer <admin_token>
Content-Type: application/json

{
  "reason": "Review complete - violation resolved"
}

EXPECTED RESPONSE (200 OK):
{
  "id": "d1234567-89ab-cdef-0123-456789abcdef",
  "status": "Active",
  "blockReason": null,
  "blockedUntilUtc": null,
  "isBlocked": false
}

VALIDATION CHECKLIST:
✅ Status changed back to "Active"
✅ BlockReason cleared
✅ BlockedUntilUtc cleared
✅ AuditLog created


// ============================================================================
// 3. DELETE FLOW TESTS (CRITICAL)
// ============================================================================

/**
 * TEST 3.1: Admin requests user deletion (creates approval workflow)
 * Expected: 202 Accepted + AdminRequest created with Status=Pending
 */
DELETE /api/admin/users/d3234567-89ab-cdef-0123-456789abcdef
Authorization: Bearer <admin_token_1>
Content-Type: application/json

{
  "reason": "User requested account closure via support ticket #12345"
}

EXPECTED RESPONSE (202 Accepted):
{
  "id": "a5234567-89ab-cdef-0123-456789abcdef",
  "entityType": "User",
  "entityId": "d3234567-89ab-cdef-0123-456789abcdef",
  "action": "Delete",
  "reason": "User requested account closure via support ticket #12345",
  "status": "Pending",
  "requestedByAdminId": "a1111111-11ab-cdef-0111-111111111111",
  "approvedByAdminId": null,
  "createdAtUtc": "2026-04-27T10:00:00Z",
  "approvedAtUtc": null
}

Location: /api/admin/approvals/a5234567-89ab-cdef-0123-456789abcdef

VALIDATION CHECKLIST:
✅ HTTP 202 Accepted (not 200)
✅ AdminRequest created with Status=Pending
✅ User Status still Active (not deleted yet)
❓ Response includes requestId for approval tracking
✅ AuditLog created for deletion request


/**
 * TEST 3.2: Same admin tries to approve own deletion request
 * Expected: 403 Forbidden - self-approval prevented
 * 
 * EXCEPT: SuperAdmin can approve own request
 */
POST /api/admin/approvals/a5234567-89ab-cdef-0123-456789abcdef/approve
Authorization: Bearer <admin_token_1>
Content-Type: application/json

{}

EXPECTED RESPONSE (403 Forbidden):
{
  "error": "Permission denied",
  "message": "An admin cannot approve their own request"
}

VALIDATION CHECKLIST:
✅ Self-approval rejected
✅ Proper error message
❓ Does ApprovalService check requestedByAdminId == approvedByAdminId?
⚠️ SECURITY CRITICAL:
- Must prevent regular admin self-approval
- SuperAdmin exception allowed


/**
 * TEST 3.3: Different admin approves deletion request
 * Expected: 200 OK + User Status = Deleted
 */
POST /api/admin/approvals/a5234567-89ab-cdef-0123-456789abcdef/approve
Authorization: Bearer <admin_token_2>
Content-Type: application/json

{}

EXPECTED RESPONSE (200 OK):
{
  "id": "a5234567-89ab-cdef-0123-456789abcdef",
  "entityType": "User",
  "entityId": "d3234567-89ab-cdef-0123-456789abcdef",
  "action": "Delete",
  "reason": "User requested account closure via support ticket #12345",
  "status": "Approved",
  "requestedByAdminId": "a1111111-11ab-cdef-0111-111111111111",
  "approvedByAdminId": "a2222222-22ab-cdef-0222-222222222222",
  "createdAtUtc": "2026-04-27T10:00:00Z",
  "approvedAtUtc": "2026-04-27T10:05:00Z"
}

VALIDATION CHECKLIST:
✅ Status changed to "Approved"
✅ ApprovedByAdminId set to second admin
✅ ApprovedAtUtc set
✅ User Status NOW = Deleted (in database)
✅ User DeletedAtUtc set to approval timestamp
✅ AuditLog created for approval
✅ AuditLog created for delete execution
❓ Is UserApprovalHandler.ExecuteApprovedDeleteAsync called?
❓ Is user.Status set to Deleted?
❓ Is DeletedAtUtc set correctly?


/**
 * TEST 3.4: Deleted user attempts to login
 * Expected: 401 Unauthorized - generic error (user doesn't exist)
 */
POST /api/auth/login
Content-Type: application/json

{
  "username": "john_deleted",
  "password": "SecurePass123!"
}

EXPECTED RESPONSE (401 Unauthorized):
{
  "error": "Invalid credentials",
  "message": "Username or password is incorrect"
}

VALIDATION CHECKLIST:
✅ Login fails (user is deleted)
✅ Generic error message (no username enumeration)
❓ Does UserRepository.GetByUsernameAsync filter Status != Deleted?
⚠️ CRITICAL BUG RISK:
- If GetByUsernameAsync doesn't filter deleted users
- Deleted user can still login
- Must verify repository query


/**
 * TEST 3.5: Deleted user calls ANY authenticated API
 * Expected: 401 Unauthorized
 * 
 * Note: If somehow user has old token, it should still fail
 */
GET /api/user/profile
Authorization: Bearer <deleted_user_old_token>

EXPECTED RESPONSE (401 Unauthorized):
{
  "error": "Unauthorized",
  "message": "User account is not active"
}

VALIDATION CHECKLIST:
✅ All endpoints check user.IsDeleted
❓ Or do they check user.Status != Active?
⚠️ POTENTIAL ISSUE:
- If middleware only checks token validity (not status)
- Deleted user with valid token can still access APIs
- Recommend: Middleware or service layer must re-check Status


/**
 * TEST 3.6: Deleted user doesn't appear in user list
 * Expected: 200 + user list WITHOUT deleted user
 */
GET /api/admin/users
Authorization: Bearer <admin_token>

EXPECTED RESPONSE (200 OK):
{
  "total": 98,  // Was 99 before deletion
  "users": [
    { "id": "...", "username": "john_active", "status": "Active" },
    { "id": "...", "username": "jane_blocked", "status": "Blocked" },
    // john_deleted NOT in list
  ]
}

VALIDATION CHECKLIST:
❓ Does GetAllUsersAsync filter Status != Deleted?
❓ Or does repository query exclude deleted users?
⚠️ CRITICAL VISIBILITY ISSUE:
- If deleted user appears in list
- Admin might attempt operations on deleted user
- Repository MUST filter by Status


// ============================================================================
// 4. APPROVAL SECURITY TESTS
// ============================================================================

/**
 * TEST 4.1: SuperAdmin CAN approve own deletion request
 * Expected: 200 OK (self-approval allowed for SuperAdmin)
 */
POST /api/admin/approvals/a6234567-89ab-cdef-0123-456789abcdef/approve
Authorization: Bearer <superadmin_token>
Content-Type: application/json

{}

EXPECTED RESPONSE (200 OK):
{
  "status": "Approved",
  "approvedByAdminId": "<superadmin_id>"
}

VALIDATION CHECKLIST:
❓ Does ApprovalService check isSuperAdmin flag?
❓ IsUserSuperAdminAsync called before rejection?
✅ Behavior differs from regular admin


/**
 * TEST 4.2: Invalid admin token cannot approve
 * Expected: 401 Unauthorized
 */
POST /api/admin/approvals/a5234567-89ab-cdef-0123-456789abcdef/approve
Authorization: Bearer <invalid_token>
Content-Type: application/json

{}

EXPECTED RESPONSE (401 Unauthorized):
{
  "error": "Unauthorized",
  "message": "Invalid or expired token"
}

VALIDATION CHECKLIST:
✅ JWT validation in middleware


/**
 * TEST 4.3: Non-admin user cannot approve
 * Expected: 403 Forbidden
 */
POST /api/admin/approvals/a5234567-89ab-cdef-0123-456789abcdef/approve
Authorization: Bearer <regular_user_token>
Content-Type: application/json

{}

EXPECTED RESPONSE (403 Forbidden):
{
  "error": "Permission denied",
  "message": "Only admins can approve requests"
}

VALIDATION CHECKLIST:
✅ [Authorize(Roles = "Admin")] on controller


// ============================================================================
// 5. IDEMPOTENCY TESTS
// ============================================================================

/**
 * TEST 5.1: Submit deletion request twice (same admin, same user)
 * Expected: Second request either:
 *   a) Returns existing pending request (idempotent)
 *   b) Returns error "Already pending" (explicit check)
 */

REQUEST 1:
DELETE /api/admin/users/d4234567-89ab-cdef-0123-456789abcdef
Authorization: Bearer <admin_token_1>
Content-Type: application/json
{ "reason": "Account closure" }

RESPONSE 1 (202 Accepted):
{
  "id": "a7234567-89ab-cdef-0123-456789abcdef",
  "status": "Pending"
}


REQUEST 2 (identical):
DELETE /api/admin/users/d4234567-89ab-cdef-0123-456789abcdef
Authorization: Bearer <admin_token_1>
Content-Type: application/json
{ "reason": "Account closure" }

EXPECTED RESPONSE 2:
Option A (202 - return existing):
{
  "id": "a7234567-89ab-cdef-0123-456789abcdef",
  "status": "Pending",
  "message": "Deletion already requested"
}

Option B (400 - prevent duplicate):
{
  "error": "Bad request",
  "message": "Deletion request for this user is already pending"
}

Option C (409 - conflict):
{
  "error": "Conflict",
  "message": "User already has pending deletion request"
}

VALIDATION CHECKLIST:
❓ What does CreateDeleteApprovalAsync do if request exists?
❓ Does it check for existing Pending status?
⚠️ POTENTIAL ISSUE:
- If no check, multiple pending requests created
- User table could have duplicate deletion requests
- Should return existing or explicit error


/**
 * TEST 5.2: Multiple admins delete different users concurrently
 * Expected: Both requests succeed independently
 */
REQUEST 1 (Admin 1):
DELETE /api/admin/users/d5234567-89ab-cdef-0123-456789abcdef
Authorization: Bearer <admin_token_1>
{ "reason": "Reason 1" }

REQUEST 2 (Admin 2, different user):
DELETE /api/admin/users/d6234567-89ab-cdef-0123-456789abcdef
Authorization: Bearer <admin_token_2>
{ "reason": "Reason 2" }

EXPECTED: Both return 202 with separate approval IDs

VALIDATION CHECKLIST:
✅ No race conditions
✅ Each request gets unique ID


// ============================================================================
// 6. DATA VISIBILITY TESTS
// ============================================================================

/**
 * TEST 6.1: Deleted user not in user list
 * Already covered in TEST 3.6
 */


/**
 * TEST 6.2: Deleted user not in admin search/filter
 * Expected: Deleted user doesn't appear even if specifically searched
 */
GET /api/admin/users?search=john_deleted
Authorization: Bearer <admin_token>

EXPECTED RESPONSE (200 OK):
{
  "total": 0,
  "users": []
}

VALIDATION CHECKLIST:
❓ Does search endpoint filter deleted?


/**
 * TEST 6.3: Cannot retrieve deleted user by ID
 * Expected: 404 Not Found (or 410 Gone)
 */
GET /api/admin/users/d3234567-89ab-cdef-0123-456789abcdef
Authorization: Bearer <admin_token>

EXPECTED RESPONSE (404 Not Found):
{
  "error": "Not found",
  "message": "User not found"
}

OR (410 Gone - semantically accurate):
{
  "error": "Gone",
  "message": "User account has been deleted"
}

VALIDATION CHECKLIST:
❓ Does GetByIdAsync filter Status != Deleted?
⚠️ IMPORTANT:
- Admins should not see deleted user details
- Only audit log should reference them


/**
 * TEST 6.4: Deleted user's transactions/history still visible (compliance)
 * Expected: 200 + transaction history preserved
 */
GET /api/admin/audit-logs?userId=d3234567-89ab-cdef-0123-456789abcdef
Authorization: Bearer <admin_token>

EXPECTED RESPONSE (200 OK):
{
  "logs": [
    { "action": "USER_DELETE", "adminId": "...", "timestamp": "..." },
    { "action": "TRADE_EXECUTED", "details": "..." },
    // Historical data preserved for compliance
  ]
}

VALIDATION CHECKLIST:
✅ Audit logs preserved
✅ Transaction history retained
✅ Deleted user cannot be identified from data


// ============================================================================
// 7. ERROR HANDLING TESTS
// ============================================================================

/**
 * TEST 7.1: Block non-existent user
 * Expected: 404 Not Found
 */
POST /api/admin/users/99999999-99ab-cdef-0999-999999999999/block
Authorization: Bearer <admin_token>
Content-Type: application/json
{ "reason": "..." }

EXPECTED RESPONSE (404 Not Found):
{
  "error": "Not found",
  "message": "User not found"
}

VALIDATION CHECKLIST:
✅ Proper error handling


/**
 * TEST 7.2: Block with invalid duration
 * Expected: 400 Bad Request
 */
POST /api/admin/users/d1234567-89ab-cdef-0123-456789abcdef/block
Authorization: Bearer <admin_token>
Content-Type: application/json
{
  "reason": "Test",
  "durationMs": -1000,  // Invalid
  "isPermanent": false
}

EXPECTED RESPONSE (400 Bad Request):
{
  "error": "Validation error",
  "message": "Duration must be non-negative"
}

VALIDATION CHECKLIST:
✅ Input validation in controller


/**
 * TEST 7.3: Delete without reason
 * Expected: 400 Bad Request
 */
DELETE /api/admin/users/d1234567-89ab-cdef-0123-456789abcdef
Authorization: Bearer <admin_token>
Content-Type: application/json
{
  "reason": ""  // Empty
}

EXPECTED RESPONSE (400 Bad Request):
{
  "error": "Validation error",
  "message": "Reason is required"
}

VALIDATION CHECKLIST:
✅ Empty/null reason rejected


// ============================================================================
// 8. SUMMARY - VALIDATION CHECKLIST
// ============================================================================

CRITICAL ITEMS TO VERIFY IN CODEBASE:

□ USER AUTHENTICATION:
  □ UserRepository.GetByUsernameAsync filters Status != Deleted
  □ UserAuthService checks user.IsBlocked before login
  □ Login returns appropriate response for blocked user
  □ Blocked/Deleted users excluded from auth queries

□ USER STATE ON LOGIN:
  □ Login response includes isBlocked, blockReason, blockedUntilUtc
  □ Login response can indicate block status separately from auth failure

□ BLOCK ENFORCEMENT:
  □ All protected endpoints check user.IsBlocked (not just login)
  □ Trading API checks block status
  □ Middleware or service layer validates Status on each request

□ DELETE IMPLEMENTATION:
  □ User.Status = Deleted (not removed from DB)
  □ User.DeletedAtUtc = now (soft delete timestamp)
  □ UserApprovalHandler.ExecuteApprovedDeleteAsync correctly executes
  □ AuditLog created with full context

□ APPROVAL WORKFLOW:
  □ ApprovalService checks requestedByAdminId != approvedByAdminId
  □ SuperAdmin exception works (IsUserSuperAdminAsync called)
  □ AdminRequest.Status properly transitions: Pending → Approved
  □ User deleted AFTER approval (not before)

□ IDEMPOTENCY:
  □ CreateDeleteApprovalAsync checks for existing Pending request
  □ Either returns existing or returns error
  □ No duplicate requests created

□ DATA VISIBILITY:
  □ GetAllUsersAsync filters Status != Deleted
  □ User search filters deleted users
  □ GetByIdAsync returns 404 for deleted user
  □ Audit logs preserved for deleted users

□ ERROR HANDLING:
  □ 404 for non-existent user
  □ 400 for invalid input
  □ 401 for generic auth failure (deleted user)
  □ 403 for self-approval violation
  □ 202 for approval requests (not 200)

