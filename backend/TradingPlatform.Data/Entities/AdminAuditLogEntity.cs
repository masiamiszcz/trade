namespace TradingPlatform.Data.Entities;



/// <summary>
/// Audit log for admin actions
/// Tracks every security-relevant action by administrators
/// Used for compliance, security investigation, and monitoring
/// </summary>
public sealed class AdminAuditLogEntity
{
    /// <summary>
    /// Unique identifier for this audit log entry
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Admin who performed the action (FK to Users.Id)
    /// </summary>
    public Guid AdminId { get; set; }

    /// <summary>
    /// Type of action performed
    /// Examples: LoginSuccess, LoginFailed, TwoFactorVerifySuccess
    /// </summary>
    public AdminAuditAction Action { get; set; }

    /// <summary>
    /// IPv4/IPv6 address of the client
    /// Used to detect suspicious activity
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent header from request
    /// Used to identify browser, OS, device type
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// When the action occurred
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional context as JSON
    /// Example for LoginFailed: { "reason": "Invalid password", "attempts": 2 }
    /// Example for TwoFactorVerifySuccess: { "method": "TOTP", "windowUsed": 0 }
    /// Example for BackupCodeUsed: { "codesRemaining": 7 }
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Types of admin actions to audit
/// </summary>
public enum AdminAuditAction
{
    /// <summary>Admin successfully logged in</summary>
    LoginSuccess = 1,
    
    /// <summary>Admin login failed (invalid credentials, 2FA disabled, etc.)</summary>
    LoginFailed = 2,
    
    /// <summary>Admin successfully verified 2FA code</summary>
    TwoFactorVerifySuccess = 3,
    
    /// <summary>Admin entered incorrect 2FA code</summary>
    TwoFactorVerifyFailed = 4,
    
    /// <summary>Admin enabled 2FA on their account</summary>
    TwoFactorEnabled = 5,
    
    /// <summary>Admin disabled 2FA on their account</summary>
    TwoFactorDisabled = 6,
    
    /// <summary>Admin used a backup code to log in</summary>
    BackupCodeUsed = 7,
    
    /// <summary>Admin regenerated backup codes</summary>
    BackupCodesRegenerated = 8,
    
    /// <summary>Admin logged out</summary>
    LogoutSuccess = 9,
    
    /// <summary>Admin performed a sensitive operation</summary>
    SensitiveOperationPerformed = 10,
    
    /// <summary>Suspicious activity detected</summary>
    SuspiciousActivityDetected = 11,
    
    /// <summary>Admin account was locked (too many failed attempts)</summary>
    AccountLocked = 12
}
