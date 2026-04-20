namespace TradingPlatform.Core.Dtos;

// AdminAuthResponses


/// <summary>
/// Response after admin registration via invitation
/// Frontend shows 2FA setup form after this
/// </summary>
public sealed record AdminRegistrationResponse(
    /// <summary>Temporary session token (5 min expiry, for 2FA setup only)</summary>
    string Token,
    /// <summary>Session identifier (unique per registration attempt)</summary>
    string SessionId,
    /// <summary>Flag indicating 2FA setup is required next</summary>
    bool RequiresTwoFactorSetup,
    /// <summary>User-friendly message</summary>
    string Message,
    /// <summary>QR code data URL (data:image/png;base64,...) for Google Authenticator</summary>
    string? QrCodeDataUrl = null,
    /// <summary>Manual TOTP key (Base32 encoded) for manual entry into authenticator</summary>
    string? ManualKey = null,
    /// <summary>Backup codes for account recovery (8 codes)</summary>
    List<string>? BackupCodes = null
);

/// <summary>
/// Response after admin login (before 2FA)
/// Frontend shows 2FA code input form
/// </summary>
public sealed record AdminLoginResponse(
    /// <summary>Temporary session token (5 min expiry, for 2FA verification only)</summary>
    string Token,
    /// <summary>Session identifier (must be sent with 2FA code)</summary>
    string SessionId,
    /// <summary>Flag indicating 2FA verification is required next</summary>
    bool RequiresTwoFactor,
    /// <summary>Admin username (for display)</summary>
    string Username
);

/// <summary>
/// Response after successful 2FA verification
/// Frontend stores this JWT and uses it for API calls
/// </summary>
public sealed record AdminAuthSuccessResponse(
    /// <summary>JWT token with role=Admin claim (60 min expiry)</summary>
    string Token,
    /// <summary>Admin role for authorization</summary>
    string Role,
    /// <summary>Admin ID</summary>
    Guid AdminId,
    /// <summary>Admin username</summary>
    string Username,
    /// <summary>Token expiry time (Unix timestamp)</summary>
    long ExpiresAt
);

/// <summary>
/// Response when generating 2FA QR code
/// Frontend scans QR or manually enters the key
/// </summary>
public sealed record AdminTwoFactorSetupResponse(
    /// <summary>QR code as base64 PNG image (can be used directly in img src)</summary>
    string QrCodeDataUrl,
    /// <summary>Manual entry key in Base32 format (for backup if can't scan)</summary>
    string ManualKey,
    /// <summary>Session ID (needed when confirming code)</summary>
    string SessionId,
    /// <summary>User-friendly instruction</summary>
    string Message
);

/// <summary>
/// Response after successfully enabling/regenerating 2FA
/// Admin MUST save these codes somewhere safe
/// </summary>
public sealed record AdminTwoFactorCompleteResponse(
    /// <summary>8 backup codes (use if authenticator is lost)</summary>
    string[] BackupCodes,
    /// <summary>Flag indicating setup is complete</summary>
    bool Success,
    /// <summary>Warning message about backup codes</summary>
    string Message
);

/// <summary>
/// Response when admin disables 2FA
/// Admin can re-enable anytime
/// </summary>
public sealed record AdminTwoFactorDisableResponse(
    /// <summary>Confirmation that 2FA is now disabled</summary>
    bool Success,
    /// <summary>Warning that admin will need to re-enable before login</summary>
    string Message
);

/// <summary>
/// Response when super admin invites a new admin
/// The token should be sent to admin via email
/// </summary>
public sealed record AdminInvitationResponse(
    /// <summary>32-char invitation token (to be sent in email link)</summary>
    string Token,
    /// <summary>Email address for reference</summary>
    string Email,
    /// <summary>When the token expires (ISO 8601 format)</summary>
    string ExpiresAt,
    /// <summary>Full invitation URL for email</summary>
    string InvitationUrl
);

/// <summary>
/// Standard error response
/// Returned when validation or business logic fails
/// </summary>
public sealed record AdminAuthErrorResponse(
    /// <summary>HTTP status code (400, 401, 403, 404, 409)</summary>
    int StatusCode,
    /// <summary>Error message for user</summary>
    string Message,
    /// <summary>Error code for frontend routing (e.g., "INVALID_TOKEN", "EMAIL_TAKEN")</summary>
    string ErrorCode,
    /// <summary>Additional details (optional)</summary>
    string? Details = null
);

/// <summary>
/// Response for admin API health check
/// </summary>
public sealed record AdminHealthCheckResponse(
    string Status,
    DateTime Timestamp,
    string Message,
    bool IsAuthenticated,
    string ApiVersion
);
