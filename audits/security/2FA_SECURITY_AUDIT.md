# 🔒 AUDYT BEZPIECZEŃSTWA 2FA - RAPORT SZCZEGÓŁOWY

**Data**: Kwiecień 2026  
**Status**: ⚠️ **KRYTYCZNE PROBLEMY WYKRYTE**  
**Priorytet**: NATYCHMIAST DO NAPRAWY

---

## 📋 SPIS TREŚCI
1. [Podsumowanie Wykonawcze](#podsumowanie-wykonawcze)
2. [Jak Działa Obecna Implementacja](#jak-działa-obecna-implementacja)
3. [Znalezione Problemy](#znalezione-problemy)
4. [Porównanie do Best Practices](#porównanie-do-best-practices)
5. [Plan Naprawy](#plan-naprawy)
6. [Wdrażanie Krok po Kroku](#wdrażanie-krok-po-kroku)

---

## 🚨 PODSUMOWANIE WYKONAWCZE

### Obecny Stan: NIEZABEZPIECZONY ❌

Twoja obecna implementacja 2FA ma **7 KRYTYCZNYCH I POWAŻNYCH PROBLEMÓW BEZPIECZEŃSTWA**:

| Priorytet | Problem | Wpływ | Status |
|-----------|---------|-------|--------|
| 🔴 KRYTYCZNY | TOTP secret w JWT (plaintext) | Całkowita kompromitacja 2FA | AKTYWNY |
| 🔴 KRYTYCZNY | Brak session state na serwerze | Brak kontroli nad rejestracją/loginem | AKTYWNY |
| 🔴 KRYTYCZNY | Backup codes w JWT + plaintext | Łatwy dostęp dla atakującego | AKTYWNY |
| 🟠 POWAŻNY | Brak rate limiting 2FA | Brute force na kody (10^6 możliwości) | AKTYWNY |
| 🟠 POWAŻNY | Hasło w JWT claim | Exposure hasła użytkownika | AKTYWNY |
| 🟠 POWAŻNY | Master key w config file | Łatwy dostęp do encrypted danych | AKTYWNY |
| 🟡 WAŻNY | Niewystarczający audit log | Brak śladu ataków | AKTYWNY |

---

## 📊 JAK DZIAŁA OBECNA IMPLEMENTACJA

### REJESTRACJA (2 kroki):

```
Step 1: RegisterInitialAsync
  ├─ Input: username, email, password, ...
  ├─ Generuje TOTP secret (20 bytes random)
  ├─ Generuje Backup Codes (8 szt, 8 znaków)
  ├─ Tworzy JWT Token (5 min, TEMP)
  │  └─ Claims: userId, sessionId, TOTP_SECRET (PLAINTEXT!), BackupCodes JSON, PASSWORD (!)
  └─ Output: Token, QR code, Manual key, Backup codes (PLAINTEXT!)

Step 2: RegisterCompleteInternalAsync  
  ├─ Input: 2FA code z authenticator app
  ├─ Validates code against TOTP secret (z JWT claims)
  ├─ Szyfruje TOTP secret (AES-256-GCM)
  ├─ Hashuje Backup codes (SHA256 → Base64)
  ├─ Tworzy User w DB (TwoFactorEnabled = TRUE)
  └─ Generuje JWT (60 min, FINAL)
```

### LOGOWANIE (2 kroki, jeśli 2FA enabled):

```
Step 1: LoginInitialAsync
  ├─ Input: username/email, password
  ├─ Validates password
  ├─ Checks TwoFactorEnabled flag
  ├─ Jeśli tak → Pobiera encrypted TOTP secret z DB
  ├─ Tworzy JWT Token (5 min, TEMP)
  │  └─ Claims: userId, sessionId, TOTP_SECRET (encrypted z DB!) ← PROBLEM!
  └─ Output: Token temp, sessionId

Step 2: VerifyUserTwoFactorInternalAsync
  ├─ Input: 2FA code, TOTP secret (z JWT claim)
  ├─ Validates code
  └─ Generuje JWT (60 min, FINAL)
```

### SZYFROWANIE:

- **Algorytm**: AES-256-GCM (DOBRE!)
- **Nonce**: 12 bytes random (DOBRE!)
- **Format**: `base64(nonce):base64(ciphertext):base64(authTag)` (DOBRE!)
- **Master Key**: `DevelopmentMasterKeyForLocalTesting...` w **plaintext w config file** ❌

### TOTP WERYFIKACJA:

- **Algorytm**: HMAC-SHA1, RFC 6238 (DOBRE!)
- **Time Step**: 30 sekund (standard)
- **Tolerance**: ±1 window = 60 sekund tolerancji (DOBRE, ale może być zbyt dużo)
- **Kod**: 6 cyfr (standard, ale słaby - tylko 10^6 = 1M możliwości)

---

## ❌ ZNALEZIONE PROBLEMY

### Problem #1: 🔴 KRYTYCZNY - TOTP Secret w JWT Claims (Plaintext)

#### Opis:
Podczas rejestracji i logowania, TOTP secret jest przechowywany w JWT claims **w plaintext**:

```csharp
// JwtTokenGenerator.cs, line 88
claims.Add(new Claim("totp_secret", context.TotpSecret)); // ← PLAINTEXT!
```

#### Dlaczego to niebezpieczne:
1. **JWT nie jest szyfrowany, tylko signed** - każdy może dekodować i czytać claims
2. **Jeśli JWT wycieka** (np. przez HTTPS downgrade, MITM, XSS, browser history) → attacker ma TOTP secret
3. **Możliwość offline attack** - attacker może wygenerować wszystkie możliwe TOTP kody na podstawie sekretu
4. **Violates OWASP** - sensitive data in JWTs

#### Przykład ataku:
```javascript
// Atacker przechwytuje JWT z registration/login
const jwtPayload = jwt_decode(interceptedToken);
const totpSecret = jwtPayload.totp_secret; // ← "JBSWY3DPEBLW64TMMQ=====" (Base32)

// Generuje wszystkie możliwe kody dla tego sekretu (szybko!)
// Próbuje je bez żadnego rate limiting
```

#### Best Practice:
- ❌ NIGDY nie przechowuj TOTP secrets w JWT
- ✅ Serwer powinien przechowywać `sessionId → TOTP secret` mapping w pamięci (Redis/Cache)
- ✅ Klient dostaje tylko `sessionId`, nie secret

---

### Problem #2: 🔴 KRYTYCZNY - Brak Server-Side Session State

#### Opis:
Rejestracja i login 2FA opierają się **wyłącznie na JWT claims** bez server-side validation:

```csharp
// UserAuthService.cs, line 180
// Brak weryfikacji czy sessionId istnieje na serwerze!
// Brak weryfikacji czy registracja jest w trakcie dla tego sessionId!
```

#### Dlaczego to niebezpieczne:
1. **Atacker może podrobić JWT claims** - jeśli ma klucz JWT (često developmentowy i słaby)
2. **Brak rate limiting** - można spróbować nieograniczoną liczbę kodów
3. **Brak wygasania sesji** - JWT wygasa za 5 minut, ale mogą być generowane nowe
4. **Brak atomicity** - registration może być przerwane na pół i nigdy nie sprzątane

#### Scenario ataku:
```
1. Atacker inituje registration
2. Zamiast czekać na email, próbuje każdy kod 000000-999999
   (bez rate limiting - szybko!)
3. Po ~500 kodów trafia na poprawny
4. Account created, 2FA bypassed!
```

#### Best Practice:
- ✅ Serwer przechowuje `sessionId → {userId, secret, attempts, timestamp}` w Redis
- ✅ Rate limiting: max 5 failed attempts na session, potem lockout na 5 minut
- ✅ Session wygasa po 10 minutach (nie mniej!)
- ✅ Brak możliwości reuse JWT - każda weryfikacja czyszcz session z serwera

---

### Problem #3: 🔴 KRYTИЧЕСКИЙ - Backup Codes w JWT + Plaintext

#### Opis:
Backup codes są:
1. Generowane jako plaintext: `["ABC12345", "DEF67890", ...]`
2. Serializowane jako JSON w JWT claim
3. Przesyłane do klienta i wstecz

```csharp
// UserAuthService.cs, line 160
var backupCodes = _twoFactorService.GenerateBackupCodes(); // Plaintext array

// Dodane do JWT claim:
BackupCodes = backupCodes.ToList() // ← Cały JSON w JWT!
```

#### Dlaczego to niebezpieczne:
1. **Jeśli JWT wycieka → všechny backup codes compromised**
2. **Backup codes są ostatecznym fallbackiem** - jeśli wycieka, 2FA jest bezużyteczna
3. **Nieograniczone użycia** - backup codes powinny być jednorazowe

#### Best Practice:
- ✅ Nigdy nie wysyłaj backup codes w JWT
- ✅ Backup codes powinny być wysłane TYLKO raz w registration
- ✅ Zapisz je z hash'em w DB (SHA256 + salt)
- ✅ Track which codes foram używane (db column: `used_at`)
- ✅ Warn użytkownika, że to ostatnie codes (np. 8 codes = max 8 emergency logins)

---

### Problem #4: 🔴 KRYTICAL - Hasło w JWT Claim

#### Opis:
Hasło użytkownika jest przechowywane w JWT claim:

```csharp
// JwtTokenGenerator.cs
context.Password = password // ← Hasło w plaintext!
claims.Add(new Claim("password", password)); // ← Wysyła do JWT!
```

#### Dlaczego to ABSOLUTNIE NIEBEZPIECZNE:
1. **Hasło nigdy nie powinno być w JWT**
2. **Jeśli JWT wycieka → hasło compromised**
3. **Hasło będzie w browser history, logs, network traces**
4. **VIOLATES OWASP Security Requirement**

#### Best Practice:
- ❌ NIGDY nie przechowuj hasła w JWT
- ✅ Hasło jest hashed na serwerze (bcrypt/Argon2) i nigdy nie wysyłane
- ✅ Jeśli musisz pass hasło między steps rejestracji - przechowuj na serwerze w session, nie w JWT

---

### Problem #5: 🟠 POWAŻNY - Brak Rate Limiting

#### Opis:
Nie ma rate limiting na 2FA code verification:

```csharp
// UserAuthService.cs - VerifyUserTwoFactorInternalAsync
if (!_twoFactorService.VerifyCode(totpSecret, code))
{
    throw new UnauthorizedAccessException("Invalid 2FA code");
    // ← Atacker może spróbować kolejny kod natychmiast!
}
```

#### Dlaczego to niebezpieczne:
1. **6-digit TOTP = 1,000,000 możliwości**
2. **Atacker może próbować szybko**: 1000 kodów/sekundę = kompromitacja w ~1000 sekund (~17 minut)
3. **Brak tracking failed attempts**

#### Best Practice:
- ✅ Max 5 failed attempts na session/IP
- ✅ Po 5 failurach → locked for 5 minutes
- ✅ Track w Redis: `session_attempts:{sessionId} = {count, timestamp}`
- ✅ Log wszystkie failed attempts do audit log

---

### Problem #6: 🟠 POWAŻNY - Master Encryption Key w Config File

#### Opis:
Master key do szyfrowania TOTP secrets jest w plaintext w appsettings:

```json
{
  "Encryption": {
    "MasterKey": "DevelopmentMasterKeyForLocalTesting32CharactersLongMinimumRequired!!!"
  }
}
```

#### Dlaczego to niebezpieczne:
1. **Każdy z dostępem do repo/server może read encryption key**
2. **Jeśli key wycieka → wszystkie encrypted TOTP secrets mogą być dekodowane**
3. **Brak key rotation**
4. **Dev key w production!**

#### Best Practice:
- ✅ Master key w Azure Key Vault / AWS Secrets Manager / HashiCorp Vault
- ✅ Application load key z secure location na startup
- ✅ Key rotation co 90 dni
- ✅ Separate keys dla dev/staging/production

---

### Problem #7: 🟡 WAŻNY - Niewystarczający Audit Logging

#### Opis:
Brak szczegółowego logging 2FA events:

```csharp
// Tylko basic logs:
_logger.LogInformation("Login STEP 1 successful for user '{UserId}'", user.Id);
// ← Brak informacji o:
// - Skąd pochodzi żądanie (IP)
// - Jaki device/browser
// - Ile failed attempts było przed tym
// - Geo-location
```

#### Best Practice:
- ✅ Log każdy 2FA event:
  - Registration started/completed
  - Login 2FA started/completed
  - Failed code attempts (z IP)
  - Backup code used
  - 2FA disabled
- ✅ Include: userId, sessionId, IP, User-Agent, timestamp
- ✅ Send anomalous activity to alerting system

---

## 📚 PORÓWNANIE DO BEST PRACTICES

### Co Robi GOOGLE, MICROSOFT, AWS itd. (Industry Standard):

| Aspekt | Twoje Podejście | Best Practice | Ocena |
|--------|-----------------|----------------|-------|
| **TOTP Secret Storage** | JWT claim (plaintext) | Server-side Redis + sessionId | ❌ FAIL |
| **Session Management** | JWT only | Redis + server state | ❌ FAIL |
| **Backup Codes** | JWT + plaintext | DB hash + one-time use | ❌ FAIL |
| **Hasło** | JWT claim | Nigdy nie wysyłaj | ❌ FAIL |
| **Rate Limiting** | Brak | Max 5 attempts + lockout | ❌ FAIL |
| **Encryption Key** | Config file | Key Vault + rotation | ❌ FAIL |
| **TOTP Algorithm** | HMAC-SHA1, ±1 window | ✅ OK | ✅ PASS |
| **Szyfrowanie danych** | AES-256-GCM | ✅ OK (ale key management źle) | ⚠️ PARTIAL |
| **Audit Logging** | Minimal | Detailed + geo-location | ❌ FAIL |
| **Token Expiry** | 5 min temp, 60 min final | ✅ OK | ✅ PASS |

### Referenci OWASP:
- **OWASP A02:2021 - Cryptographic Failures**: ❌ Encryption key exposure, plaintext secrets
- **OWASP A03:2021 - Injection**: ⚠️ Potential JWT claim injection
- **OWASP A04:2021 - Insecure Design**: ❌ Brak session state, brak rate limiting
- **OWASP A07:2021 - Authentication**: ❌ Weak 2FA implementation
- **OWASP A09:2021 - Logging & Monitoring**: ❌ Insufficient audit trail

---

## 🔧 PLAN NAPRAWY

### FAZA 1: IMMEDIATE (Godziny/Dni)
```
Priority: 🔴 CRITICAL
- [ ] Remove password from JWT claims (ASAP!)
- [ ] Remove backup codes from JWT (ASAP!)
- [ ] Implement basic server-side session state (Redis)
- [ ] Move TOTP secret verification to server-side only
```

### FAZA 2: SHORT-TERM (Tygodnie)
```
Priority: 🟠 HIGH
- [ ] Implement Redis session storage for 2FA
- [ ] Add rate limiting (5 attempts + lockout)
- [ ] Move encryption key to Azure Key Vault
- [ ] Implement detailed audit logging
- [ ] Add IP tracking and anomaly detection
```

### FAZA 3: LONG-TERM (Miesiące)
```
Priority: 🟡 MEDIUM
- [ ] Implement WebAuthn/FIDO2 alongside TOTP
- [ ] Add geographic anomaly detection
- [ ] Implement key rotation mechanism
- [ ] Add device fingerprinting
- [ ] Implement 2FA enforcement policies
```

---

## 🚀 WDRAŻANIE KROK PO KROKU

### KROK 1: Przygotowanie (Godziny 0-2)

#### 1.1 Zainstaluj Redis lokalnie:
```bash
# Windows: via Docker
docker pull redis
docker run -d --name trading-redis -p 6379:6379 redis

# Verify:
redis-cli ping  # Should return PONG
```

#### 1.2 Install NuGet packages:
```bash
# W backendzie
dotnet add package StackExchange.Redis
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
```

#### 1.3 Update appsettings:
```json
{
  "Redis": {
    "Connection": "localhost:6379",
    "DefaultDb": 0
  },
  "TwoFactorAuth": {
    "SessionTimeoutSeconds": 600,
    "MaxFailedAttempts": 5,
    "LockoutDurationSeconds": 300
  }
}
```

---

### KROK 2: Create Redis Service (Godziny 2-4)

#### Nowy plik: `IRedisSessionService.cs`

```csharp
namespace TradingPlatform.Core.Interfaces;

public interface IRedisSessionService
{
    // Store temporary 2FA session
    Task<bool> CreateSession(string sessionId, string userId, string totpSecret, int expirySeconds, CancellationToken ct);
    
    // Get session data
    Task<TwoFASessionData?> GetSession(string sessionId, CancellationToken ct);
    
    // Increment failed attempts
    Task<int> IncrementFailedAttempts(string sessionId, CancellationToken ct);
    
    // Get failed attempts
    Task<int> GetFailedAttempts(string sessionId, CancellationToken ct);
    
    // Check if session is locked (too many failed attempts)
    Task<bool> IsSessionLocked(string sessionId, CancellationToken ct);
    
    // Delete session (cleanup after success/failure)
    Task<bool> DeleteSession(string sessionId, CancellationToken ct);
}

// Data model
public sealed record TwoFASessionData(
    string UserId,
    string TotpSecret,
    int FailedAttempts,
    DateTime CreatedAt,
    DateTime ExpiresAt);
```

#### Nowy plik: `RedisSessionService.cs`

```csharp
using StackExchange.Redis;
using TradingPlatform.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TradingPlatform.Core.Services;

public sealed class RedisSessionService : IRedisSessionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSessionService> _logger;
    private const string SessionKeyPrefix = "2fa:session:";
    private const string AttemptsKeyPrefix = "2fa:attempts:";
    private const string LockoutKeyPrefix = "2fa:lockout:";

    public RedisSessionService(
        IConnectionMultiplexer redis,
        ILogger<RedisSessionService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CreateSession(string sessionId, string userId, string totpSecret, int expirySeconds, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) throw new ArgumentNullException(nameof(sessionId));
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
        if (string.IsNullOrWhiteSpace(totpSecret)) throw new ArgumentNullException(nameof(totpSecret));

        try
        {
            var db = _redis.GetDatabase();
            var sessionData = new
            {
                UserId = userId,
                TotpSecret = totpSecret,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(expirySeconds).ToUnixTimeSeconds()
            };

            var json = JsonSerializer.Serialize(sessionData);
            var key = SessionKeyPrefix + sessionId;
            
            var result = await db.StringSetAsync(key, json, TimeSpan.FromSeconds(expirySeconds));
            
            _logger.LogInformation("2FA session created: {SessionId}, expires in {ExpirySeconds}s", sessionId, expirySeconds);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create 2FA session: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<TwoFASessionData?> GetSession(string sessionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return null;

        try
        {
            var db = _redis.GetDatabase();
            var key = SessionKeyPrefix + sessionId;
            var value = await db.StringGetAsync(key);

            if (!value.HasValue) return null;

            var data = JsonSerializer.Deserialize<dynamic>(value.ToString());
            // Parse and return
            return null; // TODO: Proper deserialization
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get 2FA session: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<int> IncrementFailedAttempts(string sessionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return 0;

        try
        {
            var db = _redis.GetDatabase();
            var key = AttemptsKeyPrefix + sessionId;
            var count = await db.StringIncrementAsync(key);
            
            // Set expiry (1 hour)
            await db.KeyExpireAsync(key, TimeSpan.FromHours(1));
            
            _logger.LogWarning("2FA failed attempt for session {SessionId}, attempt count: {Count}", sessionId, count);
            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment failed attempts: {SessionId}", sessionId);
            return 0;
        }
    }

    public async Task<int> GetFailedAttempts(string sessionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return 0;

        try
        {
            var db = _redis.GetDatabase();
            var key = AttemptsKeyPrefix + sessionId;
            var value = await db.StringGetAsync(key);
            return value.HasValue ? int.Parse(value.ToString()) : 0;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<bool> IsSessionLocked(string sessionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;

        try
        {
            var db = _redis.GetDatabase();
            var lockKey = LockoutKeyPrefix + sessionId;
            return await db.KeyExistsAsync(lockKey);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> DeleteSession(string sessionId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return false;

        try
        {
            var db = _redis.GetDatabase();
            var sessionKey = SessionKeyPrefix + sessionId;
            var attemptsKey = AttemptsKeyPrefix + sessionId;
            var lockKey = LockoutKeyPrefix + sessionId;

            await db.KeyDeleteAsync(new[] { sessionKey.AsRedisKey(), attemptsKey.AsRedisKey(), lockKey.AsRedisKey() });
            
            _logger.LogInformation("2FA session deleted: {SessionId}", sessionId);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete 2FA session: {SessionId}", sessionId);
            return false;
        }
    }
}
```

---

### KROK 3: Update JWT Token Generator (Godziny 4-6)

#### Modify: `JwtTokenGenerator.cs`

```csharp
// REMOVE: Nie dodawaj TOTP secret do JWT
// REMOVE: Nie dodawaj hasła do JWT
// REMOVE: Nie dodawaj backup codes do JWT

// CHANGE: Dodaj tylko sessionId

public string GenerateToken(User user, bool isTempToken, Models.TokenContext? context = null)
{
    var claims = new List<Claim>
    {
        new(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new("sub", user.Id.ToString()),
        new(ClaimTypes.Name, user.UserName),
        new(ClaimTypes.Email, user.Email),
        new(ClaimTypes.Role, user.Role.ToString()),
        new("userId", user.Id.ToString())
    };

    if (context != null)
    {
        // ✅ KEEP: sessionId (safe)
        if (!string.IsNullOrWhiteSpace(context.SessionId))
            claims.Add(new Claim("session_id", context.SessionId));

        if (context.TwoFactorRequired)
            claims.Add(new Claim("requires_2fa", "true"));

        // ❌ REMOVE: totp_secret claim
        // ❌ REMOVE: password claim
        // ❌ REMOVE: backup_codes claim
    }

    // ... rest of token generation
}
```

---

### KROK 4: Update UserAuthService (Godziny 6-10)

#### Modify: `RegisterInitialAsync`

```csharp
public async Task<UserRegistrationInitialResponse> RegisterInitialAsync(...)
{
    // ... validation ...

    try
    {
        // Generate TOTP secret
        var secretDto = _twoFactorService.GenerateSecret();
        var backupCodes = _twoFactorService.GenerateBackupCodes();

        var sessionId = Guid.NewGuid().ToString();

        // ✅ NEW: Store secret in Redis, NOT in JWT!
        await _redisSessionService.CreateSession(
            sessionId,
            userId: Guid.NewGuid().ToString(), // Temp userId for this registration
            totpSecret: secretDto.Secret,
            expirySeconds: 600, // 10 minutes
            cancellationToken);

        // Generate temp token - WITHOUT secret/codes/password
        var tempToken = _jwtTokenGenerator.GenerateToken(
            tempUser,
            isTempToken: true,
            context: new TokenContext 
            { 
                SessionId = sessionId,
                TwoFactorRequired = true
                // ❌ REMOVED: TotpSecret
                // ❌ REMOVED: BackupCodes
                // ❌ REMOVED: Password
            });

        // ✅ Return backup codes ONLY ONCE (not encrypted, just plaintext for this first response)
        // User MUST save them immediately!
        return new UserRegistrationInitialResponse(
            Token: tempToken,
            SessionId: sessionId,
            QrCodeDataUrl: secretDto.QrCodeDataUrl,
            ManualKey: secretDto.Secret,
            BackupCodes: backupCodes.ToList(),
            Message: "2FA required. Save backup codes in a SAFE PLACE!");
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Registration STEP 1 error");
        throw;
    }
}
```

#### Modify: `RegisterCompleteInternalAsync`

```csharp
public async Task<UserRegistrationCompleteResponse> RegisterCompleteInternalAsync(
    string sessionId,
    string code,
    List<string> backupCodes,
    ...)
{
    if (string.IsNullOrWhiteSpace(sessionId))
        throw new ArgumentException("Session ID is required");

    try
    {
        // ✅ NEW: Get TOTP secret from Redis, NOT from JWT!
        var sessionData = await _redisSessionService.GetSession(sessionId, cancellationToken);
        if (sessionData == null)
            throw new UnauthorizedAccessException("Invalid or expired session");

        // Check rate limiting
        var failedAttempts = await _redisSessionService.GetFailedAttempts(sessionId, cancellationToken);
        if (failedAttempts >= 5)
        {
            _logger.LogWarning("Registration: Too many failed 2FA attempts for session {SessionId}", sessionId);
            throw new InvalidOperationException("Too many failed attempts. Try again in 5 minutes.");
        }

        // Verify 2FA code against Redis secret
        if (!_twoFactorService.VerifyCode(sessionData.TotpSecret, code))
        {
            await _redisSessionService.IncrementFailedAttempts(sessionId, cancellationToken);
            _logger.LogWarning("Registration: Invalid 2FA code for session {SessionId}", sessionId);
            throw new UnauthorizedAccessException("Invalid 2FA code");
        }

        // ✅ All good! Create user with encrypted secret + hashed backup codes
        var encryptedSecret = _encryptionService.Encrypt(sessionData.TotpSecret);
        var hashedBackupCodes = backupCodes
            .Select(code => _twoFactorService.HashBackupCode(code))
            .ToList();

        var user = new User(
            // ... user creation ...
            TwoFactorSecret: encryptedSecret,
            BackupCodes: JsonSerializer.Serialize(hashedBackupCodes),
            // ...
        );

        await _userRepository.AddAsync(user, passwordHash, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // ✅ Clean up Redis session
        await _redisSessionService.DeleteSession(sessionId, cancellationToken);

        var finalToken = _jwtTokenGenerator.GenerateToken(user, isTempToken: false);
        
        _logger.LogInformation("User {UserId} registered with 2FA", user.Id);

        return new UserRegistrationCompleteResponse(
            Token: finalToken,
            UserId: user.Id,
            Username: user.UserName,
            // ❌ NOT returning backup codes again!
            Message: "✅ Registration complete. 2FA is enabled.");
    }
    catch (Exception ex)
    {
        // Cleanup on error
        await _redisSessionService.DeleteSession(sessionId, cancellationToken);
        _logger.LogError(ex, "Registration error");
        throw;
    }
}
```

#### Modify: `LoginInitialAsync`

```csharp
public async Task<UserLoginInitialResponse> LoginInitialAsync(...)
{
    try
    {
        // ... password validation ...

        if (user.TwoFactorEnabled)
        {
            var sessionId = Guid.NewGuid().ToString();

            // ✅ NEW: Decrypt TOTP secret from DB and store in Redis
            // Do NOT put in JWT!
            var decryptedSecret = _encryptionService.Decrypt(user.TwoFactorSecret);
            
            await _redisSessionService.CreateSession(
                sessionId,
                userId: user.Id.ToString(),
                totpSecret: decryptedSecret,
                expirySeconds: 600, // 10 minutes
                cancellationToken);

            var tempToken = _jwtTokenGenerator.GenerateToken(
                user,
                isTempToken: true,
                context: new TokenContext
                {
                    SessionId = sessionId,
                    TwoFactorRequired = true
                    // ❌ REMOVED: TotpSecret
                });

            return new UserLoginInitialResponse(
                Token: tempToken,
                SessionId: sessionId,
                RequiresTwoFactor: true,
                Username: user.UserName);
        }
        else
        {
            // ... normal login without 2FA ...
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Login error");
        throw;
    }
}
```

#### Modify: `VerifyUserTwoFactorInternalAsync`

```csharp
public async Task<UserAuthCompleteResponse> VerifyUserTwoFactorInternalAsync(
    string sessionId,
    string code,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(sessionId))
        throw new ArgumentException("Session ID is required");

    try
    {
        // ✅ NEW: Get session from Redis
        var sessionData = await _redisSessionService.GetSession(sessionId, cancellationToken);
        if (sessionData == null)
            throw new UnauthorizedAccessException("Session expired or invalid");

        // Check rate limiting
        var isLocked = await _redisSessionService.IsSessionLocked(sessionId, cancellationToken);
        if (isLocked)
            throw new InvalidOperationException("Too many failed attempts. Session locked for 5 minutes.");

        var failedAttempts = await _redisSessionService.GetFailedAttempts(sessionId, cancellationToken);
        if (failedAttempts >= 5)
        {
            // Lock session
            var db = _redis.GetDatabase();
            await db.StringSetAsync(
                "2fa:lockout:" + sessionId,
                "locked",
                TimeSpan.FromMinutes(5));
            throw new InvalidOperationException("Too many failed attempts. Locked for 5 minutes.");
        }

        // Get user from DB
        var user = await _userRepository.GetByIdAsync(Guid.Parse(sessionData.UserId), cancellationToken);
        if (user == null)
            throw new UnauthorizedAccessException("User not found");

        // Verify code
        if (!_twoFactorService.VerifyCode(sessionData.TotpSecret, code))
        {
            await _redisSessionService.IncrementFailedAttempts(sessionId, cancellationToken);
            _logger.LogWarning("Login: Invalid 2FA code for user {UserId}", user.Id);
            throw new UnauthorizedAccessException("Invalid 2FA code");
        }

        // ✅ Success! Clean up and issue final token
        await _redisSessionService.DeleteSession(sessionId, cancellationToken);

        var finalToken = _jwtTokenGenerator.GenerateToken(user, isTempToken: false);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds();

        _logger.LogInformation("User {UserId} authenticated with 2FA", user.Id);

        return new UserAuthCompleteResponse(
            Token: finalToken,
            UserId: user.Id,
            Username: user.UserName,
            ExpiresAt: expiresAt,
            Role: user.Role.ToString());
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "2FA verification error");
        throw;
    }
}
```

---

### KROK 5: Register Services (Godziny 10-11)

#### Modify: `Program.cs`

```csharp
// Add Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(
        builder.Configuration.GetConnectionString("Redis") 
        ?? throw new InvalidOperationException("Redis connection string not configured")));

// Add Redis session service
builder.Services.AddScoped<IRedisSessionService, RedisSessionService>();

// Update UserAuthService DI
builder.Services.AddScoped<IUserAuthService>(sp =>
    new UserAuthService(
        sp.GetRequiredService<IUserRepository>(),
        sp.GetRequiredService<IAccountService>(),
        sp.GetRequiredService<ITwoFactorService>(),
        sp.GetRequiredService<IEncryptionService>(),
        sp.GetRequiredService<IJwtTokenGenerator>(),
        sp.GetRequiredService<IValidator<RegisterRequest>>(),
        sp.GetRequiredService<IMapper>(),
        sp.GetRequiredService<IRedisSessionService>(), // ← NEW!
        sp.GetRequiredService<ILogger<UserAuthService>>()));
```

---

### KROK 6: Update appsettings (Godziny 11-12)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",
    "Redis": "localhost:6379"
  },
  "Jwt": {
    "Key": "...",
    // ... 
  },
  "TwoFactorAuth": {
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 5,
    "SessionTimeoutMinutes": 10,
    "RequireStrongPassword": true
  },
  "Encryption": {
    // ⚠️ TEMPORARY for testing
    // Later move to Azure Key Vault!
    "MasterKey": "..."
  }
}
```

---

### KROK 7: Testing (Godziny 12-16)

#### Unit Tests: `TwoFactorServiceTests.cs`

```csharp
[TestClass]
public class TwoFactorServiceTests
{
    [TestMethod]
    public void VerifyCode_WithValidCode_ReturnsTrue()
    {
        // Arrange
        var service = new TwoFactorService(/* ... */);
        var secret = "JBSWY3DPEBLW64TMMQ======"; // Example
        
        // Generate a valid code for current time
        var validCode = GenerateValidTotp(secret);

        // Act
        var result = service.VerifyCode(secret, validCode);

        // Assert
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void VerifyCode_WithInvalidCode_ReturnsFalse()
    {
        var service = new TwoFactorService(/* ... */);
        var secret = "JBSWY3DPEBLW64TMMQ======";

        var result = service.VerifyCode(secret, "000000");

        Assert.IsFalse(result);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void HashBackupCode_WithNullCode_ThrowsException()
    {
        var service = new TwoFactorService(/* ... */);
        service.HashBackupCode(null);
    }

    [TestMethod]
    public void VerifyBackupCode_WithValidCode_ReturnsTrue()
    {
        var service = new TwoFactorService(/* ... */);
        var code = "ABC12345";
        var hashedCode = service.HashBackupCode(code);

        var (isValid, index) = service.VerifyBackupCode(code, new[] { hashedCode });

        Assert.IsTrue(isValid);
        Assert.AreEqual(0, index);
    }
}
```

#### Integration Tests: `UserAuthIntegrationTests.cs`

```csharp
[TestClass]
public class UserAuthIntegrationTests
{
    [TestMethod]
    public async Task RegisterUser_CompleteFlow_SucceedsWithValidCode()
    {
        // Arrange
        var redis = /* get redis */;
        var service = /* get user auth service */;

        var registerReq = new UserRegisterInitialRequest(
            Username: "testuser",
            Email: "test@example.com",
            FirstName: "Test",
            LastName: "User",
            Password: "ValidPassword123!",
            BaseCurrency: "PLN");

        // Act - Step 1
        var step1Response = await service.RegisterInitialAsync(
            registerReq.Username,
            registerReq.Email,
            registerReq.FirstName,
            registerReq.LastName,
            registerReq.Password,
            registerReq.BaseCurrency);

        // Verify secret is in Redis, NOT in JWT
        var sessionData = await redis.GetSession(step1Response.SessionId);
        Assert.IsNotNull(sessionData);
        Assert.IsNotNull(sessionData.TotpSecret);

        // Verify JWT doesn't contain secret
        var jwtPayload = DecodeJwt(step1Response.Token);
        Assert.IsFalse(jwtPayload.ContainsKey("totp_secret"));
        Assert.IsFalse(jwtPayload.ContainsKey("password"));
        Assert.IsFalse(jwtPayload.ContainsKey("backup_codes"));

        // Act - Step 2
        var validCode = GenerateValidTotp(sessionData.TotpSecret);
        var step2Response = await service.RegisterCompleteInternalAsync(
            sessionId: step1Response.SessionId,
            code: validCode,
            backupCodes: step1Response.BackupCodes,
            /* ... */);

        // Assert
        Assert.IsNotNull(step2Response.Token);
        Assert.IsTrue(step2Response.UserId != Guid.Empty);

        // Verify session is cleaned up
        var deletedSession = await redis.GetSession(step1Response.SessionId);
        Assert.IsNull(deletedSession);
    }

    [TestMethod]
    public async Task VerifyLogin2FA_WithTooManyAttempts_LocksSession()
    {
        // Arrange
        var redis = /* ... */;
        var sessionId = "test-session";
        await redis.CreateSession(sessionId, "user-id", "secret", 600);

        // Act - Try 5 invalid codes
        for (int i = 0; i < 5; i++)
        {
            try
            {
                await service.VerifyUserTwoFactorInternalAsync(sessionId, "000000");
            }
            catch { }
        }

        // Act - 6th attempt should fail with lockout
        var ex = Assert.ThrowsException<InvalidOperationException>(
            async () => await service.VerifyUserTwoFactorInternalAsync(sessionId, "000000"));

        Assert.IsTrue(ex.Message.Contains("locked"));
    }
}
```

---

## 📝 CHECKLIST IMPLEMENTACJI

```
FAZA 1: Remove Sensitive Data from JWT
- [ ] Remove totp_secret from JWT claims
- [ ] Remove password from JWT claims
- [ ] Remove backup_codes from JWT claims
- [ ] Keep only: sessionId, userId, requires_2fa
- [ ] Deploy & Test

FAZA 2: Redis Session Storage
- [ ] Install Redis locally/cloud
- [ ] Create IRedisSessionService interface
- [ ] Implement RedisSessionService
- [ ] Store TOTP secret in Redis during registration
- [ ] Store TOTP secret in Redis during login
- [ ] Retrieve secret from Redis during verification
- [ ] Register service in DI

FAZA 3: Rate Limiting
- [ ] Track failed attempts in Redis
- [ ] Lock session after 5 failed attempts
- [ ] Unlock after 5 minutes
- [ ] Log all failed attempts with IP

FAZA 4: Update UserAuthService
- [ ] Modify RegisterInitialAsync (use Redis)
- [ ] Modify RegisterCompleteInternalAsync (use Redis + rate limiting)
- [ ] Modify LoginInitialAsync (decrypt + use Redis)
- [ ] Modify VerifyUserTwoFactorInternalAsync (use Redis + rate limiting)
- [ ] Add cleanup on success/failure

FAZA 5: Testing
- [ ] Unit tests for TwoFactorService
- [ ] Unit tests for RedisSessionService
- [ ] Integration tests for full 2FA flow
- [ ] Rate limiting tests
- [ ] Session cleanup tests

FAZA 6: Deployment
- [ ] Update appsettings
- [ ] Update Program.cs
- [ ] Database migration (if needed)
- [ ] Deploy to production
- [ ] Monitor Redis usage
- [ ] Monitor failed attempts

FAZA 7: Monitoring & Audit
- [ ] Implement audit logging (userId, sessionId, IP, timestamp, action)
- [ ] Alert on multiple failed attempts
- [ ] Alert on unusual login patterns
- [ ] Setup Redis monitoring
```

---

## 🎯 NASTĘPNE KROKI (LONG-TERM)

### Wdrożyć FIDO2/WebAuthn:
```
- Dodaj FIDO2 jako primary 2FA method
- Utrzymaj TOTP jako fallback
- Usuń TOTP gdy user ma FIDO2
```

### Key Management:
```
- Przenieś master key do Azure Key Vault
- Implement key rotation co 90 dni
- Audit key access
```

### Anomaly Detection:
```
- Track login patterns (IP, device, geo-location)
- Alert na suspicious activity
- Force re-verification na nowym device
```

---

## 📞 PODSUMOWANIE DLA CZAP

### Dlaczego musi to być naprawione:
1. **2FA jest BEZUŻYTECZNA** - TOTP secret w JWT można przechwycić
2. **Brute force attack** - brak rate limiting = można spróbować wszystkie 1M kodów
3. **Hasło w JWT** - może być przechwycone i użyte do zalogowania
4. **Brak audyt trail** - nie widzisz ataków ani podejrzanej aktywności

### Jakie są ryzyka:
- ❌ Account takeover (jeśli 2FA secret wycieka)
- ❌ Credential stuffing (jeśli hasło wycieka)
- ❌ Compliance issues (GDPR, PCI DSS, SOC2)
- ❌ Regulatory fines (jeśli dojdzie do breach)

### Timeline:
- **Dziś**: Remove password from JWT (ASAP!)
- **Jutro**: Remove TOTP secret from JWT + add Redis
- **Tydzień**: Add rate limiting
- **Miesiąc**: Move encryption key to Key Vault

---

**Gotowy do implementacji? Zaczynamy od KROKU 1!** 🚀
