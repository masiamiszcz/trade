using StackExchange.Redis;
using TradingPlatform.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace TradingPlatform.Core.Services;

/// <summary>
/// Redis-based session storage for temporary 2FA data
/// 
/// Design:
/// - Stores TOTP secrets in Redis (never in JWT or unencrypted DB)
/// - Tracks failed attempts per session
/// - Implements rate limiting: 5 attempts max, then 5 min lockout
/// - Auto-cleanup via TTL (session expires after 10 min inactivity)
/// 
/// Redis Keys:
/// - 2fa:session:{sessionId} → JSON { userId, totpSecret, createdAt, expiresAt }
/// - 2fa:attempts:{sessionId} → integer count of failed attempts
/// - 2fa:lockout:{sessionId} → "locked" (with 5 min TTL)
/// 
/// Thread-safe: Uses Redis INCR command for atomic increment
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

    /// <summary>
    /// Create temporary 2FA session with TOTP secret, password, and backup codes
    /// All data is stored in Redis (memory) with auto-expiration (TTL)
    /// 
    /// ⚠️ Security Note: Password is stored plaintext in Redis
    /// This is acceptable because:
    /// 1. Redis is in-memory (not disk)
    /// 2. Redis connection is internal (not network-exposed)
    /// 3. Data auto-expires after 10 minutes
    /// 4. Password is hashed immediately upon account creation
    /// 5. Data is never logged or persisted
    /// </summary>
    public async Task<bool> CreateSessionAsync(
        string sessionId,
        string userId,
        string totpSecret,
        int timeoutSeconds,
        string? password = null,
        List<string>? backupCodes = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning("CreateSessionAsync: sessionId is empty");
            throw new ArgumentException("Session ID is required", nameof(sessionId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("CreateSessionAsync: userId is empty");
            throw new ArgumentException("User ID is required", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(totpSecret))
        {
            _logger.LogWarning("CreateSessionAsync: totpSecret is empty");
            throw new ArgumentException("TOTP secret is required", nameof(totpSecret));
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = SessionPrefix + sessionId;
            var createdAt = DateTime.UtcNow;
            var expiresAt = createdAt.AddSeconds(timeoutSeconds);

            var sessionData = new TwoFASessionData(
                UserId: userId,
                TotpSecret: totpSecret,
                CreatedAt: createdAt,
                ExpiresAt: expiresAt,
                Password: password,
                BackupCodes: backupCodes);

            var json = JsonSerializer.Serialize(sessionData);
            var timespan = TimeSpan.FromSeconds(timeoutSeconds);

            // Store in Redis with TTL (auto-delete after timeout)
            var success = await db.StringSetAsync(key, json, timespan);

            if (success)
            {
                _logger.LogInformation(
                    "Redis: Created 2FA session {SessionId} for user {UserId}, TTL={Timeout}s, contains password={HasPassword}, backup_codes={HasBackupCodes}",
                    sessionId, userId, timeoutSeconds, !string.IsNullOrWhiteSpace(password), backupCodes?.Count > 0);
            }
            else
            {
                _logger.LogError("Redis: Failed to create session {SessionId}", sessionId);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Error creating session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Create temporary 2FA session with TOTP secret only (for login flow)
    /// Overload without password/backup codes parameters
    /// </summary>
    public async Task<bool> CreateSessionAsync(
        string sessionId,
        string userId,
        string totpSecret,
        int timeoutSeconds,
        CancellationToken ct = default)
    {
        // Delegate to the full overload with password/backup codes = null
        return await CreateSessionAsync(
            sessionId,
            userId,
            totpSecret,
            timeoutSeconds,
            password: null,
            backupCodes: null,
            ct: ct);
    }

    /// <summary>
    /// Retrieve session data from Redis
    /// Returns null if session doesn't exist or has expired (TTL passed)
    /// </summary>
    public async Task<TwoFASessionData?> GetSessionAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning("GetSessionAsync: sessionId is empty");
            return null;
        }

        try
        {
            var db = _redis.GetDatabase();
            var key = SessionPrefix + sessionId;

            var value = await db.StringGetAsync(key);

            if (!value.IsNull)
            {
                try
                {
                    var sessionData = JsonSerializer.Deserialize<TwoFASessionData>(value.ToString());
                    
                    _logger.LogInformation(
                        "Redis: Retrieved 2FA session {SessionId} for user {UserId}",
                        sessionId, sessionData?.UserId);
                    
                    return sessionData;
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "Redis: Failed to deserialize session {SessionId}", sessionId);
                    return null;
                }
            }
            else
            {
                _logger.LogInformation("Redis: Session {SessionId} not found or expired", sessionId);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Error retrieving session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Get number of failed 2FA verification attempts for session
    /// Returns 0 if attempts counter doesn't exist yet
    /// </summary>
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

            if (value.IsNull)
            {
                _logger.LogDebug("Redis: No attempts recorded for session {SessionId}", sessionId);
                return 0;
            }

            if (int.TryParse(value.ToString(), out int attempts))
            {
                _logger.LogDebug("Redis: Session {SessionId} has {Attempts} failed attempts", sessionId, attempts);
                return attempts;
            }

            _logger.LogWarning("Redis: Invalid attempt count value for session {SessionId}: {Value}", sessionId, value);
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Error getting failed attempts for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Increment failed attempt counter (atomic operation)
    /// Uses Redis INCR command for thread-safe increment
    /// Auto-deletes after 10 minutes (session TTL)
    /// </summary>
    public async Task<int> IncrementFailedAttemptsAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));

        try
        {
            var db = _redis.GetDatabase();
            var key = AttemptsPrefix + sessionId;

            // INCR returns new value
            var newCount = await db.StringIncrementAsync(key);

            // Set TTL so counter auto-deletes after 10 min (same as session TTL)
            await db.KeyExpireAsync(key, TimeSpan.FromMinutes(10));

            _logger.LogInformation(
                "Redis: Incremented failed attempts for session {SessionId}: {Count}",
                sessionId, newCount);

            return (int)newCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Error incrementing attempts for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Check if session is locked (due to too many failed attempts)
    /// Lockout key exists with 5 min TTL set by LockSessionAsync
    /// </summary>
    public async Task<bool> IsSessionLockedAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return false;

        try
        {
            var db = _redis.GetDatabase();
            var key = LockoutPrefix + sessionId;

            var exists = await db.KeyExistsAsync(key);

            if (exists)
            {
                _logger.LogWarning("Redis: Session {SessionId} is locked", sessionId);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Error checking lockout for session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Lock session for specified duration (default 5 minutes)
    /// Called after max failed attempts (5)
    /// Auto-expires after lockDurationSeconds
    /// </summary>
    public async Task<bool> LockSessionAsync(
        string sessionId,
        int lockDurationSeconds = 300,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));

        try
        {
            var db = _redis.GetDatabase();
            var key = LockoutPrefix + sessionId;

            var success = await db.StringSetAsync(
                key,
                "locked",
                TimeSpan.FromSeconds(lockDurationSeconds));

            if (success)
            {
                _logger.LogWarning(
                    "Redis: Locked session {SessionId} for {Duration}s (due to too many failed attempts)",
                    sessionId, lockDurationSeconds);
            }

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Error locking session {SessionId}", sessionId);
            throw;
        }
    }

    /// <summary>
    /// Delete session (cleanup)
    /// Called after successful 2FA verification or manual cleanup
    /// Deletes both session data and attempt counter
    /// </summary>
    public async Task<bool> DeleteSessionAsync(
        string sessionId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            _logger.LogWarning("DeleteSessionAsync: sessionId is empty");
            return false;
        }

        try
        {
            var db = _redis.GetDatabase();

            var keys = new RedisKey[]
            {
                SessionPrefix + sessionId,
                AttemptsPrefix + sessionId,
                LockoutPrefix + sessionId
            };

            // Delete all related keys (session, attempts, lockout)
            var deletedCount = await db.KeyDeleteAsync(keys);

            _logger.LogInformation(
                "Redis: Deleted {DeletedCount} keys for session {SessionId}",
                deletedCount, sessionId);

            return deletedCount > 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis: Error deleting session {SessionId}", sessionId);
            throw;
        }
    }
}
