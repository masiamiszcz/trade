using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Api.Controllers;

/// <summary>
/// Request DTOs for User Authentication
/// </summary>
public sealed record UserRegisterInitialRequest(
    string Username,
    string Email,
    string FirstName,
    string LastName,
    string Password,
    string BaseCurrency);

public sealed record UserRegisterComplete2FARequest(
    string SessionId,
    string Code);

public sealed record UserLoginInitialRequest(
    string UserNameOrEmail,
    string Password);

public sealed record UserVerifyLogin2FARequest(
    string SessionId,
    string Code);

public sealed record UserSetup2FAInitialRequest();

public sealed record UserSetup2FAEnableRequest(
    string SessionId,
    string Code);

public sealed record UserDisable2FARequest(
    string Code);

/// <summary>
/// User Authentication Controller
/// Handles mandatory 2FA during registration and optional 2FA during login
/// 
/// Flow:
/// 1. POST /register - Get TOTP secret + QR code (temp token)
/// 2. POST /register/verify-2fa - Verify 2FA code, create user (final token)
/// 3. POST /login - Login (returns final token if 2FA disabled, temp token if enabled)
/// 4. POST /verify-2fa - Verify login 2FA code (final token)
/// 5. POST /2fa-setup - Setup 2FA for authenticated user
/// 6. POST /2fa-enable - Enable 2FA after verification
/// 7. POST /2fa-disable - Disable 2FA
/// 8. GET /2fa-status - Get 2FA status
/// </summary>
[ApiController]
[Route("api/user")]
public sealed class UserAuthController : ControllerBase
{
    private readonly IUserAuthService _userAuthService;

    public UserAuthController(IUserAuthService userAuthService)
    {
        _userAuthService = userAuthService;
    }

    /// <summary>
    /// Step 1 of user registration: Generate TOTP secret + QR code
    /// Returns temporary token for 2FA verification
    /// </summary>
    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterInitial([FromBody] UserRegisterInitialRequest request, CancellationToken cancellationToken)
    {
        var response = await _userAuthService.RegisterInitialAsync(
            request.Username,
            request.Email,
            request.FirstName,
            request.LastName,
            request.Password,
            request.BaseCurrency,
            cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Step 2 of user registration: Verify 2FA code and create user account
    /// User is created in database only after successful 2FA verification
    /// Extracts user data from JWT token claims
    /// </summary>
    [HttpPost("register/verify-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> RegisterComplete2FA([FromBody] UserRegisterComplete2FARequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Extract Bearer token from Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { error = "Authorization token required" });

            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Validate token and extract all claims
            // This happens in JwtTokenGenerator.ValidateTokenAndGetClaims
            var claims = _userAuthService.ValidateTokenAndExtractClaims(token);
            if (claims == null)
                return Unauthorized(new { error = "Invalid or expired token" });

            // Extract required data from claims
            if (!Guid.TryParse(claims.GetValueOrDefault("userId") ?? claims.GetValueOrDefault("sub"), out var userId))
                return BadRequest(new { error = "Invalid user ID in token" });

            var username = claims.GetValueOrDefault("name") ?? claims.GetValueOrDefault("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name");
            var email = claims.GetValueOrDefault("email") ?? claims.GetValueOrDefault("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress");
            var firstName = claims.GetValueOrDefault("given_name") ?? "";
            var lastName = claims.GetValueOrDefault("family_name") ?? "";
            var baseCurrency = claims.GetValueOrDefault("baseCurrency") ?? "PLN";
            var totpSecret = claims.GetValueOrDefault("totp_secret");
            var backupCodesJson = claims.GetValueOrDefault("backup_codes");
            var password = claims.GetValueOrDefault("password");

            if (string.IsNullOrEmpty(username))
                return BadRequest(new { error = "Username missing from token" });
            if (string.IsNullOrEmpty(email))
                return BadRequest(new { error = "Email missing from token" });
            if (string.IsNullOrEmpty(totpSecret))
                return BadRequest(new { error = "TOTP secret missing from token" });
            if (string.IsNullOrEmpty(password))
                return BadRequest(new { error = "Password missing from token" });

            // Parse backup codes from JSON
            var backupCodes = new List<string>();
            if (!string.IsNullOrEmpty(backupCodesJson))
            {
                try
                {
                    backupCodes = System.Text.Json.JsonSerializer.Deserialize<List<string>>(backupCodesJson) ?? new List<string>();
                }
                catch
                {
                    return BadRequest(new { error = "Invalid backup codes in token" });
                }
            }

            // Call service with extracted data
            var response = await _userAuthService.RegisterCompleteInternalAsync(
                userId,
                username,
                email,
                firstName,
                lastName,
                baseCurrency,
                request.Code,
                totpSecret,
                backupCodes,
                password,
                cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Registration verification failed", detail = ex.Message });
        }
    }

    /// <summary>
    /// Step 1 of user login: Authenticate and return token
    /// - If 2FA disabled: returns final token (user logged in)
    /// - If 2FA enabled: returns temporary token (requires step 2)
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> LoginInitial([FromBody] UserLoginInitialRequest request, CancellationToken cancellationToken)
    {
        var response = await _userAuthService.LoginInitialAsync(
            request.UserNameOrEmail,
            request.Password,
            cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Step 2 of user login: Verify 2FA code (only required if user has 2FA enabled)
    /// Returns final token after successful 2FA verification
    /// Extracts userId and totp_secret from JWT temp token claims
    /// </summary>
    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyLogin2FA([FromBody] UserVerifyLogin2FARequest request, CancellationToken cancellationToken)
    {
        try
        {
            // Extract Bearer token from Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
                return Unauthorized(new { error = "Authorization token required" });

            var token = authHeader.Substring("Bearer ".Length).Trim();

            // Validate token and extract all claims
            var claims = _userAuthService.ValidateTokenAndExtractClaims(token);
            if (claims == null)
                return Unauthorized(new { error = "Invalid or expired token" });

            // Extract required data from claims
            if (!Guid.TryParse(claims.GetValueOrDefault("userId") ?? claims.GetValueOrDefault("sub"), out var userId))
                return BadRequest(new { error = "Invalid user ID in token" });

            var totpSecret = claims.GetValueOrDefault("totp_secret");
            if (string.IsNullOrEmpty(totpSecret))
                return BadRequest(new { error = "TOTP secret missing from token - must provide 2FA temp token" });

            // Call service with extracted data
            var response = await _userAuthService.VerifyUserTwoFactorAsync(
                sessionId: request.SessionId,
                code: request.Code,
                cancellationToken: cancellationToken);

            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "2FA verification failed", detail = ex.Message });
        }
    }

    /// <summary>
    /// Initiate 2FA setup for authenticated user
    /// Returns TOTP secret + QR code for scanning with authenticator app
    /// </summary>
    [HttpPost("2fa-setup")]
    [Authorize]
    public async Task<IActionResult> Setup2FAInitial([FromBody] UserSetup2FAInitialRequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User ID not found in token");

        var response = await _userAuthService.Setup2FAGenerateAsync(userId, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Enable 2FA after user verifies the TOTP code
    /// Must be called after user scans QR code and verifies with authenticator app
    /// </summary>
    [HttpPost("2fa-enable")]
    [AllowAnonymous]
    public async Task<IActionResult> Setup2FAEnable([FromBody] UserSetup2FAEnableRequest request, CancellationToken cancellationToken)
    {
        // Extract userId from JWT token if provided, or use request data
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User ID not found in token");

        var response = await _userAuthService.Setup2FAEnableAsync(
            userId,
            request.SessionId,
            request.Code,
            cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Disable 2FA for authenticated user
    /// </summary>
    [HttpPost("2fa-disable")]
    [Authorize]
    public async Task<IActionResult> Disable2FA([FromBody] UserDisable2FARequest request, CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User ID not found in token");

        var response = await _userAuthService.DisableTwoFactorAsync(userId, request.Code, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Get 2FA status for authenticated user
    /// </summary>
    [HttpGet("2fa-status")]
    [Authorize]
    public async Task<IActionResult> Get2FAStatus(CancellationToken cancellationToken)
    {
        var userIdClaim = User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            return Unauthorized("User ID not found in token");

        var response = await _userAuthService.Get2FAStatusAsync(userId, cancellationToken);
        return Ok(response);
    }
}
