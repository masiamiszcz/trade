namespace TradingPlatform.Core.Dtos;

/// <summary>
/// Response after user provides initial registration data (username, email, password)
/// 2FA setup is MANDATORY for user registration
/// Frontend must show QR code and code input form
/// </summary>
public sealed record UserRegistrationInitialResponse(
    /// <summary>Temporary session token (5 min expiry, ONLY for 2FA verification)</summary>
    string Token,
    /// <summary>Session identifier (must be sent with 2FA code for verify)</summary>
    string SessionId,
    /// <summary>QR code as base64 PNG image (data:image/png;base64,...)</summary>
    string QrCodeDataUrl,
    /// <summary>Manual entry key in Base32 format (backup if can't scan)</summary>
    string ManualKey,
    /// <summary>8 backup codes for account recovery</summary>
    List<string> BackupCodes,
    /// <summary>User-friendly message</summary>
    string Message = "Zeskanuj kod QR z aplikacją Authenticator. 2FA jest wymagane do ukończenia rejestracji."
);

/// <summary>
/// Response after user login (first step - password only)
/// If user has 2FA enabled: returns temp token + requires 2FA verification
/// If user doesn't have 2FA: returns final token (legacy support)
/// </summary>
public sealed record UserLoginInitialResponse(
    /// <summary>JWT token (5 min if 2FA required, 60 min if 2FA disabled)</summary>
    string Token,
    /// <summary>Session identifier (required for 2FA verification)</summary>
    string SessionId,
    /// <summary>Flag indicating if 2FA verification is required next</summary>
    bool RequiresTwoFactor,
    /// <summary>User username for display</summary>
    string Username
);

/// <summary>
/// Response after successful 2FA code verification during registration
/// User account is NOW CREATED in the database
/// 2FA is ENABLED for this user
/// </summary>
public sealed record UserRegistrationCompleteResponse(
    /// <summary>JWT token (60 min expiry) - user is now fully authenticated</summary>
    string Token,
    /// <summary>Newly created user ID</summary>
    Guid UserId,
    /// <summary>Username</summary>
    string Username,
    /// <summary>User email</summary>
    string Email,
    /// <summary>Token expiry time (Unix timestamp)</summary>
    long ExpiresAt,
    /// <summary>User-friendly success message</summary>
    string Message = "2FA verified! Your account is created and 2FA is enabled.",
    /// <summary>Backup codes for account recovery (displayed only once to user)</summary>
    List<string>? BackupCodes = null
);

/// <summary>
/// Response after successful 2FA code verification during login
/// User is now fully authenticated with final token
/// </summary>
public sealed record UserAuthCompleteResponse(
    /// <summary>JWT token (60 min expiry)</summary>
    string Token,
    /// <summary>User ID</summary>
    Guid UserId,
    /// <summary>Username</summary>
    string Username,
    /// <summary>Token expiry time (Unix timestamp)</summary>
    long ExpiresAt,
    /// <summary>User role</summary>
    string Role = "User"
);

/// <summary>
/// Response when user requests 2FA setup (after successful login)
/// OPTIONAL - user can enable 2FA on their account
/// </summary>
public sealed record UserTwoFactorSetupResponse(
    /// <summary>QR code as base64 PNG</summary>
    string QrCodeDataUrl,
    /// <summary>Manual entry key (Base32)</summary>
    string ManualKey,
    /// <summary>Session ID (needed when confirming code)</summary>
    string SessionId,
    /// <summary>8 backup codes (user must save these)</summary>
    List<string> BackupCodes,
    /// <summary>User-friendly instruction</summary>
    string Message = "Skanuj kod QR z aplikacją Authenticator lub wpisz klucz ręcznie"
);

/// <summary>
/// Response after successfully enabling 2FA on user account
/// User MUST save backup codes somewhere safe
/// </summary>
public sealed record UserTwoFactorEnableResponse(
    /// <summary>8 backup codes (user should save these)</summary>
    List<string> BackupCodes,
    /// <summary>Success flag</summary>
    bool Success = true,
    /// <summary>Warning message about backup codes</summary>
    string Message = "✅ 2FA enabled! Zapisz te kody w bezpiecznym miejscu. Będą potrzebne jeśli stracisz dostęp do aplikacji."
);

/// <summary>
/// Response when user disables 2FA on their account
/// </summary>
public sealed record UserTwoFactorDisableResponse(
    /// <summary>Success flag</summary>
    bool Success,
    /// <summary>Message</summary>
    string Message = "2FA has been disabled. You can enable it again anytime."
);

/// <summary>
/// Response for 2FA status check
/// </summary>
public sealed record UserTwoFactorStatusResponse(
    /// <summary>Whether 2FA is enabled for this user</summary>
    bool TwoFactorEnabled,
    /// <summary>Number of remaining backup codes (if 2FA enabled)</summary>
    int? RemainingBackupCodes = null
);
