namespace TradingPlatform.Core.Models;

/// <summary>
/// Request to verify 2FA code during user registration or login
/// Frontend must include the temporary token received from register/login endpoint
/// </summary>
public sealed record UserVerifyTwoFactorRequest(
    /// <summary>Temporary JWT token from RegisterInitialAsync or LoginInitialAsync</summary>
    /// <remarks>Contains userId, sessionId, and TOTP secret in claims</remarks>
    string TempToken,
    /// <summary>6-digit TOTP code from authenticator app OR backup code</summary>
    string Code);

/// <summary>
/// Request to enable/disable 2FA (for future user-initiated 2FA setup)
/// </summary>
public sealed record UserSetupTwoFactorRequest(
    /// <summary>Session ID from 2FA setup request</summary>
    string SessionId,
    /// <summary>6-digit code to verify</summary>
    string Code);

/// <summary>
/// Request to disable 2FA (requires current 2FA code as security check)
/// </summary>
public sealed record UserDisableTwoFactorRequest(
    /// <summary>Current 2FA code to verify identity</summary>
    string Code);
