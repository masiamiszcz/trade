# 🔧 KONKRETNY PLAN NAPRAWY - IMPLEMENTATION READY

> **Status**: Gotowe do skopiowania i wdrożenia  
> **Czas**: ~4-6 godzin pracy (dla doświadczonego developera)  
> **Trudność**: ŚREDNIA

---

## 📋 PRZEGLĄD ZMIAN

```
┌─────────────────────────────────────────────────────────────────┐
│ OBECNY STAN (NIEBEZPIECZNY)                                     │
├─────────────────────────────────────────────────────────────────┤
│ JWT: {userId, sessionId, TOTP_SECRET, PASSWORD, BACKUP_CODES}  │
│ ❌ Wszystko w plaintext!                                        │
│ ❌ Brak server-side validation                                  │
│ ❌ Brak rate limiting                                           │
└─────────────────────────────────────────────────────────────────┘

                              ⬇️  ZMIENIĆ NA  ⬇️

┌─────────────────────────────────────────────────────────────────┐
│ NOWY STAN (BEZPIECZNY)                                          │
├─────────────────────────────────────────────────────────────────┤
│ JWT:     {userId, sessionId, requires_2fa}                      │
│ REDIS:   {sessionId → {userId, SECRET, attempts, expiry}}       │
│ DB:      {userId → {encrypted_SECRET, hashed_BACKUP_CODES}}     │
│ ✅ Bezpieczne!                                                  │
│ ✅ Server-side validation                                       │
│ ✅ Rate limiting + lockout                                      │
│ ✅ Full audit trail                                             │
└─────────────────────────────────────────────────────────────────┘
```

---

## 🎯 FAZA 1: USUWANIE SENSITIVE DATA Z JWT (2 GODZINY)

### Plik 1: `backend/TradingPlatform.Core/Services/JwtTokenGenerator.cs`

**Zmiana**: Usunąć TOTP secret, hasło i backup codes z JWT claims

```csharp
// STARA WERSJA (NIE BEZPIECZNA):
if (context != null)
{
    // ... other claims ...
    
    if (!string.IsNullOrWhiteSpace(context.TotpSecret))
    {
        claims.Add(new Claim("totp_secret", context.TotpSecret)); // ❌ DELETE
    }
    
    // ❌ DELETE password claim
    // ❌ DELETE backup_codes claim
}

// NOWA WERSJA (BEZPIECZNA):
if (context != null)
{
    if (!string.IsNullOrWhiteSpace(context.SessionId))
        claims.Add(new Claim("session_id", context.SessionId)); // ✅ KEEP

    if (context.TwoFactorRequired)
        claims.Add(new Claim("requires_2fa", "true")); // ✅ KEEP
    
    // ✅ REMOVE: totp_secret (store in Redis instead)
    // ✅ REMOVE: password (never send in JWT)
    // ✅ REMOVE: backup_codes (store in DB instead)
}
```

**Dokładny kod do wklejenia:**

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Services;

public sealed class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
    {
        _jwtSettings = jwtSettings.Value;
        if (string.IsNullOrWhiteSpace(_jwtSettings.Key))
            throw new InvalidOperationException("JWT key is not configured.");
    }

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new("sub", user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddMinutes(60),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

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
            // ✅ ONLY sessionId and requires_2fa
            if (!string.IsNullOrWhiteSpace(context.SessionId))
                claims.Add(new Claim("session_id", context.SessionId));

            if (context.TwoFactorRequired)
                claims.Add(new Claim("requires_2fa", "true"));

            // ❌ REMOVED: totp_secret, password, backup_codes
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiry = isTempToken ? 5 : 60;

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expiry),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public Dictionary<string, string>? ValidateTokenAndGetClaims(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            var tokenHandler = new JwtSecurityTokenHandler();

            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = key,
                ValidateIssuer = true,
                ValidIssuer = _jwtSettings.Issuer,
                ValidateAudience = true,
                ValidAudience = _jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out SecurityToken validatedToken);

            var claims = new Dictionary<string, string>();
            foreach (var claim in principal.Claims)
            {
                if (!claims.ContainsKey(claim.Type))
                    claims[claim.Type] = claim.Value;
            }

            return claims;
        }
        catch
        {
            return null;
        }
    }
}
```

---

## 🎯 FAZA 2: REDIS SESSION SERVICE (2 GODZINY)

### Plik 2: `backend/TradingPlatform.Core/Interfaces/IRedisSessionService.cs`

```csharp
namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Service for storing temporary 2FA session data in Redis
/// Stores: userId, totpSecret, attemptCount, createdAt, expiresAt
/// </summary>
public interface IRedisSessionService
{
    /// <summary>Create temporary 2FA session with TOTP secret</summary>
    Task<bool> CreateSessionAsync(string sessionId, string userId, string totpSecret, int timeoutSeconds, CancellationToken ct = default);

    /// <summary>Get session data from Redis</summary>
    Task<TwoFASessionData?> GetSessionAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Get number of failed 2FA attempts for session</summary>
    Task<int> GetFailedAttemptsAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Increment failed attempt counter</summary>
    Task<int> IncrementFailedAttemptsAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Check if session is locked due to too many failed attempts</summary>
    Task<bool> IsSessionLockedAsync(string sessionId, CancellationToken ct = default);

    /// <summary>Lock session for 5 minutes (after max failed attempts)</summary>
    Task<bool> LockSessionAsync(string sessionId, int lockDurationSeconds = 300, CancellationToken ct = default);

    /// <summary>Delete session (cleanup)</summary>
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken ct = default);
}

/// <summary>
/// Data stored in Redis for 2FA session
/// </summary>
public sealed record TwoFASessionData(
    string UserId,
    string TotpSecret,
    DateTime CreatedAt,
    DateTime ExpiresAt);
```

### Plik 3: `backend/TradingPlatform.Core/Services/RedisSessionService.cs`

```csharp
using StackExchange.Redis;
using TradingPlatform.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TradingPlatform.Core.Services;

/// <summary>
/// Redis-based session storage for temporary 2FA data
/// Handles: session creation, retrieval, attempt tracking, lockout
/// </summary>
public sealed class RedisSessionService : IRedisSessionService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisSessionService> _logger;
    
    private const string SessionPrefix = "2fa:session:";
    private const string AttemptsPrefix = "2fa:attempts:";
    private const string LockoutPrefix = "2fa:lockout:";

    public RedisSessionService(
        IConnectionMultiplexer redis,
        ILogger<RedisSessionService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> CreateSessionAsync(
        string sessionId, 
        string userId, 
        string totpSecret, 
        int timeoutSeconds, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) 
            throw new ArgumentNullException(nameof(sessionId));
        if (string.IsNullOrWhiteSpace(userId)) 
            throw new ArgumentNullException(nameof(userId));
        if (string.IsNullOrWhiteSpace(totpSecret)) 
            throw new ArgumentNullException(nameof(totpSecret));

        try
        {
            var db = _redis.GetDatabase();
            var expiresAt = DateTime.UtcNow.AddSeconds(timeoutSeconds);
            
            var sessionData = new
            {
                UserId = userId,
                TotpSecret = totpSecret,
                CreatedAt = DateTime.UtcNow.ToUniversalTime(),
                ExpiresAt = expiresAt.ToUniversalTime()
            };

            var json = JsonSerializer.Serialize(sessionData);
            var key = SessionPrefix + sessionId;
            
            var result = await db.StringSetAsync(
                key, 
                json, 
                TimeSpan.FromSeconds(timeoutSeconds),
                when: When.NotExists);

            _logger.LogInformation(
                "2FA session created: {SessionId}, expires in {TimeoutSeconds}s", 
                sessionId, 
                timeoutSeconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create 2FA session: {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<TwoFASessionData?> GetSessionAsync(
        string sessionId, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) 
            return null;

        try
        {
            var db = _redis.GetDatabase();
            var key = SessionPrefix + sessionId;
            var value = await db.StringGetAsync(key);

            if (!value.HasValue)
            {
                _logger.LogWarning("2FA session not found: {SessionId}", sessionId);
                return null;
            }

            var data = JsonSerializer.Deserialize<TwoFASessionDataInternal>(value.ToString());
            if (data == null)
                return null;

            return new TwoFASessionData(
                UserId: data.UserId,
                TotpSecret: data.TotpSecret,
                CreatedAt: data.CreatedAt,
                ExpiresAt: data.ExpiresAt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get 2FA session: {SessionId}", sessionId);
            return null;
        }
    }

    public async Task<int> GetFailedAttemptsAsync(
        string sessionId, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) 
            return 0;

        try
        {
            var db = _redis.GetDatabase();
            var key = AttemptsPrefix + sessionId;
            var value = await db.StringGetAsync(key);

            return value.HasValue ? int.Parse(value.ToString()) : 0;
        }
        catch
        {
            return 0;
        }
    }

    public async Task<int> IncrementFailedAttemptsAsync(
        string sessionId, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) 
            return 0;

        try
        {
            var db = _redis.GetDatabase();
            var key = AttemptsPrefix + sessionId;
            
            var count = await db.StringIncrementAsync(key);
            await db.KeyExpireAsync(key, TimeSpan.FromHours(1));

            _logger.LogWarning(
                "2FA failed attempt for session {SessionId}, count={Count}", 
                sessionId, 
                count);

            return (int)count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment attempts: {SessionId}", sessionId);
            return 0;
        }
    }

    public async Task<bool> IsSessionLockedAsync(
        string sessionId, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) 
            return false;

        try
        {
            var db = _redis.GetDatabase();
            var lockKey = LockoutPrefix + sessionId;
            return await db.KeyExistsAsync(lockKey);
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> LockSessionAsync(
        string sessionId, 
        int lockDurationSeconds = 300, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) 
            return false;

        try
        {
            var db = _redis.GetDatabase();
            var lockKey = LockoutPrefix + sessionId;
            
            var result = await db.StringSetAsync(
                lockKey,
                "locked",
                TimeSpan.FromSeconds(lockDurationSeconds));

            _logger.LogWarning(
                "2FA session locked: {SessionId}, duration={DurationSeconds}s", 
                sessionId, 
                lockDurationSeconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to lock session: {SessionId}", sessionId);
            return false;
        }
    }

    public async Task<bool> DeleteSessionAsync(
        string sessionId, 
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) 
            return false;

        try
        {
            var db = _redis.GetDatabase();
            var sessionKey = (RedisKey)(SessionPrefix + sessionId);
            var attemptsKey = (RedisKey)(AttemptsPrefix + sessionId);
            var lockKey = (RedisKey)(LockoutPrefix + sessionId);

            var deleted = await db.KeyDeleteAsync(new[] { sessionKey, attemptsKey, lockKey });

            _logger.LogInformation("2FA session deleted: {SessionId}", sessionId);
            return deleted > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete session: {SessionId}", sessionId);
            return false;
        }
    }

    // Internal class for JSON deserialization
    private class TwoFASessionDataInternal
    {
        public string UserId { get; set; } = string.Empty;
        public string TotpSecret { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
    }
}
```

---

## 🎯 FAZA 3: UPDATE USER AUTH SERVICE (3 GODZINY)

### Plik 4: Zmień `UserAuthService.cs` - RegisterInitialAsync

```csharp
// Zamień całą RegisterInitialAsync tą wersją:

public async Task<UserRegistrationInitialResponse> RegisterInitialAsync(
    string username,
    string email,
    string firstName,
    string lastName,
    string password,
    string baseCurrency,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(username))
        throw new ArgumentException("Username is required", nameof(username));
    if (string.IsNullOrWhiteSpace(email))
        throw new ArgumentException("Email is required", nameof(email));
    if (string.IsNullOrWhiteSpace(password))
        throw new ArgumentException("Password is required", nameof(password));

    try
    {
        var registerRequest = new RegisterRequest(
            UserName: username,
            Email: email,
            FirstName: firstName,
            LastName: lastName,
            Password: password,
            BaseCurrency: baseCurrency);

        var validationResult = await _registerValidator.ValidateAsync(registerRequest, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errorMessage = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.LogWarning("Registration validation failed: {Error}", errorMessage);
            throw new ArgumentException(errorMessage);
        }

        var existingByUserName = await _userRepository.GetByUserNameAsync(username, cancellationToken);
        if (existingByUserName is not null)
        {
            _logger.LogWarning("Username already taken: {Username}", username);
            throw new InvalidOperationException("Username is already taken");
        }

        var existingByEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (existingByEmail is not null)
        {
            _logger.LogWarning("Email already registered: {Email}", email);
            throw new InvalidOperationException("Email is already registered");
        }

        // Generate TOTP secret
        var secretDto = _twoFactorService.GenerateSecret();
        var backupCodes = _twoFactorService.GenerateBackupCodes();
        var sessionId = Guid.NewGuid().ToString();

        // ✅ STORE SECRET IN REDIS, NOT IN JWT
        await _redisSessionService.CreateSessionAsync(
            sessionId,
            userId: Guid.NewGuid().ToString(),
            totpSecret: secretDto.Secret,
            timeoutSeconds: 600, // 10 minutes
            ct: cancellationToken);

        // Create temp token WITHOUT secret, password, backup codes
        var tempUser = new User(
            Id: Guid.NewGuid(),
            UserName: username.Trim(),
            Email: email.Trim(),
            FirstName: firstName.Trim(),
            LastName: lastName.Trim(),
            Role: UserRole.User,
            EmailConfirmed: false,
            TwoFactorEnabled: false,
            TwoFactorSecret: string.Empty,
            BackupCodes: string.Empty,
            Status: UserStatus.Active,
            BaseCurrency: baseCurrency.ToUpper(),
            CreatedAtUtc: DateTimeOffset.UtcNow);

        var tempToken = _jwtTokenGenerator.GenerateToken(
            tempUser,
            isTempToken: true,
            context: new TokenContext 
            { 
                SessionId = sessionId,
                TwoFactorRequired = true
                // ❌ NO totp_secret, password, backup_codes
            });

        _logger.LogInformation("Registration STEP 1 completed for user '{Username}'", username);

        return new UserRegistrationInitialResponse(
            Token: tempToken,
            SessionId: sessionId,
            QrCodeDataUrl: secretDto.QrCodeDataUrl,
            ManualKey: secretDto.Secret,
            BackupCodes: backupCodes.ToList(),
            Message: "Scan QR code with Google Authenticator. Save backup codes in SAFE PLACE!");
    }
    catch (ArgumentException)
    {
        throw;
    }
    catch (InvalidOperationException)
    {
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Registration STEP 1 error");
        throw new InvalidOperationException("Registration failed", ex);
    }
}
```

### Plik 4: Zmień `UserAuthService.cs` - RegisterCompleteInternalAsync

```csharp
// Zamień całą RegisterCompleteInternalAsync tą wersją:

public async Task<UserRegistrationCompleteResponse> RegisterCompleteInternalAsync(
    Guid userId,
    string username,
    string email,
    string firstName,
    string lastName,
    string baseCurrency,
    string code,
    string sessionId, // ← NEW PARAM
    List<string> backupCodes,
    string password,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(sessionId))
        throw new ArgumentException("Session ID is required", nameof(sessionId));
    if (string.IsNullOrWhiteSpace(code))
        throw new ArgumentException("Code is required", nameof(code));
    if (string.IsNullOrWhiteSpace(password))
        throw new ArgumentException("Password is required", nameof(password));

    try
    {
        // ✅ Get TOTP secret from Redis, NOT from JWT!
        var sessionData = await _redisSessionService.GetSessionAsync(sessionId, cancellationToken);
        if (sessionData == null)
        {
            _logger.LogWarning("Registration: Session not found or expired: {SessionId}", sessionId);
            throw new UnauthorizedAccessException("Session expired. Please start registration again.");
        }

        // Check rate limiting
        var failedAttempts = await _redisSessionService.GetFailedAttemptsAsync(sessionId, cancellationToken);
        if (failedAttempts >= 5)
        {
            var isLocked = await _redisSessionService.IsSessionLockedAsync(sessionId, cancellationToken);
            if (!isLocked)
            {
                await _redisSessionService.LockSessionAsync(sessionId, 300, cancellationToken); // 5 min lockout
            }
            _logger.LogWarning("Registration: Too many failed attempts for session {SessionId}", sessionId);
            throw new InvalidOperationException("Too many failed attempts. Try again in 5 minutes.");
        }

        // Verify 2FA code
        if (!_twoFactorService.VerifyCode(sessionData.TotpSecret, code))
        {
            await _redisSessionService.IncrementFailedAttemptsAsync(sessionId, cancellationToken);
            _logger.LogWarning("Registration: Invalid 2FA code for session {SessionId}", sessionId);
            throw new UnauthorizedAccessException("Invalid 2FA code. Please try again.");
        }

        _logger.LogInformation("Registration: 2FA code verified, creating user '{Username}'", username);

        // Encrypt TOTP secret
        var encryptedSecret = _encryptionService.Encrypt(sessionData.TotpSecret);

        // Hash backup codes
        var hashedBackupCodes = backupCodes
            .Select(code => _twoFactorService.HashBackupCode(code))
            .ToList();
        var hashedBackupCodesJson = System.Text.Json.JsonSerializer.Serialize(hashedBackupCodes);

        // Create user
        var user = new User(
            Id: userId,
            UserName: username.Trim(),
            Email: email.Trim(),
            FirstName: firstName.Trim(),
            LastName: lastName.Trim(),
            Role: UserRole.User,
            EmailConfirmed: false,
            TwoFactorEnabled: true, // ✅ Enabled after verification
            TwoFactorSecret: encryptedSecret,
            BackupCodes: hashedBackupCodesJson,
            Status: UserStatus.Active,
            BaseCurrency: baseCurrency.ToUpper(),
            CreatedAtUtc: DateTimeOffset.UtcNow);

        var passwordHash = _passwordHasher.HashPassword(user, password);
        
        await _userRepository.AddAsync(user, passwordHash, cancellationToken);
        await _userRepository.SaveChangesAsync(cancellationToken);

        // Create main account
        await _accountService.CreateMainAccountAsync(
            user.Id,
            baseCurrency.ToUpper(),
            initialBalance: 10000,
            cancellationToken);

        // ✅ Clean up Redis session
        await _redisSessionService.DeleteSessionAsync(sessionId, cancellationToken);

        var finalToken = _jwtTokenGenerator.GenerateToken(user, isTempToken: false);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds();

        _logger.LogInformation("User '{UserId}' registered with 2FA enabled", user.Id);

        return new UserRegistrationCompleteResponse(
            Token: finalToken,
            UserId: user.Id,
            Username: user.UserName,
            Email: user.Email,
            ExpiresAt: expiresAt,
            Message: "✅ Registration complete! 2FA is enabled.",
            BackupCodes: backupCodes); // ← Return codes only this once
    }
    catch (UnauthorizedAccessException)
    {
        throw;
    }
    catch (Exception ex)
    {
        // Cleanup on error
        await _redisSessionService.DeleteSessionAsync(sessionId, cancellationToken);
        _logger.LogError(ex, "Registration STEP 2 error");
        throw new InvalidOperationException("Registration failed", ex);
    }
}
```

### Plik 4: Zmień `UserAuthService.cs` - LoginInitialAsync

```csharp
public async Task<UserLoginInitialResponse> LoginInitialAsync(
    string userNameOrEmail,
    string password,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(userNameOrEmail))
        throw new ArgumentException("Username or email is required", nameof(userNameOrEmail));
    if (string.IsNullOrWhiteSpace(password))
        throw new ArgumentException("Password is required", nameof(password));

    try
    {
        var (user, hashedPassword) = await _userRepository.GetByUserNameOrEmailWithPasswordHashAsync(
            userNameOrEmail, cancellationToken);

        if (user == null || string.IsNullOrWhiteSpace(hashedPassword))
        {
            _logger.LogWarning("Login failed: invalid credentials for '{UserNameOrEmail}'", userNameOrEmail);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        var verifyResult = _passwordHasher.VerifyHashedPassword(user, hashedPassword, password);
        if (verifyResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Login failed: invalid password for user '{UserId}'", user.Id);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (user.Status != UserStatus.Active)
        {
            _logger.LogWarning("Login failed: user '{UserId}' is not active", user.Id);
            throw new UnauthorizedAccessException("User account is not active");
        }

        // Check 2FA status
        if (user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(user.TwoFactorSecret))
        {
            var sessionId = Guid.NewGuid().ToString();

            // ✅ NEW: Decrypt TOTP secret and store in Redis
            var decryptedSecret = _encryptionService.Decrypt(user.TwoFactorSecret);
            
            await _redisSessionService.CreateSessionAsync(
                sessionId,
                userId: user.Id.ToString(),
                totpSecret: decryptedSecret,
                timeoutSeconds: 600, // 10 minutes
                ct: cancellationToken);

            var tempToken = _jwtTokenGenerator.GenerateToken(
                user,
                isTempToken: true,
                context: new TokenContext
                {
                    SessionId = sessionId,
                    TwoFactorRequired = true
                    // ❌ NO totp_secret
                });

            _logger.LogInformation("Login STEP 1: 2FA required for user '{UserId}'", user.Id);

            return new UserLoginInitialResponse(
                Token: tempToken,
                SessionId: sessionId,
                RequiresTwoFactor: true,
                Username: user.UserName);
        }
        else
        {
            // No 2FA
            var finalToken = _jwtTokenGenerator.GenerateToken(user, isTempToken: false);

            _logger.LogInformation("Login successful for user '{UserId}' (no 2FA)", user.Id);

            return new UserLoginInitialResponse(
                Token: finalToken,
                SessionId: Guid.NewGuid().ToString(),
                RequiresTwoFactor: false,
                Username: user.UserName);
        }
    }
    catch (UnauthorizedAccessException)
    {
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Login error for user '{UserNameOrEmail}'", userNameOrEmail);
        throw new InvalidOperationException("Login failed", ex);
    }
}
```

### Plik 4: Zmień `UserAuthService.cs` - VerifyUserTwoFactorInternalAsync

```csharp
public async Task<UserAuthCompleteResponse> VerifyUserTwoFactorInternalAsync(
    Guid userId,
    string sessionId, // ← ADD THIS PARAM
    string code,
    CancellationToken cancellationToken = default)
{
    if (string.IsNullOrWhiteSpace(sessionId))
        throw new ArgumentException("Session ID is required", nameof(sessionId));
    if (string.IsNullOrWhiteSpace(code))
        throw new ArgumentException("Code is required", nameof(code));

    try
    {
        // ✅ Get session from Redis
        var sessionData = await _redisSessionService.GetSessionAsync(sessionId, cancellationToken);
        if (sessionData == null)
        {
            _logger.LogWarning("Login STEP 2: Session not found for user '{UserId}'", userId);
            throw new UnauthorizedAccessException("Session expired. Please login again.");
        }

        // Check rate limiting
        var failedAttempts = await _redisSessionService.GetFailedAttemptsAsync(sessionId, cancellationToken);
        if (failedAttempts >= 5)
        {
            var isLocked = await _redisSessionService.IsSessionLockedAsync(sessionId, cancellationToken);
            if (!isLocked)
            {
                await _redisSessionService.LockSessionAsync(sessionId, 300, cancellationToken);
            }
            _logger.LogWarning("Login STEP 2: Session locked due to too many failed attempts: {SessionId}", sessionId);
            throw new InvalidOperationException("Too many failed attempts. Session locked for 5 minutes.");
        }

        // Get user
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            _logger.LogWarning("Login STEP 2: User not found: {UserId}", userId);
            throw new UnauthorizedAccessException("User not found");
        }

        // Verify code
        if (!_twoFactorService.VerifyCode(sessionData.TotpSecret, code))
        {
            await _redisSessionService.IncrementFailedAttemptsAsync(sessionId, cancellationToken);
            _logger.LogWarning("Login STEP 2: Invalid 2FA code for user '{UserId}'", userId);
            throw new UnauthorizedAccessException("Invalid 2FA code");
        }

        // ✅ Success! Clean up and issue final token
        await _redisSessionService.DeleteSessionAsync(sessionId, cancellationToken);

        var finalToken = _jwtTokenGenerator.GenerateToken(user, isTempToken: false);
        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds();

        _logger.LogInformation("User '{UserId}' authenticated with 2FA", user.Id);

        return new UserAuthCompleteResponse(
            Token: finalToken,
            UserId: user.Id,
            Username: user.UserName,
            ExpiresAt: expiresAt,
            Role: user.Role.ToString());
    }
    catch (UnauthorizedAccessException)
    {
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Login STEP 2 error for user '{UserId}'", userId);
        throw new InvalidOperationException("2FA verification failed", ex);
    }
}
```

---

## 🎯 FAZA 4: PROGRAM.CS - REGISTER SERVICES

```csharp
// Dodaj w Program.cs:

// Install Redis
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var redisConnection = builder.Configuration.GetConnectionString("Redis") 
        ?? throw new InvalidOperationException("Redis connection string is required in appsettings.json");
    
    return ConnectionMultiplexer.Connect(redisConnection);
});

// Register IRedisSessionService
builder.Services.AddScoped<IRedisSessionService, RedisSessionService>();
```

---

## 🎯 FAZA 5: APPSETTINGS - ADD REDIS CONNECTION

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "...",
    "Redis": "localhost:6379"
  },
  "TwoFactorAuth": {
    "MaxFailedAttempts": 5,
    "LockoutDurationMinutes": 5,
    "SessionTimeoutMinutes": 10
  }
}
```

---

## ✅ CHECKLIST DO SKOPIOWANIA

```markdown
# 2FA Security Fix - Implementation Checklist

## PHASE 1: JWT Changes (30 min)
- [ ] Modify JwtTokenGenerator.cs - remove totp_secret claim
- [ ] Verify no password in JWT claims
- [ ] Verify no backup_codes in JWT claims
- [ ] Test: JWT should only have {userId, sessionId, requires_2fa}

## PHASE 2: Redis Service (45 min)
- [ ] Create IRedisSessionService.cs interface
- [ ] Create RedisSessionService.cs implementation
- [ ] Add using statements for StackExchange.Redis
- [ ] Verify Redis connection works locally

## PHASE 3: UserAuthService Updates (90 min)
- [ ] Update RegisterInitialAsync to use Redis
- [ ] Update RegisterCompleteInternalAsync with rate limiting
- [ ] Update LoginInitialAsync to store secret in Redis
- [ ] Update VerifyUserTwoFactorInternalAsync with rate limiting
- [ ] Add sessionId parameter where needed
- [ ] Test: Full registration flow with Redis

## PHASE 4: DI Configuration (15 min)
- [ ] Register IRedisSessionService in Program.cs
- [ ] Register IConnectionMultiplexer in Program.cs
- [ ] Update appsettings.json with Redis connection string

## PHASE 5: Testing (60 min)
- [ ] Unit test: Redis session CRUD
- [ ] Unit test: Rate limiting logic
- [ ] Integration test: Full registration flow
- [ ] Integration test: Full login + 2FA flow
- [ ] Test: Session cleanup on success/failure
- [ ] Test: Lockout after 5 failed attempts

## PHASE 6: Documentation (30 min)
- [ ] Document Redis setup steps
- [ ] Document session timeout values
- [ ] Add comments explaining rate limiting
- [ ] Update API documentation

## PHASE 7: Deployment (30 min)
- [ ] Deploy Redis to staging
- [ ] Deploy updated API code
- [ ] Monitor Redis connection
- [ ] Monitor login success rate
- [ ] Monitor 2FA failure rate

## POST-DEPLOYMENT
- [ ] Monitor alerts for suspicious activity
- [ ] Check Redis memory usage
- [ ] Verify no sensitive data in logs
- [ ] Get security review approval
```

---

## 🎓 Lekcja dla przyszłości

**NIGDY** nie umieszczaj w JWT:
- ❌ TOTP/2FA secrets
- ❌ Hasła użytkownika
- ❌ Backup codes
- ❌ Session state info

**Zawsze** przechowuj na serwerze:
- ✅ Session state (Redis/cache)
- ✅ TOTP secrets (encrypted w DB/Redis)
- ✅ Backup codes (hashed w DB)
- ✅ Failed attempt counters

---

**Wszystko gotowe do wdrażania! 🚀**
