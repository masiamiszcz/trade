
namespace TradingPlatform.Core.Models;

/// <summary>
/// Request to register admin via invitation token
/// Used after admin receives email with invitation link
/// </summary>
public sealed record AdminRegisterViaInviteRequest(
    /// <summary>32-char token from invitation email (from query param: ?token=ABC123...)</summary>
    string Token,
    /// <summary>Desired username (must be unique, case-insensitive)</summary>
    string Username,
    /// <summary>Password (must meet security requirements)</summary>
    string Password
);

/// <summary>
/// Request to log in as admin
/// Requires username/password, but 2FA is MANDATORY
/// </summary>
public sealed record AdminLoginRequest(
    /// <summary>Username or email address</summary>
    string UsernameOrEmail,
    /// <summary>Password (will be verified against hash)</summary>
    string Password
);

/// <summary>
/// Request to verify 2FA code during login
/// </summary>
public sealed record AdminVerifyTwoFactorRequest(
    /// <summary>Session ID from previous login response (identifies the temp token)</summary>
    string SessionId,
    /// <summary>6-digit code from Google Authenticator or backup code</summary>
    string Code
);

/// <summary>
/// Request to set up 2FA for admin account
/// Used during registration (mandatory) and optionally for existing admins
/// </summary>
public sealed record AdminSetupTwoFactorRequest(
    /// <summary>6-digit code from Google Authenticator (proves possession)</summary>
    string Code
);

/// <summary>
/// Request to disable 2FA for admin account
/// Requires verification of current 2FA code (security check)
/// </summary>
public sealed record AdminDisableTwoFactorRequest(
    /// <summary>Current 6-digit TOTP code (must be valid)</summary>
    string Code
);

/// <summary>
/// Request to regenerate backup codes
/// Requires verification of current 2FA code
/// </summary>
public sealed record AdminRegenerateBackupCodesRequest(
    /// <summary>Current 6-digit TOTP code (must be valid)</summary>
    string Code
);

/// <summary>
/// Super Admin request to invite a new admin
/// </summary>
public sealed record AdminInviteRequest(
    /// <summary>Email address of new admin</summary>
    string Email,
    /// <summary>First name for admin account</summary>
    string FirstName,
    /// <summary>Last name for admin account</summary>
    string LastName,
    /// <summary>Optional: JSON array of permissions ["ManageInstruments", "ViewAuditLogs"]</summary>
    string[]? Permissions = null
);
