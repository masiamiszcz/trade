# Admin Invite 401 Unauthorized - ROOT CAUSE ANALYSIS

## Issue Summary
- **Endpoint:** `POST /api/auth/admin/invite`  
- **Status:** ❌ 401 Unauthorized → ✅ **FIXED: 201 Created**
- **Token:** Valid JWT with `role: Admin` and `is_super_admin: true`  
- **Root Causes:** 2 configuration issues in JWT setup
- **Fix Applied:** Updated Program.cs JWT configuration

---

## Pipeline Failure Analysis

### ❌ PROBLEM #1: RequireHttpsMetadata = true (FIXED ✅)

**Location:** [Program.cs](../backend/TradingPlatform.Api/Program.cs) Line 60

**Original Config:**
```csharp
options.RequireHttpsMetadata = true;
```

**Issue:**
- JWT Bearer middleware checks if connection is HTTPS when `RequireHttpsMetadata = true`
- localhost uses HTTP protocol
- Validation fails BEFORE reaching authorization step
- Returns 401 immediately with no detailed error

**Attack Vector:**
```
Request (HTTP) → JwtBearer Middleware
├─ Check: Is connection HTTPS?
├─ Result: NO (localhost = HTTP)
└─ Action: Return 401 (token never validated)

Authorization step never reached!
```

**Status:** ✅ FIXED

---

### ❌ PROBLEM #2: MapInboundClaims = true (FIXED ✅)

**Location:** [Program.cs](../backend/TradingPlatform.Api/Program.cs) Line 61

**Original Config:**
```csharp
options.MapInboundClaims = true;
```

**Issue:**
When `MapInboundClaims = true`, .NET transforms JWT claims to standard types:
- `"sub"` → maps to `ClaimTypes.NameIdentifier`
- Custom claims like `"is_super_admin"` are preserved

**Controller expects "sub" claim:**
```csharp
// AdminAuthController.cs line 852
var adminIdClaim = User.FindFirst("sub");  // ← Looks for "sub"
if (adminIdClaim == null || !Guid.TryParse(...))
    return Unauthorized();  // ← Returns 401!
```

**What happens with MapInboundClaims = true:**
1. Token contains: `"sub": "a6637de4-..."`
2. JwtBearer maps it to: `ClaimTypes.NameIdentifier` (internal name)
3. `User.FindFirst("sub")` finds NOTHING → returns null
4. Authorization check fails → 401 Unauthorized

**Status:** ✅ FIXED

---

## Solution Applied

### Changes to Program.cs (Lines 59-61)

```csharp
// BEFORE (Broken)
options.RequireHttpsMetadata = true;
options.SaveToken = true;
options.MapInboundClaims = true;

// AFTER (Fixed)
options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
options.SaveToken = true;
options.MapInboundClaims = false;
```

**Rationale:**
1. `RequireHttpsMetadata = !IsDevelopment()` → Allow HTTP in dev, enforce HTTPS in prod
2. `MapInboundClaims = false` → Preserve custom JWT claims ("sub", "is_super_admin") without mapping

---

## Verification Test

**Test Command:**
```powershell
POST /api/auth/admin/invite
Authorization: Bearer <super-admin-token>
Body: {"email":"newadmin@example.com","firstName":"New","lastName":"Admin"}
```

**Before Fix:** 401 Unauthorized ❌
**After Fix:** 201 Created ✅

**Response:**
```json
{
  "token": "RVHHy5Z8XckmVKdCgaGCCgorKJrDpUg2",
  "email": "newadmin@example.com",
  "expiresAt": "2026-04-24T09:49:41.8212154+00:00",
  "invitationUrl": "https://yourapp.com/admin/register?token=RVHHy5Z8XckmVKdCgaGCCgorKJrDpUg2"
}
```

---

## Impact Summary

| Issue | Severity | Impact |
|-------|----------|--------|
| RequireHttpsMetadata | 🔴 Critical | All JWT endpoints reject HTTP requests silently |
| MapInboundClaims | 🔴 Critical | Custom claim handlers break authorization |
| **Combined** | 🔴 Critical | 401 on all admin/auth endpoints over HTTP |

---

## Key Findings

✅ **Token is valid** - Claims structure correct
✅ **Role is present** - `role: Admin` in token  
✅ **Endpoint is correct** - Route matches properly
❌ **Middleware configuration was wrong** - 2 settings preventing token reach

---

## Lessons Learned

1. **MapInboundClaims transforms claim types** - Can break code expecting custom claim names
2. **RequireHttpsMetadata is strict** - Rejects HTTP automatically without entering controller
3. **Always test with actual environment** - Development needs different JWT config than production
4. **Custom claims should not be mapped** - Keep them as-is for direct claim extraction

