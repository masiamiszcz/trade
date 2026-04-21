namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Service for storing temporary 2FA session data in Redis
/// Manages:
/// - TOTP secrets (temporary, during registration/login 2FA flow)
/// - Failed attempt tracking
/// - Session lockout after too many failed attempts
/// 
/// Key Design:
/// - Secrets NEVER stored in JWT
/// - Secrets stored in Redis with short TTL (10 minutes)
/// - Rate limiting: max 5 failed attempts, then 5 minute lockout
/// - Session cleanup after successful or failed verification
/// </summary>
public interface IRedisSessionService
{
    /// <summary>Create temporary 2FA session with TOTP secret</summary>
    /// <param name="sessionId">Unique session identifier</param>
    /// <param name="userId">User ID associated with session</param>
    /// <param name="totpSecret">Unencrypted TOTP secret (generated fresh)</param>
    /// <param name="timeoutSeconds">Session TTL in seconds (typically 600 = 10 min)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if created successfully</returns>
    Task<bool> CreateSessionAsync(
        string sessionId,
        string userId,
        string totpSecret,
        int timeoutSeconds,
        CancellationToken ct = default);

    /// <summary>Create temporary 2FA session with TOTP secret, password, and backup codes</summary>
    /// <param name="sessionId">Unique session identifier</param>
    /// <param name="userId">User ID associated with session</param>
    /// <param name="totpSecret">Unencrypted TOTP secret (generated fresh)</param>
    /// <param name="timeoutSeconds">Session TTL in seconds (typically 600 = 10 min)</param>
    /// <param name="password">User password (plaintext, hashed upon account creation)</param>
    /// <param name="backupCodes">Generated backup codes for 2FA recovery</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if created successfully</returns>
    Task<bool> CreateSessionAsync(
        string sessionId,
        string userId,
        string totpSecret,
        int timeoutSeconds,
        string? password = null,
        List<string>? backupCodes = null,
        CancellationToken ct = default);

    /// <summary>Retrieve session data from Redis</summary>
    /// <param name="sessionId">Session to retrieve</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Session data if exists and not expired, null otherwise</returns>
    Task<TwoFASessionData?> GetSessionAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>Get number of failed 2FA verification attempts for session</summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Current attempt count (0 if session/attempts don't exist)</returns>
    Task<int> GetFailedAttemptsAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>Increment failed attempt counter</summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New attempt count after increment</returns>
    Task<int> IncrementFailedAttemptsAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>Check if session is locked due to too many failed attempts</summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if session is locked</returns>
    Task<bool> IsSessionLockedAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>Lock session for specified duration (after max failed attempts)</summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="lockDurationSeconds">How long to lock (default 300 = 5 min)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if locked successfully</returns>
    Task<bool> LockSessionAsync(
        string sessionId,
        int lockDurationSeconds = 300,
        CancellationToken ct = default);

    /// <summary>Delete session (cleanup after verification success/failure)</summary>
    /// <param name="sessionId">Session ID</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if deleted successfully</returns>
    Task<bool> DeleteSessionAsync(
        string sessionId,
        CancellationToken ct = default);

    /// <summary>Increment login attempt counter (rate limiting, per IP)</summary>
    /// <param name="key">Counter key (e.g., "auth:login:attempts:{ipAddress}")</param>
    /// <param name="ttlSeconds">TTL for counter (5 min = 300, 10 min = 600)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>New counter value after increment</returns>
    Task<int> IncrementCounterAsync(
        string key,
        int ttlSeconds = 600,
        CancellationToken ct = default);

    /// <summary>Get counter value (for rate limit checks)</summary>
    /// <param name="key">Counter key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Current counter value, 0 if key doesn't exist</returns>
    Task<int> GetCounterAsync(
        string key,
        CancellationToken ct = default);

    /// <summary>Reset counter to 0 (on successful login)</summary>
    /// <param name="key">Counter key</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>True if reset successfully</returns>
    Task<bool> ResetCounterAsync(
        string key,
        CancellationToken ct = default);
}

/// <summary>
/// Data stored in Redis for a single 2FA session
/// Used during registration step 2 and login step 2 (2FA verification)
/// 
/// ⚠️ NOTE: Contains plaintext password for Step 2 account creation
/// This is secure because:
/// 1. Stored ONLY in Redis (memory)
/// 2. Password is hashed immediately upon account creation
/// 3. Session expires after 10 minutes
/// 4. Not persisted to disk
/// </summary>
public sealed record TwoFASessionData(
    string UserId,
    string TotpSecret,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string? Password = null,
    List<string>? BackupCodes = null);
