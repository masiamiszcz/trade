using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Service for user authentication with mandatory 2FA during registration
/// 2FA is optional but required to complete registration flow
/// 
/// REGISTRATION FLOW:
/// 1. RegisterInitialAsync(data) → returns temp token + QR code (NO USER IN DB YET)
/// 2. RegisterCompleteAsync(code) → verifies 2FA code, CREATES USER in DB, returns final token
/// 
/// LOGIN FLOW:
/// 1. LoginInitialAsync(username, password) → verifies password
///    - If 2FA disabled: returns final token (60 min)
///    - If 2FA enabled: returns temp token (5 min) + sessionId
/// 2. VerifyUserTwoFactorAsync(code) → verifies 2FA code, returns final token
/// </summary>
public interface IUserAuthService
{
    /// <summary>
    /// STEP 1: User provides registration data
    /// Validates input, generates TOTP secret and backup codes
    /// ⚠️ Does NOT create user in database yet!
    /// Returns temp token (5 min) so user can verify 2FA code
    /// </summary>
    Task<UserRegistrationInitialResponse> RegisterInitialAsync(
        string username,
        string email,
        string firstName,
        string lastName,
        string password,
        string baseCurrency,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// STEP 2: User provides 2FA code from authenticator
    /// Verifies 2FA code against the secret
    /// If valid: CREATES USER in database, saves encrypted TOTP secret, creates main account
    /// Returns final JWT token (60 min) - user is now fully registered and authenticated
    /// </summary>
    Task<UserRegistrationCompleteResponse> RegisterCompleteAsync(
        string sessionId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// STEP 2 (Internal variant): User provides 2FA code from authenticator
    /// <summary>
    /// Registration STEP 2 (sessionId variant): Verify 2FA code and create user
    /// Retrieves password + backup codes from Redis using sessionId
    /// ✅ SECURE: Sensitive data never leaves server memory
    /// </summary>
    Task<UserRegistrationCompleteResponse> RegisterCompleteInternalAsync(
        Guid userId,
        string username,
        string email,
        string firstName,
        string lastName,
        string baseCurrency,
        string code,
        string sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Registration STEP 2 (full param variant): Verify 2FA code and create user
    /// Takes all parameters explicitly; used internally by sessionId overload
    /// Called by controller after extracting user data from JWT token claims
    /// Verifies 2FA code against the secret
    /// If valid: CREATES USER in database, saves encrypted TOTP secret, creates main account
    /// Returns final JWT token (60 min) - user is now fully registered and authenticated
    /// </summary>
    Task<UserRegistrationCompleteResponse> RegisterCompleteInternalAsync(
        Guid userId,
        string username,
        string email,
        string firstName,
        string lastName,
        string baseCurrency,
        string code,
        string sessionId,
        List<string> backupCodes,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Login STEP 1: User provides username/email and password
    /// Verifies password against stored hash
    /// 
    /// If user has 2FA enabled:
    ///   Returns temp token (5 min) + sessionId → Frontend shows 2FA code input
    /// 
    /// If user doesn't have 2FA enabled:
    ///   Returns final token (60 min) → User is logged in immediately
    /// 
    /// This method does NOT require temp token - it's public
    /// </summary>
    Task<UserLoginInitialResponse> LoginInitialAsync(
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Login STEP 2: User provides 2FA code from authenticator
    /// Only called if user has 2FA enabled (requires temp token from LoginInitialAsync)
    /// Verifies 2FA code, returns final JWT token (60 min)
    /// </summary>
    Task<UserAuthCompleteResponse> VerifyUserTwoFactorAsync(
        string sessionId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Login STEP 2 (Internal variant for API Controller): User provides 2FA code from authenticator
    /// Called by VerifyLogin2FA controller after extracting userId and totpSecret from JWT temp token claims  
    /// Verifies TOTP code against stored secret, returns final 60-minute JWT token on success
    /// Parameters: userId (Guid), code (string), totpSecret (string), cancellationToken (optional)
    /// FIXME: Ensure totpSecret parameter is included - do not remove!
    /// </summary>
    Task<UserAuthCompleteResponse> VerifyUserTwoFactorInternalAsync(
        Guid userId,
        string code,
        string totpSecret,  // IMPORTANT: This must be string, not CancellationToken!
        CancellationToken cancellationToken = default);


    /// <summary>
    /// OPTIONAL: User wants to enable 2FA after registration
    /// Only available if user is authenticated (has valid JWT token)
    /// Generates QR code, manual key, and backup codes
    /// Does NOT enable 2FA yet - returns temp token for verification
    /// </summary>
    Task<UserTwoFactorSetupResponse> Setup2FAGenerateAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// OPTIONAL: User confirms 2FA setup by providing code
    /// Verifies code against generated secret
    /// If valid: saves encrypted TOTP secret and backup codes to database
    /// 2FA is now ENABLED for this user
    /// </summary>
    Task<UserTwoFactorEnableResponse> Setup2FAEnableAsync(
        Guid userId,
        string sessionId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// OPTIONAL: User wants to disable 2FA
    /// Requires user to provide current 2FA code as security verification
    /// Removes TOTP secret and backup codes from database
    /// User can re-enable anytime
    /// </summary>
    Task<UserTwoFactorDisableResponse> DisableTwoFactorAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if user has 2FA enabled
    /// Returns status and number of remaining backup codes
    /// </summary>
    Task<UserTwoFactorStatusResponse> Get2FAStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate JWT token and extract all claims
    /// Used by controller to extract user data from temp tokens (registration, 2FA)
    /// Returns dictionary of claims or null if token is invalid
    /// </summary>
    Dictionary<string, string>? ValidateTokenAndExtractClaims(string token);
}
