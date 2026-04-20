# 🎯 IMPLEMENTATION TODO LIST - Trading Platform Admin Auth

## ✅已完成 (COMPLETED)
- [x] IJwtTokenGenerator interface extended (3 token types)
- [x] JwtTokenGenerator implementation (5min temp + 60min final)
- [x] AdminAuthService - core logic
- [x] AdminAuthRepository - database operations
- [x] EncryptionService - AES-256-GCM
- [x] TwoFactorService - TOTP + backup codes
- [x] All Response DTOs (AdminAuthResponses.cs)
- [x] All Request Models (AdminAuthRequests.cs)
- [x] Remove dangerous logging from AdminAuthController
- [x] Fix parameter names (invocationContext → context)

---

## ⚠️ MUST FIX IMMEDIATELY

### 1️⃣ Move AdminHealthCheckResponse to DTOs
**File:** `TradingPlatform.Api/Controllers/AdminAuthController.cs`
**Lines:** 768-774
**Action:** 
- Copy `AdminHealthCheckResponse` record to `TradingPlatform.Core/Dtos/AdminAuthResponses.cs`
- Delete from controller
- Add `using TradingPlatform.Core.Dtos;` if not present

### 2️⃣ Create TwoFactorSettings Model
**File:** `TradingPlatform.Core/Models/TwoFactorSettings.cs` (NEW)
**Content:**
```csharp
namespace TradingPlatform.Core.Models;

public sealed record TwoFactorSettings(
    string Issuer = "TradingPlatform",
    int QrCodeSize = 10,
    int TimeWindowSeconds = 30,
    int BackupCodeCount = 8
);
```

### 3️⃣ Create EncryptionSettings Model
**File:** `TradingPlatform.Core/Models/EncryptionSettings.cs` (NEW)
**Content:**
```csharp
namespace TradingPlatform.Core.Models;

public sealed record EncryptionSettings(
    string MasterKey
);
```

### 4️⃣ Add Configuration to appsettings.json
**File:** `TradingPlatform.Api/appsettings.json`
**Action:** Add after "Jwt" section:
```json
"TwoFactor": {
  "Issuer": "TradingPlatform",
  "QrCodeSize": 10
},
"Encryption": {
  "MasterKey": "ProductionMasterKeyMinimum32CharactersLongMustBeVerySecure!!!"
}
```

### 5️⃣ Add Configuration to appsettings.Development.json
**File:** `TradingPlatform.Api/appsettings.Development.json`
**Action:** Add after "Jwt" section:
```json
"TwoFactor": {
  "Issuer": "TradingPlatform",
  "QrCodeSize": 10
},
"Encryption": {
  "MasterKey": "DevelopmentMasterKeyForLocalTesting32CharactersLongMinimumRequired!!!"
}
```

### 6️⃣ Register Services in DI Container
**File:** `TradingPlatform.Data/Extensions/ServiceCollectionExtensions.cs`
**Location:** In `AddDataServices()` method, ADD:
```csharp
// Configure settings
services.Configure<TwoFactorSettings>(configuration.GetSection("TwoFactor"));
services.Configure<EncryptionSettings>(configuration.GetSection("Encryption"));

// Register admin auth services
services.AddScoped<ITwoFactorService, TwoFactorService>();
services.AddScoped<IEncryptionService, EncryptionService>();
services.AddScoped<IAdminAuthRepository, AdminAuthRepository>();
services.AddScoped<AdminAuthService>();
services.AddScoped<AdminInvitationService>();
services.AddScoped<IAdminService, AdminService>();
services.AddScoped<IAdminRequestRepository, SqlAdminRequestRepository>();
services.AddScoped<IAuditLogRepository, SqlAuditLogRepository>();
```

### 7️⃣ Verify AdminBootstrapRequest exists
**File:** `TradingPlatform.Core/Models/AdminBootstrapRequest.cs`
**Status:** ✅ ALREADY EXISTS

---

## 📋 SECONDARY - CREATE MISSING REQUEST MODELS

### 8️⃣ Create Missing AdminService DTOs
**Files to verify/create in `TradingPlatform.Core/Dtos/AdminDto.cs`:**
- AdminDetailDto (admin profile + 2FA status)
- AdminListDto (list of admins)
- AdminPermissionsDto (admin permissions/roles)

---

## 🔧 TERTIARY - BACKEND CONTROLLERS

### 9️⃣ Implement AdminController
**File:** `TradingPlatform.Api/Controllers/AdminController.cs` (NEW)
**Endpoints:**
- GET /api/admin/me - current admin profile
- GET /api/admin/list - list all admins (super admin only)
- PUT /api/admin/{id} - update admin
- DELETE /api/admin/{id} - delete admin (super admin only)

### 🔟 Implement InstrumentsController  
**File:** `TradingPlatform.Api/Controllers/InstrumentsController.cs` (NEW)
**Endpoints:**
- GET /api/instruments - list all
- POST /api/instruments - create
- PUT /api/instruments/{id} - update
- DELETE /api/instruments/{id} - delete (admin only)
- POST /api/instruments/{id}/block - block instrument
- POST /api/instruments/{id}/unblock - unblock instrument

### 1️⃣1️⃣ Implement AdminRequestsController
**File:** `TradingPlatform.Api/Controllers/AdminRequestsController.cs` (NEW)
**Endpoints:**
- GET /api/admin/requests - list pending requests
- POST /api/admin/requests/{id}/approve - approve request
- POST /api/admin/requests/{id}/reject - reject request

---

## 🎨 QUATERNARY - FRONTEND COMPONENTS

### 1️⃣2️⃣ Implement Admin Authentication Pages
- [x] AdminLoginPage.tsx - stub created
- [x] AdminRegisterPage.tsx - stub created  
- [x] AdminSetup2FAPage.tsx - stub created
- [x] AdminVerify2FAPage.tsx - stub created
- [ ] Implement login form logic
- [ ] Implement 2FA setup UI (QR code display)
- [ ] Implement 2FA verification form
- [ ] Implement registration flow

### 1️⃣3️⃣ Implement Admin Components
- [x] AdminHeader.tsx - stub created
- [x] AdminNavbar.tsx - stub created
- [x] AdminSidebar.tsx - stub created
- [ ] Connect navigation logic
- [ ] Add active route highlighting

### 1️⃣4️⃣ Implement Admin Dashboard
- [x] AdminDashboardPage.tsx - stub created
- [x] Section components (DashboardContent, etc) - stubs created
- [ ] Fetch and display admin statistics
- [ ] Implement data tables for requests/logs

### 1️⃣5️⃣ Implement useAdminAuth Hook
- [x] useAdminAuth.ts - stub created
- [ ] Implement login API call
- [ ] Implement 2FA verification API call
- [ ] Implement token storage in localStorage
- [ ] Implement token refresh logic
- [ ] Implement logout

---

## 🗄️ QUINARY - DATABASE & REPOSITORIES

### 1️⃣6️⃣ Verify/Complete Repository Implementations
- [ ] SqlAdminRequestRepository.cs - verify complete
- [ ] SqlAuditLogRepository.cs - verify complete
- [ ] SqlInstrumentRepository.cs - verify complete

### 1️⃣7️⃣ Create/Update EF Core Models
- [ ] AdminRequest entity
- [ ] AuditLog entity
- [ ] Admin invitation entity (if separate)
- [ ] Add migrations for admin tables

---

## ✔️ VERIFICATION CHECKLIST

Before running `dotnet build`:
- [ ] AdminHealthCheckResponse moved to DTOs
- [ ] TwoFactorSettings.cs created
- [ ] EncryptionSettings.cs created
- [ ] appsettings.json updated (TwoFactor + Encryption)
- [ ] appsettings.Development.json updated
- [ ] ServiceCollectionExtensions.cs DI registrations added
- [ ] IAdminAuthRepository interface present
- [ ] IAdminService interface present (if using)

**THEN:**
```bash
cd backend/TradingPlatform.Api
dotnet build
```
**Expected:** ✅ Exit code 0

---

## 🚀 DEPLOYMENT ORDER

1. ✅ Fix all 7 immediate issues above
2. ✅ Run `dotnet build` - verify 0 errors
3. ⏭️ Implement backend controllers + services
4. ⏭️ Run API tests - verify 2FA flow works
5. ⏭️ Implement frontend components
6. ⏭️ Test end-to-end flows
7. ⏭️ Docker build & run

---

## 📝 NOTES

- **Token Types:** User tokens (60min) vs Admin temp tokens (5min, with custom claims) vs Admin final tokens (60min)
- **2FA:** MANDATORY for ALL admins, cannot login without it
- **Master Key:** Must be ≥32 characters (will be hashed with PBKDF2)
- **Backup Codes:** 8 one-time codes generated during 2FA setup
- **Settings:** Both TwoFactorSettings and EncryptionSettings must come from appsettings.json
