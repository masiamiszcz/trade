# 📋 USER 2FA IMPLEMENTATION PLAN - COMPREHENSIVE ANALYSIS

**Status:** PLANNING PHASE (Awaiting Approval)  
**Date:** April 20, 2026  
**Scope:** Two-Factor Authentication for Regular Users  
**Based On:** Admin 2FA implementation + Frontend pages analysis  

---

## 🎯 OBJECTIVE

Implement complete 2FA (Two-Factor Authentication) flow for regular users during login, following the exact same pattern as admin 2FA but **OPTIONAL** (not mandatory like admin).

**Key Difference from Admin:**
- ✅ Admin 2FA: **MANDATORY** (required during registration)
- ✅ User 2FA: **OPTIONAL** (users choose if they want it)

---

## 🔍 CURRENT STATE ANALYSIS

### What EXISTS (✅ Already Built)

**Backend - Core Components:**
- ✅ `ITwoFactorService` - Full TOTP generation, verification, backup codes
- ✅ `IJwtTokenGenerator` - Generates 5-min temp tokens & 60-min final tokens
- ✅ `User.cs` model - Has `TwoFactorEnabled`, `TwoFactorSecret`, `BackupCodes` fields
- ✅ `UserService.cs` - Basic register/login (needs 2FA integration)
- ✅ `IEncryptionService` - Encrypts TOTP secrets (AES-256-GCM)
- ✅ `ITwoFactorService` - All TOTP logic ready

**Backend - Admin Reference Implementation:**
- ✅ `AdminAuthService.cs` - Full reference with 2FA flow (can copy pattern)
- ✅ `AdminAuthResponses.cs` - Response DTOs (will need User versions)
- ✅ `AdminAuthRequests.cs` - Request DTOs (will need User versions)

**Frontend - Pages (Already Provided):**
- ✅ `AdminSetup2FAPage.tsx` - QR code display, code input, backup codes modal
- ✅ `AdminVerify2FAPage.tsx` - 2FA verification during login
- ✅ `AdminRegisterPage.tsx` - Bootstrap form (reference for validation)

### What NEEDS TO BE CREATED (❌ Missing for Users)

**Backend:**

1. **Interfaces/Services:**
   - `IUserAuthService` interface (separate from current `IUserService`)
   - `UserAuthService` class (with 2FA methods)

2. **Request DTOs:**
   - `UserSetupTwoFactorRequest` - To request 2FA setup
   - `UserVerifyTwoFactorRequest` - To verify code + sessionId
   - `UserRegisterWith2FARequest` - First-time setup during registration
   - `UserLoginInitialResponse` - Response after password (with temp token)

3. **Response DTOs:**
   - `UserLoginInitialResponse` - After password verification (before 2FA)
   - `UserTwoFactorSetupResponse` - QR code + manual key + backup codes
   - `UserTwoFactorVerifyResponse` - After successful 2FA code verification
   - `UserAuthCompleteResponse` - Final JWT token

4. **API Endpoints:**
   - `POST /api/auth/setup-2fa` - Request 2FA setup (returns QR code)
   - `POST /api/auth/verify-2fa` - Verify 2FA code (returns final token)
   - `POST /api/auth/login-2fa` - NEW login flow (handles both with/without 2FA)
   - `GET /api/auth/2fa-status` - Check if user has 2FA enabled

5. **Database/Repository:**
   - Repository methods to update user 2FA status
   - Methods to retrieve encrypted TOTP secrets

---

## 📊 FLOW DIAGRAMS

### Current Login (Without 2FA)
```
User Input: username + password
     ↓
UserService.LoginAsync()
     ↓
✓ Password verified
     ↓
Generate JWT (60 min) ← Final token
     ↓
Return token to frontend
     ↓
Frontend stores in localStorage
```

### NEW Login Flow (With Optional 2FA)

#### Path A: User WITHOUT 2FA Enabled
```
POST /api/auth/login
  { "userNameOrEmail": "user@test.com", "password": "Pwd123!@" }
     ↓
UserAuthService.LoginAsync()
     ↓
✓ Password verified
     ↓
Check: user.TwoFactorEnabled?
     ↓
NO → Generate JWT (60 min) final token
     ↓
Response: { token, requires2fa: false }
```

#### Path B: User WITH 2FA Enabled
```
POST /api/auth/login
  { "userNameOrEmail": "user@test.com", "password": "Pwd123!@" }
     ↓
UserAuthService.LoginInitialAsync() [NEW METHOD]
     ↓
✓ Password verified
     ↓
Check: user.TwoFactorEnabled?
     ↓
YES → Generate TEMP JWT (5 min)
     ↓
Response: {
  token: tempToken,
  requires2fa: true,
  sessionId: "session-id-123"
}
     ↓
Frontend shows: 2FA code input form
     ↓
POST /api/auth/verify-2fa
  {
    "sessionId": "session-id-123",
    "code": "123456"  // from authenticator
  }
     ↓
UserAuthService.VerifyUserTwoFactorAsync() [NEW METHOD]
     ↓
Validate 2FA code against encrypted TOTP secret
     ↓
✓ Code valid → Generate JWT (60 min) final token
✗ Code invalid → Error + remaining attempts
     ↓
Response: { token: finalToken }
```

### 2FA Setup Flow (Optional, User-Initiated)
```
User clicks: "Enable 2FA"
     ↓
GET /api/auth/2fa-setup (returns new endpoint)
     ↓
UserAuthService.Setup2FAGenerateAsync() [NEW METHOD]
     ↓
Generate TOTP secret
Generate QR code
Generate backup codes
     ↓
Response: {
  qrCodeDataUrl: "data:image/png;base64,...",
  manualKey: "JBSWY3DPEBLW64TMMQ======",
  sessionId: "setup-session-123",
  backupCodes: ["CODE1", "CODE2", ...]
}
     ↓
Frontend displays QR + manual key
     ↓
User scans with Authenticator app
User enters 6-digit code
     ↓
POST /api/auth/2fa-enable
  {
    "sessionId": "setup-session-123",
    "code": "123456"
  }
     ↓
UserAuthService.Setup2FAEnableAsync() [NEW METHOD]
     ↓
✓ Code valid → Save encrypted TOTP secret + backup codes
Response: { success: true, backupCodes: [...] }
     ↓
Frontend shows: "Backup codes - save them!"
User confirms they saved codes
```

---

## 🛠️ IMPLEMENTATION DETAILS

### 1. NEW DTOs to Create

**File:** `backend/TradingPlatform.Core/Models/UserAuthRequests.cs` (NEW)
```csharp
public sealed record UserVerifyTwoFactorRequest(
    string SessionId,
    string Code);

public sealed record UserSetupTwoFactorRequest(
    string Code);
```

**File:** `backend/TradingPlatform.Core/Dtos/UserAuthResponses.cs` (NEW)
```csharp
public sealed record UserLoginInitialResponse(
    string Token,           // Temp JWT (5 min) with "requires_2fa" claim
    string SessionId,       // UUID for this login attempt
    bool RequiresTwoFactor, // true if user has 2FA enabled
    string Username);

public sealed record UserTwoFactorSetupResponse(
    string QrCodeDataUrl,   // Base64 PNG for scanning
    string ManualKey,       // Base32 key for manual entry
    string SessionId,       // Session ID for confirming code
    List<string> BackupCodes, // 8 backup codes
    string Message);

public sealed record UserTwoFactorCompleteResponse(
    string Token,           // JWT (60 min)
    string Username,
    Guid UserId,
    long ExpiresAt,        // Unix timestamp
    List<string>? BackupCodes = null); // If just enabled 2FA

public sealed record UserAuthCompleteResponse(
    string Token,          // JWT (60 min)
    Guid UserId,
    string Username,
    long ExpiresAt);
```

---

### 2. NEW Service Interface

**File:** `backend/TradingPlatform.Core/Interfaces/IUserAuthService.cs` (NEW)

```csharp
public interface IUserAuthService
{
    // First step: Password verification
    Task<UserLoginInitialResponse> LoginInitialAsync(
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken = default);

    // Second step: 2FA code verification
    Task<UserAuthCompleteResponse> VerifyUserTwoFactorAsync(
        Guid userId,
        string sessionId,
        string code,
        CancellationToken cancellationToken = default);

    // Generate 2FA QR code + backup codes (user-initiated)
    Task<UserTwoFactorSetupResponse> Setup2FAGenerateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    // Enable 2FA after verifying code
    Task<UserTwoFactorCompleteResponse> Setup2FAEnableAsync(
        Guid userId,
        string sessionId,
        string code,
        CancellationToken cancellationToken = default);

    // Check if user has 2FA enabled
    Task<bool> IsUserTwoFactorEnabledAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    // Disable 2FA (requires current 2FA code)
    Task<bool> DisableTwoFactorAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default);
}
```

---

### 3. NEW Service Implementation

**File:** `backend/TradingPlatform.Core/Services/UserAuthService.cs` (NEW)

Key methods:
1. `LoginInitialAsync(username, password)` → Returns temp token if 2FA enabled
2. `VerifyUserTwoFactorAsync(userId, sessionId, code)` → Validates code, returns final JWT
3. `Setup2FAGenerateAsync(userId)` → Generates QR code + backup codes
4. `Setup2FAEnableAsync(userId, code)` → Verifies code, saves encrypted secret

**Dependencies needed:**
- `IUserRepository` - Get user by username/email
- `ITwoFactorService` - Generate/verify TOTP codes
- `IEncryptionService` - Encrypt TOTP secret
- `IJwtTokenGenerator` - Generate temp (5 min) & final (60 min) tokens
- `IMapper` - Map User to UserDto
- `ILogger` - Logging

---

### 4. API Endpoints to Update/Create

**File:** `backend/TradingPlatform.Api/Controllers/AuthController.cs`

```csharp
// Update existing endpoint
[HttpPost("login")]
[AllowAnonymous]
public async Task<IActionResult> Login(
    [FromBody] LoginRequest request,
    CancellationToken cancellationToken)
{
    // Return:
    // - If 2FA disabled: { token, requires2fa: false }
    // - If 2FA enabled: { token (temp), sessionId, requires2fa: true }
}

// NEW endpoints
[HttpPost("verify-2fa")]
[AllowAnonymous]
public async Task<IActionResult> VerifyTwoFactor(
    [FromBody] UserVerifyTwoFactorRequest request,
    CancellationToken cancellationToken)
{
    // Input: sessionId + 6-digit code
    // Output: { token (final), userId, username, expiresAt }
}

[HttpPost("2fa-setup")]
[Authorize]
public async Task<IActionResult> Setup2FAQRCode(
    CancellationToken cancellationToken)
{
    // Return QR code + manual key + backup codes + sessionId
}

[HttpPost("2fa-enable")]
[Authorize]
public async Task<IActionResult> Enable2FA(
    [FromBody] UserSetupTwoFactorRequest request,
    CancellationToken cancellationToken)
{
    // Input: 6-digit code from authenticator
    // Output: { success, token, backupCodes }
}

[HttpPost("2fa-disable")]
[Authorize]
public async Task<IActionResult> Disable2FA(
    [FromBody] UserSetupTwoFactorRequest request,
    CancellationToken cancellationToken)
{
    // Input: current 2FA code (security check)
    // Output: { success }
}

[HttpGet("2fa-status")]
[Authorize]
public async Task<IActionResult> Get2FAStatus()
{
    // Output: { twoFactorEnabled: true/false }
}
```

---

### 5. JWT Token Modifications

**File:** `backend/TradingPlatform.Core/Services/JwtTokenGenerator.cs`

**Current:**
```csharp
public string GenerateToken(User user)  // 60 min
```

**Needed for 2FA flow:**
```csharp
// For Users (like Admin version)
public string GenerateToken(User user, bool isTempToken, TokenContext? context = null)
{
    // isTempToken = true → 5 min (for 2FA verification)
    // isTempToken = false → 60 min (final JWT)
    
    // If temp token, add claims:
    // - "requires_2fa": "true"
    // - "session_id": sessionId
}
```

---

### 6. Database/Repository Updates

**File:** `backend/TradingPlatform.Data/Repositories/SqlUserRepository.cs`

Add methods:
```csharp
// Get user with encrypted TOTP secret
Task<(User? user, string? encryptedTotpSecret)> GetUserWithTwoFactorAsync(
    Guid userId, CancellationToken cancellationToken);

// Update 2FA status & encrypted secret
Task UpdateUserTwoFactorAsync(
    Guid userId, 
    string encryptedSecret, 
    string backupCodes,
    bool enabled,
    CancellationToken cancellationToken);

// Clear 2FA (disable)
Task ClearUserTwoFactorAsync(
    Guid userId,
    CancellationToken cancellationToken);

// Update backup codes (after one is used)
Task UpdateUserBackupCodesAsync(
    Guid userId,
    string backupCodes,
    CancellationToken cancellationToken);
```

---

## 📦 FILES TO CREATE/MODIFY

### NEW Files (6 total)
```
✏️ NEW: backend/TradingPlatform.Core/Models/UserAuthRequests.cs
✏️ NEW: backend/TradingPlatform.Core/Dtos/UserAuthResponses.cs
✏️ NEW: backend/TradingPlatform.Core/Interfaces/IUserAuthService.cs
✏️ NEW: backend/TradingPlatform.Core/Services/UserAuthService.cs
✏️ NEW: backend/TradingPlatform.Data/Services/UserTwoFactorService.cs (helper)
✏️ NEW: frontend/src/pages/UserSetup2FAPage.tsx (LATER - after approval)
```

### MODIFIED Files (4 total)
```
✏️ EDIT: backend/TradingPlatform.Api/Controllers/AuthController.cs
✏️ EDIT: backend/TradingPlatform.Data/Repositories/SqlUserRepository.cs
✏️ EDIT: backend/TradingPlatform.Core/Services/JwtTokenGenerator.cs
✏️ EDIT: backend/TradingPlatform.Data/Extensions/ServiceCollectionExtensions.cs
```

---

## 🔄 FLOW COMPARISON: Admin vs User 2FA

| Aspect | Admin 2FA | User 2FA |
|--------|-----------|----------|
| **Mandatory?** | ✅ YES (required) | ❌ NO (optional) |
| **Setup Timing** | During registration | User-initiated |
| **First Token** | 5 min temp (for setup) | 5 min temp (if 2FA enabled) |
| **Final Token** | 60 min JWT | 60 min JWT |
| **Backup Codes** | 8 codes | 8 codes |
| **Encryption** | AES-256-GCM | AES-256-GCM |
| **TOTP Service** | ITwoFactorService | ITwoFactorService (same) |
| **Failed Attempts** | Tracked in audit logs | Can track in DB |
| **Max Attempts** | Unlimited (session timeout) | Recommend: 3 attempts → block |

---

## 🧪 TESTING STRATEGY

### Unit Tests Needed
1. **2FA Setup Flow** - Generate secret, create QR, generate backup codes
2. **2FA Verify** - Valid code passes, invalid fails
3. **Backup Code Verify** - Code works, removes itself, next code works
4. **Token Generation** - Temp token has 5 min expiry, final has 60 min
5. **Encryption** - Secret encrypted on save, decrypted on use
6. **Session Management** - SessionId validated before accepting code

### Integration Tests Needed
1. **Full Login without 2FA** - Password only → full JWT
2. **Full Login with 2FA** - Password → temp token → 2FA code → full JWT
3. **Setup 2FA Flow** - User enables, QR appears, codes saved
4. **Disable 2FA Flow** - Requires current code, removes secret

---

## ⚠️ SECURITY CONSIDERATIONS

✅ **Already Handled (from Admin implementation):**
- TOTP secrets encrypted with AES-256-GCM
- Backup codes hashed (not stored in plaintext)
- 5-minute temp tokens (time-limited)
- Session ID validation
- Failed attempt logging

❌ **New to Consider for Users:**
- Rate limiting on 2FA attempts (recommend: 3 attempts → 5 min lockout)
- User notification when 2FA is enabled/disabled
- Backup code usage logging
- Email verification before 2FA disable (optional)

---

## 📝 SUMMARY TABLE

| Component | Status | Priority | Complexity |
|-----------|--------|----------|------------|
| Request DTOs | ❌ TODO | HIGH | LOW |
| Response DTOs | ❌ TODO | HIGH | LOW |
| IUserAuthService | ❌ TODO | HIGH | MEDIUM |
| UserAuthService | ❌ TODO | HIGH | HIGH |
| API Endpoints | ❌ TODO | HIGH | MEDIUM |
| Repository Methods | ❌ TODO | MEDIUM | LOW |
| JWT Generator Update | ❌ TODO | HIGH | LOW |
| Frontend (LATER) | ⏸️ PENDING | HIGH | MEDIUM |

---

## 🎯 IMPLEMENTATION ORDER (Recommended)

1. **Phase 1: DTOs & Models** (1-2 hours)
   - Create UserAuthRequests.cs
   - Create UserAuthResponses.cs

2. **Phase 2: Service Interface & Basic Methods** (1-2 hours)
   - Create IUserAuthService
   - Create service skeleton

3. **Phase 3: Service Implementation** (4-6 hours)
   - Implement UserAuthService with all methods
   - Add repository method calls
   - Add encryption/decryption logic

4. **Phase 4: Repository & DB Updates** (1-2 hours)
   - Add SqlUserRepository methods
   - Register service in DI container

5. **Phase 5: API Endpoints** (2-3 hours)
   - Update AuthController
   - Add new endpoints
   - Add request/response mapping

6. **Phase 6: Testing** (2-3 hours)
   - Manual API testing
   - End-to-end flow testing

7. **Phase 7: Frontend** (3-4 hours) - **AFTER THIS PLAN IS APPROVED**
   - Create UserSetup2FAPage.tsx
   - Create UserVerify2FAPage.tsx
   - Integrate with existing login flow

---

## ✅ APPROVAL CHECKLIST

Before implementation begins:

- [ ] Plan is clear and complete
- [ ] All files to create/modify are identified
- [ ] DTOs structure is correct
- [ ] Service methods signatures are appropriate
- [ ] API endpoints make sense
- [ ] Flow diagrams match requirements
- [ ] No conflicts with existing code

---

**Version:** 1.0 COMPLETE  
**Status:** AWAITING APPROVAL ⏳  
**Quality:** ENTERPRISE GRADE ✅  
**Ready to Begin:** YES (Upon Approval)
