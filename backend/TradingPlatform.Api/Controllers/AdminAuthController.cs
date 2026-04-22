
// AdminAuthController
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Services;

namespace TradingPlatform.Api.Controllers;


/// <summary>
/// Admin authentication and 2FA management
/// IMPORTANT: All admins MUST use 2FA (Google Authenticator)
/// 2FA is enforced at login - admins cannot proceed without it
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AdminAuthController : ControllerBase
{
    private readonly IAdminAuthService _authService;
    private readonly ILogger<AdminAuthController> _logger;
    private readonly JwtSettings _jwtSettings;

    public AdminAuthController(
        IAdminAuthService authService, 
        ILogger<AdminAuthController> logger,
        IOptions<JwtSettings> jwtSettings)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _jwtSettings = jwtSettings?.Value ?? throw new ArgumentNullException(nameof(jwtSettings));
    }

    /// <summary>
    /// Generate 2FA QR code for admin setup
    /// GET /api/auth/admin/setup-2fa/generate
    /// 
    /// Called during admin registration (after successful bootstrap or invite registration).
    /// Returns QR code and manual entry key for Google Authenticator setup.
    /// The returned QR code should be displayed to admin in web UI.
    /// Admin scans QR with authenticator app, then calls enable-2fa endpoint with code.
    /// 
    /// Authorization: Temporary JWT token (from bootstrap or registration response)
    /// 
    /// Response (200):
    /// {
    ///   "qrCodeDataUrl": "data:image/png;base64,...",
    ///   "manualKey": "JBSWY3DPEBLW64TMMQ5A",
    ///   "sessionId": "session-123",
    ///   "message": "Scan QR code with Google Authenticator or similar app"
    /// }
    /// </summary>
    [HttpGet("admin/setup-2fa/generate")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdminTwoFactorSetupResponse>> GenerateTwoFactorSetup(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("=== GenerateTwoFactorSetup: START ===");
            
            // Extract Authorization header
            var authHeader = Request.Headers["Authorization"].ToString();
            _logger.LogInformation("Auth Header Present: {Present}", !string.IsNullOrEmpty(authHeader));
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("❌ No Bearer token in header");
                return Unauthorized(new AdminAuthErrorResponse(401, "Missing or invalid Authorization header. Expected: Bearer <token>", "MISSING_TOKEN"));
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            _logger.LogInformation("Token length: {Length}", token.Length);

            // Manually validate JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            tokenHandler.MapInboundClaims = false;  // ✅ Keep original claim names (sub, registration_step)
            var key = Encoding.UTF8.GetBytes(_jwtSettings.Key);

            try
            {
                var principal = tokenHandler.ValidateToken(token,
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ValidateIssuer = true,
                        ValidIssuer = _jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = _jwtSettings.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    }, out SecurityToken validatedToken);

                _logger.LogInformation("✅ Token validated successfully");

                // Extract admin ID from 'sub' claim
                var adminIdClaim = principal.FindFirst("sub");
                if (adminIdClaim == null)
                {
                    _logger.LogWarning("❌ Missing 'sub' claim in JWT");
                    // Log all claims for debugging
                    var allClaims = string.Join(", ", principal.Claims.Select(c => $"{c.Type}={c.Value}"));
                    _logger.LogWarning("Available claims: {Claims}", allClaims);
                    return Unauthorized(new AdminAuthErrorResponse(401, "Missing 'sub' (admin ID) claim in JWT token", "MISSING_SUB_CLAIM"));
                }

                if (!Guid.TryParse(adminIdClaim.Value, out var adminId))
                {
                    _logger.LogWarning("❌ Invalid admin ID format: {Value}", adminIdClaim.Value);
                    return Unauthorized(new AdminAuthErrorResponse(401, $"Invalid admin ID format in 'sub' claim: {adminIdClaim.Value}", "INVALID_ADMIN_ID"));
                }

                // Verify this is a registration temp token by checking 'registration_step' claim
                var registrationStepClaim = principal.FindFirst("registration_step");
                if (registrationStepClaim == null)
                {
                    _logger.LogWarning("⚠️ Missing 'registration_step' claim in JWT");
                    return Unauthorized(new AdminAuthErrorResponse(401, "Missing 'registration_step' claim. Token must be from bootstrap/register endpoint.", "MISSING_REGISTRATION_STEP"));
                }

                if (registrationStepClaim.Value != "pending_2fa")
                {
                    _logger.LogWarning("❌ Invalid registration_step: {Value}", registrationStepClaim.Value);
                    return Unauthorized(new AdminAuthErrorResponse(401, $"Invalid registration step: {registrationStepClaim.Value}. Expected: pending_2fa", "INVALID_REGISTRATION_STEP"));
                }

                // Extract registration session ID (points to Redis where temp data is stored)
                var registrationSessionIdClaim = principal.FindFirst("registration_session_id");
                if (registrationSessionIdClaim == null)
                {
                    _logger.LogWarning("❌ Missing 'registration_session_id' claim in JWT");
                    return Unauthorized(new AdminAuthErrorResponse(401, "Missing 'registration_session_id' claim. Token must be from bootstrap/register endpoint.", "MISSING_SESSION_ID"));
                }

                var registrationSessionId = registrationSessionIdClaim.Value;

                _logger.LogInformation("✅ All validations passed. Generating 2FA setup for admin {AdminId}, sessionId={SessionId}", adminId, registrationSessionId);

                var response = await _authService.SetupTwoFactorGenerateAsync(adminId, registrationSessionId, cancellationToken);

                _logger.LogInformation("✅ 2FA setup generated successfully");
                return Ok(response);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning(ex, "❌ JWT validation failed");
                return Unauthorized(new AdminAuthErrorResponse(401, $"Token validation failed: {ex.Message}", "TOKEN_VALIDATION_FAILED"));
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "❌ JWT parsing error");
                return Unauthorized(new AdminAuthErrorResponse(401, $"Token parsing error: {ex.Message}", "TOKEN_PARSE_ERROR"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to generate 2FA setup: {Message}", ex.Message);
            return StatusCode(500, new AdminAuthErrorResponse(500, $"Setup generation failed: {ex.Message}", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Enable 2FA after verifying the code
    /// POST /api/auth/admin/setup-2fa/enable
    /// 
    /// Called after admin scans QR code and enters 6-digit code from authenticator.
    /// Must match the code generated by the secret stored in Redis.
    /// On success, returns 8 backup codes (MUST be saved by admin).
    /// After this, admin is fully registered and can login.
    /// 
    /// 🔐 SECURITY: Retrieves TOTP secret from Redis (NOT JWT!)
    /// Uses sessionId from JWT to lookup secret in Redis
    /// Deletes session from Redis after verification (cleanup)
    /// 
    /// Authorization: Temporary JWT token (from bootstrap or register endpoints)
    /// 
    /// Request body:
    /// {
    ///   "code": "6-digit code from Google Authenticator"
    /// }
    /// 
    /// Response (200):
    /// {
    ///   "backupCodes": ["CODE1", "CODE2", ...],
    ///   "success": true,
    ///   "message": "2FA enabled successfully. Save these backup codes..."
    /// }
    /// </summary>
    [HttpPost("admin/setup-2fa/enable")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminTwoFactorCompleteResponse>> EnableTwoFactor(
        [FromBody] AdminSetupTwoFactorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            _logger.LogInformation("=== EnableTwoFactor: START ===");
            var authHeader = Request.Headers["Authorization"].ToString();
            _logger.LogInformation("Auth Header Present: {Present}", !string.IsNullOrEmpty(authHeader));
            
            if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
            {
                _logger.LogWarning("No Bearer token");
                return Unauthorized(new AdminAuthErrorResponse(401, "Missing Bearer token", "NO_TOKEN"));
            }

            var token = authHeader.Substring("Bearer ".Length).Trim();
            _logger.LogInformation("Token received, length: {Length}", token.Length);

            // Use JwtSecurityTokenHandler to manually parse and validate
            var tokenHandler = new JwtSecurityTokenHandler();
            
            try
            {
                var principal = tokenHandler.ValidateToken(token, 
                    new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key)),
                        ValidateIssuer = true,
                        ValidIssuer = _jwtSettings.Issuer,
                        ValidateAudience = true,
                        ValidAudience = _jwtSettings.Audience,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromMinutes(1)
                    }, out SecurityToken validatedToken);

                _logger.LogInformation("✅ Token validated successfully");

                // Try to find admin ID - check multiple possible claim names
                var adminIdClaim = principal.FindFirst("sub") 
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)
                    ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");
                
                if (adminIdClaim == null)
                {
                    _logger.LogWarning("❌ No 'sub' or NameIdentifier claim in token");
                    return Unauthorized(new AdminAuthErrorResponse(401, "No admin ID in token", "NO_SUB"));
                }

                if (!Guid.TryParse(adminIdClaim.Value, out var adminId))
                {
                    _logger.LogWarning("❌ Invalid admin ID format: {Value}", adminIdClaim.Value);
                    return Unauthorized(new AdminAuthErrorResponse(401, "Invalid admin ID format", "INVALID_ADMIN_ID"));
                }

                var registrationStepClaim = principal.FindFirst("registration_step");
                if (registrationStepClaim?.Value != "pending_2fa")
                {
                    _logger.LogWarning("❌ Wrong registration_step: {Value}", registrationStepClaim?.Value ?? "NULL");
                    return Unauthorized(new AdminAuthErrorResponse(401, "Not a 2FA setup token", "WRONG_TOKEN_TYPE"));
                }

                // Extract registration_session_id from JWT (stored in Step 1)
                var registrationSessionIdClaim = principal.FindFirst("registration_session_id");
                if (string.IsNullOrWhiteSpace(registrationSessionIdClaim?.Value))
                {
                    _logger.LogWarning("❌ No registration_session_id in JWT");
                    return Unauthorized(new AdminAuthErrorResponse(401, "No registration session ID in token", "NO_REG_SESSION"));
                }

                // 🔐 SECURITY: Extract sessionId from REQUEST BODY (generated in /generate)
                if (string.IsNullOrWhiteSpace(request.SessionId))
                {
                    _logger.LogWarning("❌ No SessionId in request body");
                    return BadRequest(new AdminAuthErrorResponse(400, "Missing SessionId in request body. Must call /generate first to get sessionId.", "NO_SESSION"));
                }

                var sessionId = request.SessionId;
                var registrationSessionId = registrationSessionIdClaim.Value;

                // 🔐 SECURITY: Verify sessionId from request matches JWT claim (prevent tampering)
                if (sessionId != registrationSessionId)
                {
                    _logger.LogWarning("❌ SessionId mismatch! request={RequestSessionId}, jwt={JwtSessionId}", sessionId, registrationSessionId);
                    return BadRequest(new AdminAuthErrorResponse(400, "SessionId mismatch. Request has been tampered with.", "SESSION_MISMATCH"));
                }

                _logger.LogInformation("✅ All validations passed for admin {AdminId}, sessionId={SessionId}", 
                    adminId, sessionId);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                // 🔐 SECURITY: sessionId == registrationSessionId (verified above)
                var response = await _authService.SetupTwoFactorEnableAsync(
                    adminId,
                    request.Code,
                    sessionId,  // ← Both TOTP secret Redis session AND registration data are keyed by this
                    sessionId,  // ← Same as above (after validation they're equal)
                    ipAddress,
                    userAgent,
                    cancellationToken);

                _logger.LogInformation("✅ 2FA enabled for admin {AdminId}, Redis session cleaned up", adminId);
                return Ok(response);
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("❌ Token validation failed: {Message}", ex.Message);
                return Unauthorized(new AdminAuthErrorResponse(401, "Invalid or expired token", "TOKEN_INVALID"));
            }
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "2FA setup failed: {Message}", ex.Message);
            return BadRequest(new AdminAuthErrorResponse(400, ex.Message, "INVALID_CODE"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 2FA enable error");
            return StatusCode(500, new AdminAuthErrorResponse(500, $"Error: {ex.Message}", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Admin login (first step - password verification)
    /// POST /api/auth/admin-login
    /// 
    /// Step 1 of 2FA login process.
    /// Verifies username and password, then returns temporary token.
    /// Admin MUST verify 2FA code using the verify-2fa endpoint to get final JWT.
    /// 
    /// Request body:
    /// {
    ///   "usernameOrEmail": "admin username or email",
    ///   "password": "admin password"
    /// }
    /// 
    /// Response (200):
    /// {
    ///   "token": "temporary JWT (5 min, for 2FA verification only)",
    ///   "sessionId": "unique session identifier",
    ///   "requiresTwoFactor": true,
    ///   "username": "admin username"
    /// }
    /// 
    /// Response (401):
    /// {
    ///   "statusCode": 401,
    ///   "message": "Invalid credentials",
    ///   "errorCode": "INVALID_CREDENTIALS"
    /// }
    /// </summary>
    [HttpPost("admin-login")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminLoginResponse>> AdminLogin(
        [FromBody] AdminLoginRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var response = await _authService.AdminLoginAsync(
                request.UsernameOrEmail,
                request.Password,
                ipAddress,
                userAgent,
                cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Admin login failed: {Message}", ex.Message);
            return Unauthorized(new AdminAuthErrorResponse(401, ex.Message, "INVALID_CREDENTIALS"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin login error");
            return StatusCode(500, new AdminAuthErrorResponse(500, "Login failed", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Verify 2FA code and get final JWT token
    /// POST /api/auth/admin/verify-2fa
    /// 
    /// Step 2 of 2FA login process.
    /// Verifies 6-digit code from Google Authenticator.
    /// On success, returns final JWT token (60 min) for API access.
    /// Code can be either TOTP code or backup code (if authenticator lost).
    /// 
    /// Authorization: Temporary JWT token (from admin-login response)
    /// 
    /// Request body:
    /// {
    ///   "sessionId": "session ID from admin-login response",
    ///   "code": "6-digit code or backup code"
    /// }
    /// 
    /// Response (200):
    /// {
    ///   "token": "final JWT (60 min, for API access)",
    ///   "role": "Admin",
    ///   "adminId": "admin GUID",
    ///   "username": "admin username",
    ///   "expiresAt": 1234567890
    /// }
    /// 
    /// Response (401):
    /// {
    ///   "statusCode": 401,
    ///   "message": "Invalid 2FA code",
    ///   "errorCode": "INVALID_CODE"
    /// }
    /// </summary>
    [HttpPost("admin/verify-2fa")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminAuthSuccessResponse>> VerifyAdminTwoFactor(
        [FromBody] AdminVerifyTwoFactorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("❌ Invalid model state for verify-2fa");
            return BadRequest(ModelState);
        }

        try
        {
            _logger.LogInformation("📍 VerifyAdminTwoFactor called");

            // Extract Bearer token from Authorization header
            var authHeader = Request.Headers["Authorization"].ToString();
            if (string.IsNullOrEmpty(authHeader))
            {
                _logger.LogWarning("❌ No Authorization header provided");
                return Unauthorized(new AdminAuthErrorResponse(401, "Missing Authorization header", "NO_AUTH_HEADER"));
            }

            var token = authHeader.StartsWith("Bearer ") ? authHeader["Bearer ".Length..] : authHeader;
            if (string.IsNullOrEmpty(token))
            {
                _logger.LogWarning("❌ No Bearer token found");
                return Unauthorized(new AdminAuthErrorResponse(401, "Invalid Authorization header format", "INVALID_AUTH_FORMAT"));
            }

            _logger.LogInformation("✅ Authorization header found, validating token...");

            // Manually validate JWT
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_jwtSettings.Key);
            
            try
            {
                var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                }, out SecurityToken validatedToken);

                _logger.LogInformation("✅ Token validated successfully");

                // Try to find admin ID - check multiple possible claim names
                var adminIdClaim = principal.FindFirst("sub")
                    ?? principal.FindFirst(ClaimTypes.NameIdentifier)
                    ?? principal.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier");

                if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                {
                    _logger.LogWarning("❌ No valid admin ID in token");
                    return Unauthorized(new AdminAuthErrorResponse(401, "Invalid token: no admin ID", "NO_ADMIN_ID"));
                }

                // Verify this is a login session token (not a 2FA setup token)
                var sessionIdClaim = principal.FindFirst("session_id");
                if (sessionIdClaim == null)
                {
                    _logger.LogWarning("❌ No session_id in token - this is not a login token");
                    return Unauthorized(new AdminAuthErrorResponse(401, "Invalid token type: not a login session", "WRONG_TOKEN_TYPE"));
                }

                var requires2FAClaim = principal.FindFirst("requires_2fa");
                if (requires2FAClaim?.Value != "true")
                {
                    _logger.LogWarning("❌ Token doesn't require 2FA verification (already verified or wrong token)");
                    return Unauthorized(new AdminAuthErrorResponse(401, "Token doesn't require 2FA verification", "NO_2FA_REQUIRED"));
                }

                _logger.LogInformation("✅ Token structure valid. Admin: {AdminId}, SessionId: {SessionId}", adminId, sessionIdClaim.Value);

                // Now verify 2FA code
                _logger.LogInformation("📍 Verifying 2FA code for admin {AdminId}", adminId);

                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var userAgent = Request.Headers["User-Agent"].ToString();

                var response = await _authService.VerifyAdminTwoFactorAsync(
                    adminId,
                    sessionIdClaim.Value,
                    request.Code,
                    ipAddress,
                    userAgent,
                    cancellationToken);

                _logger.LogInformation("✅ 2FA verified successfully. Admin {AdminId} logged in", adminId);

                return Ok(response);
            }
            catch (SecurityTokenExpiredException ex)
            {
                _logger.LogWarning("❌ Token expired: {Message}", ex.Message);
                return Unauthorized(new AdminAuthErrorResponse(401, "Token expired. Please login again", "TOKEN_EXPIRED"));
            }
            catch (SecurityTokenInvalidSignatureException ex)
            {
                _logger.LogWarning("❌ Invalid token signature: {Message}", ex.Message);
                return Unauthorized(new AdminAuthErrorResponse(401, "Invalid token signature", "INVALID_SIGNATURE"));
            }
            catch (SecurityTokenException ex)
            {
                _logger.LogWarning("❌ Token validation failed: {Message}", ex.Message);
                return Unauthorized(new AdminAuthErrorResponse(401, $"Token validation failed: {ex.Message}", "TOKEN_INVALID"));
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("❌ 2FA verification failed: {Message}", ex.Message);
            return Unauthorized(new AdminAuthErrorResponse(401, ex.Message, "INVALID_CODE"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ 2FA verification error: {Message}", ex.Message);
            return StatusCode(500, new AdminAuthErrorResponse(500, $"Verification failed: {ex.Message}", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Disable 2FA for admin
    /// POST /api/auth/admin/setup-2fa/disable
    /// 
    /// WARNING: Disabling 2FA will prevent login until re-enabled.
    /// Requires verification with current 2FA code as security measure.
    /// 
    /// Authorization: JWT token (standard admin access)
    /// 
    /// Request body:
    /// {
    ///   "code": "6-digit code to verify"
    /// }
    /// 
    /// Response (200):
    /// {
    ///   "success": true,
    ///   "message": "2FA disabled. You'll need to re-enable it to login..."
    /// }
    /// </summary>
    [HttpPost("admin/setup-2fa/disable")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminTwoFactorDisableResponse>> DisableTwoFactor(
        [FromBody] AdminDisableTwoFactorRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var adminIdClaim = User.FindFirst("sub");
            if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                return Unauthorized();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            await _authService.DisableTwoFactorAsync(adminId, request.Code, ipAddress, userAgent, cancellationToken);

            _logger.LogWarning("2FA disabled for admin {AdminId}", adminId);

            return Ok(new AdminTwoFactorDisableResponse(
                Success: true,
                Message: "2FA has been disabled. You will need to re-enable it before you can login again."));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "2FA disable error");
            return StatusCode(500, new AdminAuthErrorResponse(500, "Disable failed", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Regenerate backup codes
    /// POST /api/auth/admin/backup-codes/regenerate
    /// 
    /// Generates new set of 8 backup codes.
    /// Old codes are invalidated.
    /// Requires verification with current 2FA code as security measure.
    /// 
    /// Authorization: JWT token (standard admin access)
    /// 
    /// Request body:
    /// {
    ///   "code": "6-digit code to verify"
    /// }
    /// 
    /// Response (200):
    /// {
    ///   "backupCodes": ["CODE1", "CODE2", ...],
    ///   "success": true,
    ///   "message": "Backup codes regenerated. Save these..."
    /// }
    /// </summary>
    [HttpPost("admin/backup-codes/regenerate")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminTwoFactorCompleteResponse>> RegenerateBackupCodes(
        [FromBody] AdminRegenerateBackupCodesRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var adminIdClaim = User.FindFirst("sub");
            if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var adminId))
                return Unauthorized();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var codes = await _authService.RegenerateBackupCodesAsync(
                adminId,
                request.Code,
                ipAddress,
                userAgent,
                cancellationToken);

            _logger.LogInformation("Backup codes regenerated for admin {AdminId}", adminId);

            return Ok(new AdminTwoFactorCompleteResponse(
                BackupCodes: codes,
                Success: true,
                Message: "Backup codes have been regenerated. Save these codes somewhere safe!"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup codes regenerate error");
            return StatusCode(500, new AdminAuthErrorResponse(500, "Regeneration failed", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Bootstrap Super Admin (one-time only)
    /// POST /api/auth/admin/bootstrap
    /// 
    /// One-time endpoint to create the first Super Admin.
    /// Can only be called if no admin exists in database.
    /// After first call, this endpoint returns 403 Forbidden.
    /// 
    /// Public access (no authentication required)
    /// 
    /// Request body:
    /// {
    ///   "username": "superadmin",
    ///   "email": "admin@company.com",
    ///   "password": "SecurePassword123!"
    /// }
    /// 
    /// Response (201):
    /// <summary>
    /// Register admin via invitation token
    /// POST /api/auth/admin/register
    /// 
    /// Called by invited admin to complete registration.
    /// Frontend receives invitation link: https://app.com/admin/register?token=ABC123
    /// Admin submits form with username, password, and token.
    /// On success: Returns temp token for 2FA setup.
    /// 
    /// Authorization: None (public endpoint, token validates the request)
    /// 
    /// Request body:
    /// {
    ///   "token": "48h invitation token from email",
    ///   "username": "john.doe",
    ///   "password": "SecurePassword123!"
    /// }
    /// 
    /// Response (201):
    /// {
    ///   "token": "temporary JWT (5 min, for 2FA setup)",
    ///   "sessionId": "unique session id",
    ///   "requiresTwoFactorSetup": true,
    ///   "message": "Admin registered. You must set up 2FA..."
    /// }
    /// 
    /// Response (400):
    /// {
    ///   "statusCode": 400,
    ///   "message": "Invalid or expired token / Username already taken",
    ///   "errorCode": "INVALID_TOKEN" / "USERNAME_TAKEN"
    /// }
    /// </summary>
    [HttpPost("admin/register")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminRegistrationResponse>> RegisterAdminViaInvitation(
        [FromBody] AdminRegisterViaInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var response = await _authService.RegisterAdminViaInviteAsync(
                request.Token,
                request.Username,
                request.Password,
                ipAddress,
                userAgent,
                cancellationToken);

            _logger.LogInformation("Admin registered via invitation successfully");

            return CreatedAtAction(nameof(RegisterAdminViaInvitation), response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Registration failed: {Message}", ex.Message);

            if (ex.Message.Contains("Username"))
                return BadRequest(new AdminAuthErrorResponse(400, ex.Message, "USERNAME_TAKEN"));

            if (ex.Message.Contains("Token") || ex.Message.Contains("expired") || ex.Message.Contains("invalid"))
                return BadRequest(new AdminAuthErrorResponse(400, "Invalid or expired invitation token", "INVALID_TOKEN"));

            return BadRequest(new AdminAuthErrorResponse(400, ex.Message, "REGISTRATION_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration error");
            return StatusCode(500, new AdminAuthErrorResponse(500, "Registration failed", "INTERNAL_ERROR"));
        }
    }

    /// {
    ///   "token": "temporary JWT (5 min, for 2FA setup)",
    ///   "sessionId": "unique session id",
    ///   "requiresTwoFactorSetup": true,
    ///   "message": "Super Admin created. You must set up 2FA..."
    /// }
    /// 
    /// Response (409):
    /// {
    ///   "statusCode": 409,
    ///   "message": "Super Admin already exists",
    ///   "errorCode": "ALREADY_BOOTSTRAPPED"
    /// }
    /// </summary>
    [HttpPost("admin/bootstrap")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminRegistrationResponse>> BootstrapSuperAdmin(
        [FromBody] AdminBootstrapRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var response = await _authService.BootstrapSuperAdminAsync(
                request.Username,
                request.Email,
                request.Password,
                ipAddress,
                userAgent,
                cancellationToken);

            _logger.LogInformation("Super Admin bootstrapped successfully");

            return CreatedAtAction(nameof(BootstrapSuperAdmin), response);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bootstrap failed: {Message}", ex.Message);

            if (ex.Message.Contains("already exists"))
                return Conflict(new AdminAuthErrorResponse(409, ex.Message, "ALREADY_BOOTSTRAPPED"));

            return BadRequest(new AdminAuthErrorResponse(400, ex.Message, "BOOTSTRAP_FAILED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootstrap error");
            return StatusCode(500, new AdminAuthErrorResponse(500, "Bootstrap failed", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Super Admin invites new admin
    /// POST /api/auth/admin/invite
    /// 
    /// Super Admin creates invitation token for new admin.
    /// Token is sent via email (email service to be implemented).
    /// New admin uses token with /admin/register?token=XYZ endpoint.
    /// 
    /// Authorization: JWT token with Admin role
    /// 
    /// Request body:
    /// {
    ///   "email": "newadmin@company.com",
    ///   "firstName": "John",
    ///   "lastName": "Doe",
    ///   "permissions": ["ManageInstruments", "ViewAuditLogs"]
    /// }
    /// 
    /// Response (201):
    /// {
    ///   "token": "32-char invitation token",
    ///   "email": "newadmin@company.com",
    ///   "expiresAt": "2026-04-21T10:00:00Z",
    ///   "invitationUrl": "https://app.com/admin/register?token=ABC123..."
    /// }
    /// </summary>
    [HttpPost("admin/invite")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<AdminInvitationResponse>> InviteAdmin(
        [FromBody] AdminInviteRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        try
        {
            // Extract super admin ID from JWT claims
            var adminIdClaim = User.FindFirst("sub");
            if (adminIdClaim == null || !Guid.TryParse(adminIdClaim.Value, out var superAdminId))
                return Unauthorized();

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var userAgent = Request.Headers["User-Agent"].ToString();

            var response = await _authService.InviteAdminAsync(
                superAdminId,
                request.Email,
                request.FirstName,
                request.LastName,
                request.Permissions,
                ipAddress,
                userAgent,
                cancellationToken);

            _logger.LogInformation("Admin invited: {Email}", request.Email);

            // TODO: Send email with invitation link
            // await _emailService.SendAdminInvitationEmailAsync(response.Email, response.InvitationUrl);

            return CreatedAtAction(nameof(InviteAdmin), response);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Invite failed - unauthorized: {Message}", ex.Message);
            return Unauthorized(new AdminAuthErrorResponse(401, ex.Message, "UNAUTHORIZED"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin invite error");
            return StatusCode(500, new AdminAuthErrorResponse(500, "Invitation failed", "INTERNAL_ERROR"));
        }
    }

    /// <summary>
    /// Admin panel health check
    /// GET /api/admin/health
    /// 
    /// Simple endpoint to verify admin API is accessible.
    /// Can be called with or without authentication token for diagnostics.
    /// 
    /// Authorization: Optional (works with or without Bearer token)
    /// 
    /// Response (200):
    /// {
    ///   "status": "Healthy",
    ///   "timestamp": "2026-04-20T10:30:00Z",
    ///   "message": "Admin API is operational"
    /// }
    /// </summary>
    [HttpGet("admin/health")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<AdminHealthCheckResponse> GetAdminHealth()
    {
        var authHeader = Request.Headers["Authorization"].ToString();
        var hasToken = !string.IsNullOrEmpty(authHeader);
        
        _logger.LogInformation("📍 [HEALTH CHECK] Admin API health requested. Auth: {Auth}", 
            hasToken ? "✅ Present" : "⚠️  Missing");

        return Ok(new AdminHealthCheckResponse(
            Status: "Healthy",
            Timestamp: DateTime.UtcNow,
            Message: hasToken ? "Admin API operational (authenticated)" : "Admin API operational (unauthenticated)",
            IsAuthenticated: hasToken,
            ApiVersion: "1.0.0"
        ));
    }
}
