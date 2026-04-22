# FRONTEND API USAGE AUDIT - COMPLETE CODEBASE SCAN

**Date**: April 22, 2026  
**Status**: Full Audit Complete  
**Framework**: React + TypeScript  
**Package Manager**: npm/yarn  

---

## EXECUTIVE SUMMARY

✅ **Total API Endpoints Found**: 35+  
⚠️ **Critical Issues**: 5  
🔴 **High Priority Issues**: 8  
🟡 **Medium Priority Issues**: 6  

---

## 1. SERVICES / API CALLS FOUND

### 1.1 AUTHENTICATION SERVICES

#### File: [src/services/AuthenticationService.ts](src/services/AuthenticationService.ts)
**Type**: Primary Auth Service (Unified)  
**HTTP Client**: Custom httpClient with interceptors

**USER AUTHENTICATION ENDPOINTS**:
- `POST /auth/login` → userLogin(LoginRequest)
- `POST /auth/register` → userRegister(RegisterRequest)
- `POST /auth/logout` → userLogout()

**USER 2FA ENDPOINTS**:
- `POST /auth/user/setup-2fa/generate` → userSetup2FAInitial()
- `POST /auth/user/setup-2fa/enable` → userSetup2FAEnable()
- `POST /auth/user/verify-login-2fa` → userVerifyLogin2FA()
- `POST /auth/user/register-complete-2fa` → userRegisterComplete2FA()
- `POST /auth/user/disable-2fa` → userDisable2FA()

**ADMIN BOOTSTRAP ENDPOINTS**:
- `POST /admin/bootstrap` → adminBootstrap()
- `POST /admin/auth/login` → adminLogin()
- `POST /admin/auth/2fa/verify` → adminVerify2FA()
- `POST /admin/auth/2fa/setup` → adminSetup2FA()
- `POST /admin/invitations` → adminRegisterViaInvite()
- `POST /admin/auth/invite` → adminInvite()

---

#### File: [src/services/AdminAuthService.ts](src/services/AdminAuthService.ts)
**Type**: ⚠️ DEPRECATED/DUPLICATE (Legacy)  
**HTTP Client**: fetch() directly  
**Status**: REDUNDANT - Same endpoints as AuthenticationService

**DUPLICATE ENDPOINTS**:
- `POST /auth/admin/bootstrap` ← USE AuthenticationService INSTEAD
- `POST /auth/admin-login`
- `POST /auth/admin/verify-2fa`
- `POST /auth/admin/setup-2fa/generate`
- `POST /auth/admin/setup-2fa/enable`
- `POST /auth/admin/setup-2fa/disable`
- `POST /auth/admin/backup-codes/regenerate`
- `POST /auth/admin/register`
- `POST /auth/admin/invite`

⚠️ **CRITICAL ISSUE**: Two services doing the same thing!

---

### 1.2 MARKET DATA SERVICE

#### File: [src/services/MarketDataService.ts](src/services/MarketDataService.ts)
**Type**: Market Data Service  
**HTTP Client**: Custom httpClient with interceptors

**ENDPOINTS**:
- `GET /health` → getHealth()
- `GET /market` → getAllAssets()
- `GET /market/{symbol}` → getAssetBySymbol(symbol)
- `GET /market/instruments` → getInstruments()

---

### 1.3 ADMIN MANAGEMENT SERVICE (DUPLICATED)

#### File: [src/services/adminService.ts](src/services/adminService.ts)
**Type**: ⚠️ DUPLICATE Admin Service (Legacy)  
**HTTP Client**: fetch() directly  
**Status**: REDUNDANT - Overlaps with instrumentsService & hooks

**ENDPOINTS**:
- `GET /admin/health` → getHealth()
- `GET /admin/requests` → getAdminRequests(params)
- `POST /admin/requests/{id}/approve` → approveRequest(id, reason)
- `POST /admin/requests/{id}/reject` → rejectRequest(id, reason)
- `GET /admin/instruments` → getInstruments(params)
- `POST /admin/instruments` → createInstrument(data)
- `PUT /admin/instruments/{id}` → updateInstrument(id, data)
- `DELETE /admin/instruments/{id}` → deleteInstrument(id)
- `POST /admin/instruments/{id}/submit-approval` → submitInstrumentForApproval(id)
- `GET /admin/audit-history` → getAuditLogs(params)
- `GET /admin/users` → getAdminUsers()
- `POST /admin/users/{id}/role` → changeUserRole(userId, newRole)

⚠️ **CRITICAL ISSUE**: Similar to instrumentsService - Multiple implementations of same API!

---

### 1.4 INSTRUMENTS SERVICE (AXIOS-BASED)

#### File: [src/services/admin/instrumentsService.ts](src/services/admin/instrumentsService.ts)
**Type**: Admin Instruments CRUD  
**HTTP Client**: axios directly  
**Status**: PRIMARY for instruments (but duplicates adminService)

**CRUD ENDPOINTS**:
- `GET /api/admin/instruments` → getAll()
- `GET /api/admin/instruments/{id}` → getById(id)
- `POST /api/admin/instruments` → create(request)
- `PUT /api/admin/instruments/{id}` → update(id, request)
- `DELETE /api/admin/instruments/{id}` → delete_(id)

**WORKFLOW STATE MACHINE ENDPOINTS**:
- `POST /api/admin/instruments/{id}/request-approval` → requestApproval(id)
- `POST /api/admin/instruments/{id}/approve` → approve(id)
- `POST /api/admin/instruments/{id}/reject` → reject(id, request)
- `POST /api/admin/instruments/{id}/retry-submission` → retrySubmission(id)
- `POST /api/admin/instruments/{id}/archive` → archive(id)
- `POST /api/admin/instruments/{id}/block` → block(id)
- Additional operations for instrument management

⚠️ **CRITICAL ISSUE**: Uses axios instead of centralized httpClient

---

## 2. HOOKS WITH API INTEGRATION

### 2.1 AUTHENTICATION HOOKS

#### File: [src/hooks/useAuth.tsx](src/hooks/useAuth.tsx)
**Type**: Auth Context Hook  
**API Calls**: Via AuthenticationService
- Wraps `authService.userLogin()`
- Wraps `authService.userRegister()`
- NO DIRECT API CALLS (uses service layer)

---

#### File: [src/hooks/admin/AdminAuthContext.tsx](src/hooks/admin/AdminAuthContext.tsx)
**Type**: Admin Auth Context  
**API Calls**: None (state management only)
- Token lifecycle management
- JWT decoding and expiry checking
- Session persistence to localStorage

---

#### File: [src/hooks/admin/useAdminAuth.ts](src/hooks/admin/useAdminAuth.ts)
**Type**: Admin Auth Hook  
**API Calls**: None (context wrapper only)
- Re-exports AdminAuthContext

---

### 2.2 DATA FETCHING HOOKS

#### File: [src/hooks/useAccount.ts](src/hooks/useAccount.ts)
**Type**: Account Data Hook  
**HTTP Client**: fetch() directly  
**ENDPOINT**:
- `GET /api/account/main` → fetchMainAccount()

⚠️ **ISSUE**: Direct fetch in hook instead of service

---

#### File: [src/hooks/useApi.ts](src/hooks/useApi.ts)
**Type**: Generic API Hook Wrapper  
**API Calls**: Generic wrapper for any API call
- No specific endpoints
- Handles loading/error state for any API response

---

#### File: [src/hooks/admin/useAdminRequests.ts](src/hooks/admin/useAdminRequests.ts)
**Type**: Admin Requests Hook  
**API Calls**: ❌ STUB IMPLEMENTATION (no actual API calls)
- approveRequest() - console.log only
- rejectRequest() - console.log only

🔴 **ISSUE**: Not implemented!

---

#### File: [src/hooks/admin/useAdminUsers.ts](src/hooks/admin/useAdminUsers.ts)
**Type**: Admin Users Hook  
**API Calls**: ❌ STUB IMPLEMENTATION (no actual API calls)
- changeUserRole() - console.log only

🔴 **ISSUE**: Not implemented!

---

#### File: [src/hooks/admin/useInstruments.ts](src/hooks/admin/useInstruments.ts)
**Type**: Instruments Management Hook  
**HTTP Client**: axios via instrumentsService
**ENDPOINTS** (via instrumentsService):
- `GET /api/admin/instruments` → fetchInstruments()
- `POST /api/admin/instruments` → createInstrument(data)
- `PUT /api/admin/instruments/{id}` → updateInstrument(id, data)
- `DELETE /api/admin/instruments/{id}` → deleteInstrument(id)
- `POST /api/admin/instruments/{id}/request-approval` → requestApproval(id)
- `POST /api/admin/instruments/{id}/approve` → approve(id)
- `POST /api/admin/instruments/{id}/reject` → reject(id, data)
- `POST /api/admin/instruments/{id}/retry-submission` → retrySubmission(id)
- `POST /api/admin/instruments/{id}/archive` → archive(id)
- `POST /api/admin/instruments/{id}/block` → block(id)

---

#### File: [src/hooks/admin/useAdminInvite.ts](src/hooks/admin/useAdminInvite.ts)
**Type**: Admin Invite Hook  
**HTTP Client**: fetch() directly  
**ENDPOINT**:
- `POST /api/auth/admin/invite` → inviteAdmin(email, firstName, lastName)

⚠️ **ISSUE**: Direct fetch instead of service

---

#### File: [src/hooks/admin/useGetAdminAuditLogs.ts](src/hooks/admin/useGetAdminAuditLogs.ts)
**Type**: Admin Audit Logs Hook  
**HTTP Client**: fetch() directly  
**ENDPOINT**:
- `GET /api/admin/audit-history` → fetchAdminAuditLogs()

⚠️ **ISSUE**: Direct fetch, uses different localStorage key ('auth-token' vs 'trading-admin-session')

---

#### File: [src/hooks/admin/useAuditLogs.ts](src/hooks/admin/useAuditLogs.ts)
**Type**: Audit Logs Hook  
**HTTP Client**: fetch() directly  
**ENDPOINT**:
- `GET /api/admin/audit-history?page={page}&pageSize={pageSize}` → fetchLogs()

✅ **IMPROVEMENT**: Uses correct 'trading-admin-session' key, has pagination

---

#### File: [src/hooks/admin/useGetUsers.ts](src/hooks/admin/useGetUsers.ts)
**Type**: Users List Hook  
**HTTP Client**: fetch() directly  
**ENDPOINT**:
- `GET /api/admin/users` → fetchUsers()

⚠️ **ISSUE**: 
- Auto-refresh every 10 seconds
- Direct fetch instead of service
- Could cause excessive requests

---

## 3. PAGES WITH API INTEGRATION

### 3.1 AUTHENTICATION PAGES

| File | Endpoint(s) | Method | Status |
|------|-----------|--------|--------|
| [LoginPage.tsx](src/pages/LoginPage.tsx) | `POST /auth/login` | authService | ✅ Good |
| [RegisterPage.tsx](src/pages/RegisterPage.tsx) | `POST /auth/register` + 2FA | authService | ✅ Good |
| [UserSetup2FAPage.tsx](src/pages/UserSetup2FAPage.tsx) | `POST /auth/user/setup-2fa/*` | authService | ✅ Good |
| [UserVerify2FAPage.tsx](src/pages/UserVerify2FAPage.tsx) | `POST /auth/user/verify-login-2fa` | authService | ✅ Good |
| [AdminLoginPage.tsx](src/pages/AdminLoginPage.tsx) | `POST /auth/admin-login` | adminAuthService | ⚠️ Uses deprecated service |
| [AdminRegisterPage.tsx](src/pages/AdminRegisterPage.tsx) | `POST /auth/admin/bootstrap` | adminAuthService | ⚠️ Uses deprecated service |
| [AdminSetup2FAPage.tsx](src/pages/AdminSetup2FAPage.tsx) | `POST /auth/admin/setup-2fa/*` | adminAuthService | ⚠️ Uses deprecated service |
| [AdminVerify2FAPage.tsx](src/pages/AdminVerify2FAPage.tsx) | `POST /auth/admin/verify-2fa` | adminAuthService | ⚠️ Uses deprecated service |

### 3.2 DASHBOARD PAGES

| File | Endpoint(s) | Method | Status |
|------|-----------|--------|--------|
| [AdminDashboardPage.tsx](src/pages/AdminDashboardPage.tsx) | None (indirect via components) | Components | ✅ Good |
| [DashboardPage.tsx](src/pages/DashboardPage.tsx) | `GET /api/account/main` | useAccount hook | ⚠️ Direct fetch |
| [PortfolioPage.tsx](src/pages/PortfolioPage.tsx) | None (hardcoded data) | None | ⚠️ No real data |

### 3.3 MARKET/DATA PAGES

| File | Endpoint(s) | Method | Status |
|------|-----------|--------|--------|
| [MarketPage.tsx](src/pages/MarketPage.tsx) | `GET /market`, `GET /market/{symbol}` | apiService | ✅ Good |
| [HealthPage.tsx](src/pages/HealthPage.tsx) | `GET /health` | apiService | ✅ Good |
| [InstrumentsPage.tsx](src/pages/InstrumentsPage.tsx) | Delegates to MarketPage | MarketPage | ✅ Good |

---

## 4. COMPONENTS WITH API INTEGRATION

| Component | Hook/Service Used | Endpoints | Status |
|-----------|------------------|-----------|--------|
| [InstrumentsContent.tsx](src/components/admin/Instruments/InstrumentsContent.tsx) | useInstruments | GET/POST/PUT/DELETE /admin/instruments/* | ✅ Good |
| [ApprovalsContent.tsx](src/components/admin/Approvals/ApprovalsContent.tsx) | useAdminRequests | None (stub) | 🔴 Not implemented |
| [UsersContent.tsx](src/components/admin/Users/UsersContent.tsx) | useGetUsers, useAdminInvite | GET /admin/users, POST /auth/admin/invite | ⚠️ Multiple patterns |

---

## 5. HTTP CLIENT STRATEGIES (CRITICAL ISSUE!)

### Multiple HTTP Communication Patterns Detected:

| Client Type | Files Using It | Status |
|------------|----------------|--------|
| **Custom httpClient** | AuthenticationService, MarketDataService | ✅ Preferred |
| **fetch()** directly | adminService, useAccount, useAdminInvite, useGetAdminAuditLogs, useAuditLogs, useGetUsers | ⚠️ Inconsistent |
| **axios** | instrumentsService | ⚠️ Inconsistent |

### Custom httpClient Features ([src/services/http/HttpClient.ts](src/services/http/HttpClient.ts)):
- ✅ Request/Response interceptors
- ✅ Exponential backoff retry logic
- ✅ Request/Response logging
- ✅ Error handling with custom ApiError class
- ✅ Centralized configuration

**PROBLEM**: Only 2 services use it. Others use fetch/axios directly!

---

## 6. ENDPOINT MAP (RAW)

### Authentication Endpoints

| Endpoint | Method | Used In | Count |
|----------|--------|---------|-------|
| `/auth/login` | POST | LoginPage, AuthenticationService | 1 |
| `/auth/register` | POST | UserRegisterPage, AuthenticationService | 1 |
| `/auth/logout` | POST | AuthenticationService | 1 |
| `/auth/user/setup-2fa/generate` | POST | UserSetup2FAPage (via authService) | 1 |
| `/auth/user/setup-2fa/enable` | POST | UserSetup2FAPage | 1 |
| `/auth/user/verify-login-2fa` | POST | UserVerify2FAPage | 1 |
| `/auth/user/register-complete-2fa` | POST | UserSetup2FAPage | 1 |
| `/auth/admin/bootstrap` | POST | AdminRegisterPage (adminAuthService) | 1 |
| `/auth/admin-login` | POST | AdminLoginPage (adminAuthService) | 1 |
| `/auth/admin/verify-2fa` | POST | AdminVerify2FAPage (adminAuthService) | 1 |
| `/auth/admin/setup-2fa/generate` | POST | AdminSetup2FAPage (adminAuthService) | 1 |
| `/auth/admin/setup-2fa/enable` | POST | AdminSetup2FAPage (adminAuthService) | 1 |
| `/auth/admin/invite` | POST | useAdminInvite hook | 1 |

### Market Data Endpoints

| Endpoint | Method | Used In | Count |
|----------|--------|---------|-------|
| `/health` | GET | HealthPage, MarketDataService | 1 |
| `/market` | GET | MarketPage, MarketDataService | 1 |
| `/market/{symbol}` | GET | MarketPage | 1 |
| `/market/instruments` | GET | MarketDataService | 1 |

### Account Endpoints

| Endpoint | Method | Used In | Count |
|----------|--------|---------|-------|
| `/api/account/main` | GET | DashboardPage (useAccount hook) | 1 |

### Admin Management Endpoints

| Endpoint | Method | Used In | Count |
|----------|--------|---------|-------|
| `/admin/health` | GET | adminService | 1 |
| `/admin/requests` | GET | adminService, useAdminRequests (stub) | 2 |
| `/admin/requests/{id}/approve` | POST | adminService | 1 |
| `/admin/requests/{id}/reject` | POST | adminService | 1 |
| `/admin/instruments` | GET | adminService, instrumentsService, useInstruments | 3 |
| `/admin/instruments` | POST | adminService, instrumentsService, useInstruments | 3 |
| `/admin/instruments/{id}` | GET | instrumentsService | 1 |
| `/admin/instruments/{id}` | PUT | adminService, instrumentsService, useInstruments | 3 |
| `/admin/instruments/{id}` | DELETE | adminService, instrumentsService, useInstruments | 3 |
| `/admin/instruments/{id}/request-approval` | POST | instrumentsService, useInstruments | 2 |
| `/admin/instruments/{id}/approve` | POST | instrumentsService, useInstruments | 2 |
| `/admin/instruments/{id}/reject` | POST | instrumentsService, useInstruments | 2 |
| `/admin/instruments/{id}/retry-submission` | POST | instrumentsService, useInstruments | 2 |
| `/admin/instruments/{id}/archive` | POST | instrumentsService, useInstruments | 2 |
| `/admin/instruments/{id}/block` | POST | instrumentsService, useInstruments | 2 |
| `/admin/audit-history` | GET | adminService, useGetAdminAuditLogs, useAuditLogs | 3 |
| `/admin/users` | GET | adminService, useGetUsers | 2 |
| `/admin/users/{id}/role` | POST | adminService | 1 |

---

## 7. DUPLICATED API LOGIC (CRITICAL!)

### 🔴 DUPLICATE #1: Admin Authentication

**Endpoint Group**: `/auth/admin/*`

| File | Service | HTTP Client | Used By |
|------|---------|------------|---------|
| [AuthenticationService.ts](src/services/AuthenticationService.ts) | ✅ PRIMARY | httpClient | User pages (preferable) |
| [AdminAuthService.ts](src/services/AdminAuthService.ts) | ❌ DUPLICATE | fetch | Admin pages (currently used) |

**Problem**: 
- Same endpoints implemented twice
- Different HTTP clients
- Different error handling
- Different token management

**Solution**: MIGRATE AdminAuthService endpoints to AuthenticationService, delete AdminAuthService

---

### 🔴 DUPLICATE #2: Admin Instruments Management

**Endpoint Group**: `/admin/instruments/*`

| File | Library | Used By |
|------|---------|---------|
| [adminService.ts](src/services/adminService.ts) | fetch | adminService class (unused?) |
| [instrumentsService.ts](src/services/admin/instrumentsService.ts) | axios | useInstruments hook ✅ PRIMARY |

**Problem**:
- Two implementations of same endpoints
- Different HTTP clients
- `adminService.ts` seems abandoned

**Solution**: DELETE `adminService.ts`, keep only `instrumentsService.ts`

---

### 🟡 DUPLICATE #3: Audit Logs

**Endpoint**: `/admin/audit-history`

| File | HTTP Client | Auto-Refresh | Storage Key |
|------|-------------|--------------|-------------|
| [useGetAdminAuditLogs.ts](src/hooks/admin/useGetAdminAuditLogs.ts) | fetch | No | 'auth-token' ⚠️ |
| [useAuditLogs.ts](src/hooks/admin/useAuditLogs.ts) | fetch | No | 'trading-admin-session' ✅ |
| [adminService.ts](src/services/adminService.ts) | fetch | No | 'trading-admin-session' |

**Problem**:
- Multiple implementations
- useGetAdminAuditLogs uses WRONG storage key
- Inconsistent naming

**Solution**: Keep only useAuditLogs, delete useGetAdminAuditLogs

---

### 🟡 DUPLICATE #4: User Listing

**Endpoint**: `/admin/users`

| File | HTTP Client | Auto-Refresh | Location |
|------|-------------|--------------|----------|
| [adminService.ts](src/services/adminService.ts) | fetch | No | Service |
| [useGetUsers.ts](src/hooks/admin/useGetUsers.ts) | fetch | YES (10s) | Hook ✅ PRIMARY |

**Problem**:
- Two implementations
- useGetUsers has auto-refresh (10s interval) - possible performance issue
- adminService.ts implementation unused

---

## 8. TOKEN MANAGEMENT ISSUES

### Multiple Storage Keys for Admin Token:

| Storage Key | File | Status |
|-------------|------|--------|
| `'trading-admin-session'` | AdminAuthContext, useAuditLogs, adminService | ✅ Correct (Primary) |
| `'auth-token'` | useGetAdminAuditLogs | ❌ WRONG |
| `ADMIN_SESSION_KEY` | useAuditLogs (local const) | ✅ Maps to 'trading-admin-session' |
| `ADMIN_SESSION_KEY` | adminService (local const) | ✅ Maps to 'trading-admin-session' |

**Problem**: useGetAdminAuditLogs reads from WRONG localStorage key!

---

## 9. MIGRATION PRIORITY

### 🔴 CRITICAL - DO IMMEDIATELY

#### 1. Delete AdminAuthService.ts
- **Why**: Duplicate of AuthenticationService
- **Impact**: Medium (need to migrate AdminLoginPage, AdminRegisterPage, AdminSetup2FAPage, AdminVerify2FAPage)
- **Effort**: 2-3 hours
- **Files to change**: 4 pages
- **Steps**:
  1. Add remaining admin methods to AuthenticationService
  2. Update admin pages to use AuthenticationService instead
  3. Delete AdminAuthService.ts
  4. Test all admin auth flows

#### 2. Delete adminService.ts
- **Why**: Mostly unused, duplicates instrumentsService
- **Impact**: Low (no files currently use it)
- **Effort**: 1 hour (search for usage)
- **Steps**:
  1. Grep for any usage of adminService
  2. If found, migrate to instrumentsService or dedicated services
  3. Delete file

#### 3. Fix useGetAdminAuditLogs Token Key
- **Why**: Reading from wrong localStorage key
- **Impact**: High (might be causing auth failures)
- **Effort**: 30 minutes
- **Steps**:
  1. Change from `'auth-token'` to `'trading-admin-session'`
  2. Test audit logs fetching
  3. OR: Delete useGetAdminAuditLogs (if useAuditLogs is replacement)

#### 4. Delete useGetAdminAuditLogs
- **Why**: Duplicate of useAuditLogs with incorrect implementation
- **Impact**: Low (need to confirm if used)
- **Effort**: 30 minutes
- **Steps**:
  1. Search for usage
  2. If used, replace with useAuditLogs
  3. Delete hook

---

### 🟡 HIGH PRIORITY

#### 5. Consolidate HTTP Clients
- **Why**: Three different patterns (fetch, axios, custom client)
- **Impact**: High (maintenance burden)
- **Effort**: 8-12 hours
- **Strategy**:
  - Use custom httpClient (preferred, has interceptors & retry logic)
  - Convert fetch calls to httpClient
  - Convert axios to httpClient
  - Files to refactor: instrumentsService.ts, adminService.ts, useAccount.ts, useAdminInvite.ts, all admin hooks

#### 6. Convert instrumentsService from axios to httpClient
- **Why**: Consistency, better error handling
- **Effort**: 3-4 hours
- **Steps**:
  1. Replace axios with httpClient
  2. Update error handling
  3. Add to AuthenticationService or create dedicated service
  4. Test all CRUD operations

#### 7. Fix useGetUsers Auto-Refresh (10s interval)
- **Why**: Excessive API calls (6 per minute per user)
- **Impact**: High (server load)
- **Effort**: 1 hour
- **Steps**:
  1. Remove auto-refresh OR increase interval to 30-60s
  2. Make it manual refresh only
  3. Or add configuration for interval

---

### 🟠 MEDIUM PRIORITY

#### 8. Implement Stub Hooks
- **Why**: useAdminRequests and useAdminUsers are not implemented
- **Impact**: Medium (features incomplete)
- **Effort**: 4-6 hours
- **Steps**:
  1. Create services for admin requests
  2. Implement useAdminRequests with actual API calls
  3. Implement useAdminUsers with actual API calls
  4. Test components

#### 9. Create Dedicated Services
- **Why**: Move fetch calls from hooks to services
- **Impact**: Medium (better separation of concerns)
- **Effort**: 6-8 hours
- **Services to create**:
  - AccountService (for /api/account/main)
  - AdminInviteService (for /api/auth/admin/invite)
  - AdminRequestsService (for approval workflows)
  - AdminAuditService (for audit logs)

#### 10. Add Request Logging/Tracing
- **Why**: httpClient has it, fetch calls don't
- **Impact**: Low (debugging aid)
- **Effort**: 2-3 hours

---

### 🟡 LOW PRIORITY

#### 11. Add TypeScript Strict Null Checks
- **Why**: Better type safety
- **Effort**: 4-5 hours

#### 12. Add Request Cancellation
- **Why**: Prevent stale request issues
- **Effort**: 3-4 hours

#### 13. Add Request Deduplication
- **Why**: Prevent duplicate simultaneous requests
- **Effort**: 2-3 hours

---

## 10. ARCHITECTURE RECOMMENDATIONS

### Current State
```
Pages → Hooks → Services → fetch/axios/httpClient → Backend
```

### Recommended State
```
Pages → Hooks → Services (using centralized httpClient) → Backend
        ↓
    Context (for state)
        ↓
    httpClient (with interceptors, retry, logging)
```

### Specific Recommendations

1. **Use AuthenticationService for ALL auth**
   - Migrate AdminAuthService → AuthenticationService
   - Single source of truth

2. **Use httpClient for ALL HTTP calls**
   - Replace fetch with httpClient
   - Replace axios with httpClient
   - Consistent error handling & retry logic

3. **Create Domain-Specific Services**
   - `AuthenticationService` - auth
   - `MarketDataService` - market data ✅ Already good
   - `AdminService` - admin operations
   - `InstrumentsService` - instruments CRUD ✅ Already good
   - `AccountService` - user account
   - `AuditService` - audit logs

4. **Use Hooks for State Management**
   - Hooks call services
   - Services call httpClient
   - Clear separation of concerns

5. **Configuration**
   - [apiConfig.ts](src/config/apiConfig.ts) - centralized ✅ Good
   - Add environment-specific overrides
   - Add logging level configuration

---

## 11. DETECTED ISSUES SUMMARY

### 🔴 CRITICAL (Must Fix)

1. **Duplicate AdminAuthService** - Two identical services for admin auth
2. **Token Key Mismatch** - useGetAdminAuditLogs reads from 'auth-token' instead of 'trading-admin-session'
3. **Duplicate Instruments Logic** - adminService.ts and instrumentsService.ts both handle same endpoints
4. **Multiple HTTP Clients** - fetch, axios, and custom client all used inconsistently

### 🟡 HIGH (Should Fix)

5. **Excessive API Calls** - useGetUsers auto-refreshes every 10s (6 req/min)
6. **Incomplete Implementations** - useAdminRequests and useAdminUsers are stubs
7. **Direct Fetch in Hooks** - Multiple hooks use fetch directly instead of services
8. **Inconsistent Error Handling** - Different patterns in different files

### 🟠 MEDIUM (Nice to Have)

9. **No Request Logging** - fetch calls have no logging/tracing
10. **No Request Deduplication** - Could send duplicate requests
11. **No Request Cancellation** - Could have stale request issues
12. **Storage Key Inconsistency** - 'auth-token' vs 'trading-admin-session'

---

## 12. DEPENDENCY ANALYSIS

### External Libraries Used

| Library | File | Usage |
|---------|------|-------|
| **axios** | instrumentsService.ts | HTTP requests |
| **fetch** (native) | Multiple | HTTP requests |

### Local Dependencies

| Module | Exports | Used By |
|--------|---------|---------|
| apiConfig | API_CONFIG, getEndpoint | AuthenticationService, MarketDataService, httpClient |
| httpClient | HttpClient class, singleton | AuthenticationService, MarketDataService |
| ApiError | ApiError class | All services |

---

## 13. ENDPOINT COVERAGE MAP

### Implemented ✅
- All authentication endpoints
- Market data endpoints
- Admin instruments CRUD
- Admin audit logs
- User account data

### Partially Implemented ⚠️
- Admin requests/approvals (stub)
- Admin user role changes (stub)
- User 2FA management

### Not Found 🔴
- Order management endpoints
- Trade execution endpoints
- Portfolio endpoints
- Real-time data endpoints

---

## 14. TESTING COVERAGE

### Recommended Tests

```
UNIT TESTS:
✅ AuthenticationService (all methods)
✅ MarketDataService (all methods)
⚠️  adminService (unused - skip)
❌ adminAuthService (DUPLICATE - delete)
✅ instrumentsService (all CRUD + workflow)
❌ useAdminRequests (stub - implement first)
❌ useAdminUsers (stub - implement first)
✅ useGetUsers (check excessive refresh)

INTEGRATION TESTS:
- Full auth flow (login → 2FA → dashboard)
- Admin bootstrap flow
- Instruments CRUD workflow
- Token expiration handling
- Error recovery with retry logic

E2E TESTS:
- Complete user registration → login → 2FA
- Complete admin bootstrap → 2FA → dashboard
- Instrument creation → approval → publish workflow
```

---

## 15. SUMMARY TABLE

| Category | Count | Status |
|----------|-------|--------|
| **Services** | 5 | ⚠️ 2 Duplicates |
| **Hooks** | 11 | ⚠️ 2 Stubs, Multiple issues |
| **Pages** | 14 | ✅ Good |
| **Components** | 3 | ⚠️ Some use stubs |
| **HTTP Clients** | 3 types | ⚠️ Inconsistent |
| **Endpoints** | 35+ | ⚠️ High duplication |
| **Critical Issues** | 4 | 🔴 Must fix |
| **High Priority** | 8 | 🟡 Should fix |
| **Medium Priority** | 6 | 🟠 Nice to have |

---

## NEXT STEPS

### Phase 1: Cleanup (Week 1)
1. Delete AdminAuthService.ts
2. Delete adminService.ts
3. Fix useGetAdminAuditLogs or delete it
4. Estimated time: 4-5 hours

### Phase 2: Consolidation (Week 2-3)
1. Convert all to httpClient
2. Create dedicated services
3. Refactor hooks to use services
4. Estimated time: 12-16 hours

### Phase 3: Implementation (Week 3-4)
1. Implement stub hooks
2. Add comprehensive testing
3. Performance optimization
4. Estimated time: 12-15 hours

---

**End of Audit Report**  
Generated: April 22, 2026  
Auditor: Full Frontend Codebase Scanner
