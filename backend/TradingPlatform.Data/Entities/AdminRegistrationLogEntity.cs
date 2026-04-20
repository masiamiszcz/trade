namespace TradingPlatform.Data.Entities;


/// <summary>
/// Audit log for admin registration process
/// Tracks every step of admin account creation and 2FA setup
/// Used for security compliance and troubleshooting
/// </summary>
public sealed class AdminRegistrationLogEntity
{
    /// <summary>
    /// Unique identifier for this log entry
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Reference to the invitation used (FK to AdminInvitations.Id)
    /// </summary>
    public Guid InvitationId { get; set; }

    /// <summary>
    /// ID of the admin created (FK to Users.Id)
    /// Null = registration failed before user creation
    /// </summary>
    public Guid? AdminId { get; set; }

    /// <summary>
    /// Email address involved in registration
    /// (copied from invitation for reference)
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// What happened during registration
    /// Examples: InvitationGenerated, RegistrationStarted, TwoFactorSetupCompleted
    /// </summary>
    public AdminRegistrationAction Action { get; set; }

    /// <summary>
    /// Success or failure of this step
    /// </summary>
    public AdminRegistrationLogStatus Status { get; set; }

    /// <summary>
    /// IPv4/IPv6 address of the client
    /// Used for security auditing
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// User-Agent header from request
    /// Used to identify browser, OS, etc.
    /// </summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Error message if Status = Failed
    /// Examples: "Token expired", "Invalid password", "Code mismatch"
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// When this event occurred
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>
    /// Additional context as JSON
    /// Example: { "attemptNumber": 2, "codeLength": 6 }
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Types of actions during admin registration process
/// </summary>
public enum AdminRegistrationAction
{
    /// <summary>Super Admin generated invitation token</summary>
    InvitationGenerated = 1,
    
    /// <summary>Admin started registration with token</summary>
    RegistrationStarted = 2,
    
    /// <summary>Admin account created in DB</summary>
    RegistrationCompleted = 3,
    
    /// <summary>Admin started 2FA setup</summary>
    TwoFactorSetupStarted = 4,
    
    /// <summary>Admin completed 2FA setup (enabled 2FA)</summary>
    TwoFactorSetupCompleted = 5,
    
    /// <summary>Registration process failed at some step</summary>
    RegistrationFailed = 6,
    
    /// <summary>Invitation token was used</summary>
    InvitationUsed = 7
}

/// <summary>
/// Status of a registration action
/// </summary>
public enum AdminRegistrationLogStatus
{
    Success = 1,
    Failed = 2,
    Pending = 3
}
