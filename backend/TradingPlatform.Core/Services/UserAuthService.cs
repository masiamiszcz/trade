using AutoMapper;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;

namespace TradingPlatform.Data.Services;

/// <summary>
/// User authentication service with mandatory 2FA during registration
/// 
/// KEY DIFFERENCE FROM ADMIN:
/// - User 2FA is MANDATORY during registration but OPTIONAL for login
/// - Registration is 2-step: Step 1 validates data, Step 2 creates account
/// - Admin 2FA is mandatory after registration (login fails without it)
/// 
/// REGISTRATION FLOW:
/// 1. RegisterInitialAsync() → validate, generate secret, return QR (NO USER IN DB)
/// 2. RegisterCompleteAsync() → verify code, CREATE USER, return final token
/// </summary>
public sealed class UserAuthService : IUserAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IAccountService _accountService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IEncryptionService _encryptionService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IRedisSessionService _redisSessionService;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IMapper _mapper;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly ILogger<UserAuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    // Constants for rate limiting
    private const int MaxFailedAttempts = 5;
    private const int LockoutDurationSeconds = 300; // 5 minutes
    private const int SessionTimeoutSeconds = 600; // 10 minutes

    public UserAuthService(
        IUserRepository userRepository,
        IAccountService accountService,
        ITwoFactorService twoFactorService,
        IEncryptionService encryptionService,
        IJwtTokenGenerator jwtTokenGenerator,
        IRedisSessionService redisSessionService,
        IValidator<RegisterRequest> registerValidator,
        IMapper mapper,
        ILogger<UserAuthService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
        _twoFactorService = twoFactorService ?? throw new ArgumentNullException(nameof(twoFactorService));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
        _redisSessionService = redisSessionService ?? throw new ArgumentNullException(nameof(redisSessionService));
        _registerValidator = registerValidator ?? throw new ArgumentNullException(nameof(registerValidator));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    /// <summary>
    /// REGISTRATION STEP 1:
    /// User provides registration data (username, email, password, etc.)
    /// - Validates input
    /// - Generates TOTP secret
    /// - Generates QR code and backup codes
    /// - ⚠️ DOES NOT create user in database
    /// - Returns temp token (5 min) for 2FA verification step
    /// </summary>
    public async Task<UserRegistrationInitialResponse> RegisterInitialAsync(
        string username,
        string email,
        string firstName,
        string lastName,
        string password,
        string baseCurrency,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required", nameof(username));
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));

        try
        {
            // Validate registration data using FluentValidation
            var registerRequest = new RegisterRequest(
                UserName: username,
                Email: email,
                FirstName: firstName,
                LastName: lastName,
                Password: password,
                BaseCurrency: baseCurrency);

            var validationResult = await _registerValidator.ValidateAsync(registerRequest, cancellationToken);
            if (!validationResult.IsValid)
            {
                var errorMessage = string.Join(" ", validationResult.Errors.Select(e => e.ErrorMessage));
                _logger.LogWarning("Registration validation failed: {Error}", errorMessage);
                throw new ArgumentException(errorMessage);
            }

            // Check username uniqueness
            var existingByUserName = await _userRepository.GetByUserNameAsync(username, cancellationToken);
            if (existingByUserName is not null)
            {
                _logger.LogWarning("Registration failed: username '{Username}' already taken", username);
                throw new InvalidOperationException("Username is already taken");
            }

            // Check email uniqueness
            var existingByEmail = await _userRepository.GetByEmailAsync(email, cancellationToken);
            if (existingByEmail is not null)
            {
                _logger.LogWarning("Registration failed: email '{Email}' already registered", email);
                throw new InvalidOperationException("Email is already registered");
            }

            // Generate TOTP secret
            var secretDto = _twoFactorService.GenerateSecret();
            
            // Generate backup codes
            var backupCodes = _twoFactorService.GenerateBackupCodes();

            // Create session ID for this registration attempt
            var sessionId = Guid.NewGuid().ToString();

            // 🔐 SECURITY: Store all session data in Redis (NOT in JWT!)
            // This includes:
            // - TOTP secret (for verification)
            // - Backup codes (shown to user, then hashed in DB)
            // - Password (hashed immediately after verification, then deleted from Redis)
            // 
            // This approach:
            // 1. Keeps sensitive data off the network (not in JWT)
            // 2. Keeps data encrypted in transit (Redis connection is internal)
            // 3. Auto-cleanup after 10 minutes (TTL)
            // 4. Password never stored persistently
            await _redisSessionService.CreateSessionAsync(
                sessionId,
                Guid.NewGuid().ToString(), // temp userId for this session
                secretDto.Secret,
                SessionTimeoutSeconds,
                password: password, // ⚠️ Plaintext in Redis (memory only, expires 10min)
                backupCodes: backupCodes.ToList(),
                cancellationToken);

            _logger.LogInformation(
                "Registration STEP 1: Created Redis session {SessionId} with TOTP secret, backup codes, and password (all expire in {Timeout}s)",
                sessionId, SessionTimeoutSeconds);

            // Create temporary user object (for metadata only - not saved to DB yet)
            var tempUserId = Guid.NewGuid();
            var tempUser = new User(
                Id: tempUserId,
                UserName: username.Trim(),
                Email: email.Trim(),
                FirstName: firstName.Trim(),
                LastName: lastName.Trim(),
                Role: UserRole.User,
                EmailConfirmed: false,
                TwoFactorEnabled: false, // Will be true after 2FA verification
                TwoFactorSecret: string.Empty, // Will be encrypted and saved in Step 2
                BackupCodes: string.Empty, // Will be saved in Step 2
                Status: UserStatus.Active,
                BaseCurrency: baseCurrency.ToUpper(),
                CreatedAtUtc: DateTimeOffset.UtcNow);

            // Generate temporary token (5 min) for 2FA verification
            // This token contains:
            // - userId (needed for RegisterCompleteAsync)
            // - sessionId (pointer to Redis where everything is stored)
            // - requires_2fa claim
            // ✅ DOES NOT contain: totp_secret, password, backup_codes
            var tempToken = _jwtTokenGenerator.GenerateToken(
                tempUser,
                isTempToken: true,
                context: new TokenContext 
                { 
                    SessionId = sessionId,
                    TwoFactorRequired = true,
                    TotpSecret = string.Empty, // ✅ EMPTY - everything is in Redis!
                    BackupCodes = new List<string>(), // ✅ NOT included in JWT
                    Password = string.Empty
                });

            _logger.LogInformation(
                "Registration STEP 1 completed for user '{Username}' - sessionId={SessionId}, awaiting 2FA verification",
                username, sessionId);

            return new UserRegistrationInitialResponse(
                Token: tempToken,
                SessionId: sessionId,
                QrCodeDataUrl: secretDto.QrCodeDataUrl,
                ManualKey: secretDto.Secret,
                BackupCodes: backupCodes.ToList(),
                Message: "2FA is required to complete registration. Scan the QR code with Google Authenticator or similar app.");
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration STEP 1 error for user '{Username}'", username);
            throw new InvalidOperationException("Registration failed", ex);
        }
    }

    /// <summary>
    /// REGISTRATION STEP 2:
    /// User provides 2FA code from authenticator
    /// - Verifies code against generated secret
    /// - If valid: CREATES USER in database
    /// - Saves encrypted TOTP secret and backup codes
    /// - Creates main account with initial balance
    /// - Returns final JWT token (60 min) - user is now fully registered
    /// </summary>
    public async Task<UserRegistrationCompleteResponse> RegisterCompleteAsync(
        string sessionId,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        try
        {
            // NOTE: In production, sessionId would be looked up in Redis/session store
            // For now, we require the temp token to be sent in Authorization header by frontend
            // The token validation happens in controller through [Authorize] attribute
            
            // This is a simplified implementation - in real scenario:
            // 1. Controller extracts token from header
            // 2. Framework validates token through [Authorize] filter
            // 3. Controller extracts userId and secret from claims
            // 4. Calls this method with extracted data
            
            // For now, throwing to indicate this requires proper integration
            throw new InvalidOperationException(
                "RegisterCompleteAsync requires token extraction from JWT claims in controller. " +
                "Controller should extract userId and totp_secret from JWT and pass them explicitly.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration STEP 2 error for session '{SessionId}'", sessionId);
            throw new InvalidOperationException("2FA verification failed 1", ex);
        }
    }

    /// <summary>
    /// REGISTRATION STEP 2 (Complete - with Redis session lookup)
    /// This is called from controller after JWT token validation
    /// 
    /// Flow:
    /// 1. Retrieve TOTP secret from Redis using sessionId
    /// 2. Check rate limiting (max 5 attempts, then lockout)
    /// 3. Verify 2FA code
    /// 4. If valid: create user + delete Redis session
    /// 5. If invalid: increment attempts + possibly lock
    /// </summary>
    /// <summary>
    /// Complete user registration Step 2: Verify 2FA code and create account
    /// Overload that retrieves password + backup codes from Redis using sessionId
    /// ✅ SECURE: Password + codes never leave server, never in JWT
    /// </summary>
    public async Task<UserRegistrationCompleteResponse> RegisterCompleteInternalAsync(
        Guid userId,
        string username,
        string email,
        string firstName,
        string lastName,
        string baseCurrency,
        string code,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        try
        {
            // Retrieve session from Redis (contains TOTP secret, password, backup codes)
            var session = await _redisSessionService.GetSessionAsync(sessionId, cancellationToken);
            if (session == null)
                throw new UnauthorizedAccessException("2FA session expired or invalid");

            // Check if session is locked (too many failed attempts)
            var isLocked = await _redisSessionService.IsSessionLockedAsync(sessionId, cancellationToken);
            if (isLocked)
                throw new UnauthorizedAccessException("Session locked due to too many failed attempts. Please try again in 5 minutes.");

            // Get failed attempts count
            var failedAttempts = await _redisSessionService.GetFailedAttemptsAsync(sessionId, cancellationToken);
            if (failedAttempts >= MaxFailedAttempts)
            {
                await _redisSessionService.LockSessionAsync(sessionId, LockoutDurationSeconds, cancellationToken);
                throw new UnauthorizedAccessException($"Maximum 2FA attempts ({MaxFailedAttempts}) exceeded. Account locked for {LockoutDurationSeconds / 60} minutes.");
            }

            // Verify 2FA code using Redis-stored secret
            if (!_twoFactorService.VerifyCode(session.TotpSecret, code))
            {
                // Increment failed attempts
                var newAttemptCount = await _redisSessionService.IncrementFailedAttemptsAsync(sessionId, cancellationToken);
                
                if (newAttemptCount >= MaxFailedAttempts)
                {
                    await _redisSessionService.LockSessionAsync(sessionId, LockoutDurationSeconds, cancellationToken);
                    throw new UnauthorizedAccessException(
                        $"Invalid 2FA code. Maximum attempts ({MaxFailedAttempts}) exceeded. Account locked for {LockoutDurationSeconds / 60} minutes.");
                }

                throw new ArgumentException(
                    $"Invalid 2FA code. Attempts: {newAttemptCount}/{MaxFailedAttempts}. Session expires in 10 minutes.");
            }

            // Get password and backup codes from session
            var password = session.Password ?? throw new InvalidOperationException("Password missing from session");
            var backupCodes = session.BackupCodes ?? throw new InvalidOperationException("Backup codes missing from session");

            // Delegate to the original overload with all parameters explicitly provided
            return await RegisterCompleteInternalAsync(
                userId,
                username,
                email,
                firstName,
                lastName,
                baseCurrency,
                code,
                sessionId,
                backupCodes,
                password,
                cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in RegisterCompleteInternalAsync (sessionId variant)");
            throw;
        }
    }

    /// <summary>
    /// Complete user registration Step 2 - original overload with all parameters
    /// Used internally; public API should use sessionId-based overload above
    /// </summary>
    public async Task<UserRegistrationCompleteResponse> RegisterCompleteInternalAsync(
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
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));

        try
        {
            _logger.LogInformation(
                "Registration STEP 2: Verifying 2FA code for user '{Username}', sessionId={SessionId}",
                username, sessionId);

            // 🔐 SECURITY: Get TOTP secret from Redis (NOT from JWT!)
            var sessionData = await _redisSessionService.GetSessionAsync(sessionId, cancellationToken);
            if (sessionData == null)
            {
                _logger.LogWarning(
                    "Registration STEP 2: Session {SessionId} not found or expired for user '{Username}'",
                    sessionId, username);
                throw new UnauthorizedAccessException("Session expired. Please start registration again.");
            }

            var totpSecret = sessionData.TotpSecret;

            // Check rate limiting - is session locked?
            var isLocked = await _redisSessionService.IsSessionLockedAsync(sessionId, cancellationToken);
            if (isLocked)
            {
                _logger.LogWarning(
                    "Registration STEP 2: Session {SessionId} is locked (too many failed attempts)",
                    sessionId);
                throw new InvalidOperationException(
                    "Too many failed attempts. Please try again in 5 minutes or restart registration.");
            }

            // Get current attempt count
            var attemptCount = await _redisSessionService.GetFailedAttemptsAsync(sessionId, cancellationToken);
            if (attemptCount >= MaxFailedAttempts)
            {
                // Lock session for 5 minutes
                await _redisSessionService.LockSessionAsync(sessionId, LockoutDurationSeconds, cancellationToken);
                _logger.LogWarning(
                    "Registration STEP 2: Locked session {SessionId} after {Attempts} failed attempts",
                    sessionId, attemptCount);
                throw new InvalidOperationException("Too many failed attempts. Please try again in 5 minutes.");
            }

            // Verify 2FA code against secret from Redis
            if (!_twoFactorService.VerifyCode(totpSecret, code))
            {
                // Increment attempt counter
                var newAttemptCount = await _redisSessionService.IncrementFailedAttemptsAsync(sessionId, cancellationToken);
                _logger.LogWarning(
                    "Registration STEP 2: Invalid 2FA code for user '{Username}', session {SessionId} - " +
                    "attempt {AttemptNumber} of {MaxAttempts}",
                    username, sessionId, newAttemptCount, MaxFailedAttempts);

                if (newAttemptCount >= MaxFailedAttempts)
                {
                    await _redisSessionService.LockSessionAsync(sessionId, LockoutDurationSeconds, cancellationToken);
                    throw new UnauthorizedAccessException(
                        $"Invalid 2FA code (attempt {newAttemptCount}/{MaxFailedAttempts}). " +
                        "Account locked for 5 minutes.");
                }

                throw new UnauthorizedAccessException(
                    $"Invalid 2FA code (attempt {newAttemptCount}/{MaxFailedAttempts})");
            }

            _logger.LogInformation(
                "Registration STEP 2: 2FA code verified for user '{Username}' - creating account",
                username);

            // ✅ Code is valid! Create user in database

            // Encrypt TOTP secret using IEncryptionService
            var encryptedSecret = _encryptionService.Encrypt(totpSecret);

            // Hash and store backup codes as JSON
            var hashedBackupCodes = backupCodes.Select(c => _twoFactorService.HashBackupCode(c)).ToList();
            var hashedBackupCodesJson = System.Text.Json.JsonSerializer.Serialize(hashedBackupCodes);

            // Create user entity
            var user = new User(
                Id: userId,
                UserName: username.Trim(),
                Email: email.Trim(),
                FirstName: firstName.Trim(),
                LastName: lastName.Trim(),
                Role: UserRole.User,
                EmailConfirmed: false,
                TwoFactorEnabled: true, // 2FA is ENABLED after verification
                TwoFactorSecret: encryptedSecret, // Encrypted secret in database
                BackupCodes: hashedBackupCodesJson, // Hashed codes in database
                Status: UserStatus.Active,
                BaseCurrency: baseCurrency.ToUpper(),
                CreatedAtUtc: DateTimeOffset.UtcNow);

            // Hash password securely
            var passwordHash = _passwordHasher.HashPassword(user, password);
            
            await _userRepository.AddAsync(user, passwordHash, cancellationToken);
            await _userRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("User '{Username}' created with 2FA enabled", username);

            // Create main account with initial balance
            await _accountService.CreateMainAccountAsync(
                user.Id,
                baseCurrency.ToUpper(),
                initialBalance: 10000,
                cancellationToken);

            // 🔐 CLEANUP: Delete session from Redis (successful verification)
            await _redisSessionService.DeleteSessionAsync(sessionId, cancellationToken);
            _logger.LogInformation("Cleaned up Redis session {SessionId}", sessionId);

            _logger.LogInformation("Main account created for user '{UserId}' with balance 10000 {BaseCurrency}", user.Id, baseCurrency);

            // Generate final JWT token (60 min)
            var finalToken = _jwtTokenGenerator.GenerateToken(user, isTempToken: false);
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds();

            _logger.LogInformation("User '{UserId}' fully registered with 2FA enabled", user.Id);

            return new UserRegistrationCompleteResponse(
                Token: finalToken,
                UserId: user.Id,
                Username: user.UserName,
                Email: user.Email,
                ExpiresAt: expiresAt,
                Message: "✅ 2FA verified! Your account is created and 2FA is enabled.",
                BackupCodes: backupCodes);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 🔐 CLEANUP: Delete session from Redis on error
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                try
                {
                    await _redisSessionService.DeleteSessionAsync(sessionId, cancellationToken);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Error cleaning up Redis session {SessionId}", sessionId);
                }
            }

            _logger.LogError(ex, "Registration STEP 2 error for user '{Username}'", username);
            throw new InvalidOperationException("Registration failed", ex);
        }
    }

    /// <summary>
    /// LOGIN STEP 1:
    /// User provides username/email and password
    /// - Verifies password
    /// - Checks 2FA status
    /// 
    /// If 2FA enabled: returns TEMP token (5 min) + sessionId → Frontend shows 2FA code input
    /// If 2FA disabled: returns FINAL token (60 min) → User logs in immediately
    /// </summary>
    public async Task<UserLoginInitialResponse> LoginInitialAsync(
        string userNameOrEmail,
        string password,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userNameOrEmail))
            throw new ArgumentException("Username or email is required", nameof(userNameOrEmail));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));

        try
        {
            // Get user with password hash
            var (user, hashedPassword) = await _userRepository.GetByUserNameOrEmailWithPasswordHashAsync(
                userNameOrEmail, cancellationToken);

            if (user == null || string.IsNullOrWhiteSpace(hashedPassword))
            {
                _logger.LogWarning("Login failed: invalid credentials for '{UserNameOrEmail}'", userNameOrEmail);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Verify password
            var verifyResult = _passwordHasher.VerifyHashedPassword(user, hashedPassword, password);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                _logger.LogWarning("Login failed: invalid password for user '{UserId}'", user.Id);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Check user is active
            if (user.Status != UserStatus.Active)
            {
                _logger.LogWarning("Login failed: user '{UserId}' is not active", user.Id);
                throw new UnauthorizedAccessException("User account is not active");
            }

            // Check 2FA status
            if (user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(user.TwoFactorSecret))
            {
                // 2FA is enabled for this user - require verification
                var sessionId = Guid.NewGuid().ToString();

                _logger.LogInformation("Login STEP 1 - 2FA enabled for user '{UserId}'", user.Id);

                // 🔐 SECURITY: Decrypt secret from DB, store in Redis (NOT JWT!)
                var decryptedSecret = _encryptionService.Decrypt(user.TwoFactorSecret);

                // Store decrypted secret in Redis for verification (NOT in JWT!)
                await _redisSessionService.CreateSessionAsync(
                    sessionId,
                    user.Id.ToString(),
                    decryptedSecret,
                    SessionTimeoutSeconds,
                    cancellationToken);

                _logger.LogInformation(
                    "Login STEP 1: Created Redis session {SessionId} for user '{UserId}' (TOTP secret stored in Redis, NOT JWT)",
                    sessionId, user.Id);

                // Generate temp token (5 min) - with sessionId only, NO secret!
                var tempToken = _jwtTokenGenerator.GenerateToken(
                    user,
                    isTempToken: true,
                    context: new TokenContext
                    {
                        SessionId = sessionId,
                        TwoFactorRequired = true,
                        TotpSecret = string.Empty, // ✅ EMPTY - secret is in Redis!
                        BackupCodes = new List<string>()
                    });

                _logger.LogInformation("Login STEP 1 successful for user '{UserId}' - awaiting 2FA", user.Id);

                return new UserLoginInitialResponse(
                    Token: tempToken,
                    SessionId: sessionId,
                    RequiresTwoFactor: true,
                    Username: user.UserName);
            }
            else
            {
                // 2FA not enabled - generate final token immediately
                var finalToken = _jwtTokenGenerator.GenerateToken(user, isTempToken: false);

                _logger.LogInformation("Login successful for user '{UserId}' without 2FA", user.Id);

                return new UserLoginInitialResponse(
                    Token: finalToken,
                    SessionId: Guid.NewGuid().ToString(), // Not used when 2FA disabled
                    RequiresTwoFactor: false,
                    Username: user.UserName);
            }
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Login error for user '{UserNameOrEmail}'", userNameOrEmail);
            throw new InvalidOperationException("Login failed", ex);
        }
    }

    /// <summary>
    /// LOGIN STEP 2:
    /// User provides 2FA code from authenticator
    /// Only called if user has 2FA enabled
    /// - Verifies code against encrypted TOTP secret
    /// - Returns final JWT token (60 min)
    /// </summary>
    public async Task<UserAuthCompleteResponse> VerifyUserTwoFactorAsync(
        string sessionId,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        try
        {
            // 🔑 1. Pobierz HttpContext
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                throw new UnauthorizedAccessException("No HTTP context");

            // 🔑 2. Pobierz userId z claims (JUŻ ZWERYFIKOWANY przez [Authorize])
            var userIdClaim = httpContext.User.FindFirst("userId")?.Value;

            if (!Guid.TryParse(userIdClaim, out var userId))
                throw new UnauthorizedAccessException("Invalid userId in token");

            // 🔑 3. Wywołaj właściwą logikę (Redis + weryfikacja)
            return await VerifyUserTwoFactorInternalAsync(
                userId,
                code,
                sessionId,
                cancellationToken);
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "2FA verification error for session '{SessionId}'", sessionId);
            throw new InvalidOperationException("2FA verification failed 2", ex);
        }
    }

    /// <summary>
    /// LOGIN STEP 2 (Complete - with Redis session lookup + rate limiting)
    /// This is called from controller after JWT token validation
    /// 
    /// Flow:
    /// 1. Retrieve TOTP secret from Redis using sessionId
    /// 2. Check rate limiting (max 5 attempts, then lockout)
    /// 3. Verify 2FA code
    /// 4. If valid: issue final token + delete Redis session
    /// 5. If invalid: increment attempts + possibly lock
    /// </summary>
    public async Task<UserAuthCompleteResponse> VerifyUserTwoFactorInternalAsync(
        Guid userId,
        string code,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        try
        {
            _logger.LogInformation(
                "Login STEP 2: Verifying 2FA code for user '{UserId}', sessionId={SessionId}",
                userId, sessionId);

            // 🔐 SECURITY: Get TOTP secret from Redis (NOT from JWT!)
            var sessionData = await _redisSessionService.GetSessionAsync(sessionId, cancellationToken);
            if (sessionData == null)
            {
                _logger.LogWarning(
                    "Login STEP 2: Session {SessionId} not found or expired for user '{UserId}'",
                    sessionId, userId);
                throw new UnauthorizedAccessException("Session expired. Please login again.");
            }

            var totpSecret = sessionData.TotpSecret;

            // Check rate limiting - is session locked?
            var isLocked = await _redisSessionService.IsSessionLockedAsync(sessionId, cancellationToken);
            if (isLocked)
            {
                _logger.LogWarning(
                    "Login STEP 2: Session {SessionId} is locked (too many failed attempts)",
                    sessionId);
                throw new InvalidOperationException(
                    "Too many failed attempts. Please try again in 5 minutes or login again.");
            }

            // Get current attempt count
            var attemptCount = await _redisSessionService.GetFailedAttemptsAsync(sessionId, cancellationToken);
            if (attemptCount >= MaxFailedAttempts)
            {
                // Lock session for 5 minutes
                await _redisSessionService.LockSessionAsync(sessionId, LockoutDurationSeconds, cancellationToken);
                _logger.LogWarning(
                    "Login STEP 2: Locked session {SessionId} after {Attempts} failed attempts",
                    sessionId, attemptCount);
                throw new InvalidOperationException("Too many failed attempts. Please try again in 5 minutes.");
            }

            // Get user from database
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Login STEP 2: User '{UserId}' not found", userId);
                throw new UnauthorizedAccessException("User not found");
            }

            // Verify 2FA code against secret from Redis
            if (!_twoFactorService.VerifyCode(totpSecret, code))
            {
                // Increment attempt counter
                var newAttemptCount = await _redisSessionService.IncrementFailedAttemptsAsync(sessionId, cancellationToken);
                _logger.LogWarning(
                    "Login STEP 2: Invalid 2FA code for user '{UserId}', session {SessionId} - " +
                    "attempt {AttemptNumber} of {MaxAttempts}",
                    userId, sessionId, newAttemptCount, MaxFailedAttempts);

                if (newAttemptCount >= MaxFailedAttempts)
                {
                    await _redisSessionService.LockSessionAsync(sessionId, LockoutDurationSeconds, cancellationToken);
                    throw new UnauthorizedAccessException(
                        $"Invalid 2FA code (attempt {newAttemptCount}/{MaxFailedAttempts}). " +
                        "Account locked for 5 minutes.");
                }

                throw new UnauthorizedAccessException(
                    $"Invalid 2FA code (attempt {newAttemptCount}/{MaxFailedAttempts})");
            }

            _logger.LogInformation(
                "Login STEP 2: 2FA code verified for user '{UserId}' - issuing final token",
                userId);

            // ✅ Code is valid! Generate final token

            // 🔐 CLEANUP: Delete session from Redis (successful verification)
            await _redisSessionService.DeleteSessionAsync(sessionId, cancellationToken);
            _logger.LogInformation("Cleaned up Redis session {SessionId}", sessionId);

            // Generate final JWT token (60 min)
            var finalToken = _jwtTokenGenerator.GenerateToken(user, isTempToken: false);
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds();

            _logger.LogInformation("User '{UserId}' authenticated with 2FA", userId);

            return new UserAuthCompleteResponse(
                Token: finalToken,
                UserId: user.Id,
                Username: user.UserName,
                ExpiresAt: expiresAt,
                Role: user.Role.ToString());
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            // 🔐 CLEANUP: Delete session from Redis on error
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                try
                {
                    await _redisSessionService.DeleteSessionAsync(sessionId, cancellationToken);
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Error cleaning up Redis session {SessionId}", sessionId);
                }
            }

            _logger.LogError(ex, "Login STEP 2 error for user '{UserId}'", userId);
            throw new InvalidOperationException("2FA verification failed", ex);
        }
    }

    /// <summary>
    /// OPTIONAL: User wants to enable 2FA on their account (after login)
    /// Generates QR code, manual key, and backup codes
    /// Does NOT save to database yet - waiting for verification
    /// </summary>
    public async Task<UserTwoFactorSetupResponse> Setup2FAGenerateAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // TODO: Implement 2FA setup generation
            throw new InvalidOperationException(
                "Setup2FAGenerateAsync not yet implemented. " +
                "Will be implemented in next iteration.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "2FA setup generation error for user '{UserId}'", userId);
            throw new InvalidOperationException("2FA setup failed", ex);
        }
    }

    /// <summary>
    /// OPTIONAL: User confirms 2FA setup with verification code
    /// Encrypts and saves TOTP secret to database
    /// 2FA is now enabled
    /// </summary>
    public async Task<UserTwoFactorEnableResponse> Setup2FAEnableAsync(
        Guid userId,
        string sessionId,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        try
        {
            // TODO: Implement 2FA enable
            throw new InvalidOperationException(
                "Setup2FAEnableAsync not yet implemented. " +
                "Will be implemented in next iteration.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "2FA enable error for user '{UserId}'", userId);
            throw new InvalidOperationException("2FA enable failed", ex);
        }
    }

    /// <summary>
    /// OPTIONAL: User disables 2FA on their account
    /// Requires current 2FA code as security verification
    /// </summary>
    public async Task<UserTwoFactorDisableResponse> DisableTwoFactorAsync(
        Guid userId,
        string code,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        try
        {
            // TODO: Implement 2FA disable
            throw new InvalidOperationException(
                "DisableTwoFactorAsync not yet implemented. " +
                "Will be implemented in next iteration.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "2FA disable error for user '{UserId}'", userId);
            throw new InvalidOperationException("2FA disable failed", ex);
        }
    }

    /// <summary>
    /// Check 2FA status for user
    /// Returns whether 2FA is enabled and number of backup codes remaining
    /// </summary>
    public async Task<UserTwoFactorStatusResponse> Get2FAStatusAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
                throw new UnauthorizedAccessException("User not found");

            int? backupCodesCount = null;
            if (user.TwoFactorEnabled && !string.IsNullOrWhiteSpace(user.BackupCodes))
            {
                var codes = JsonSerializer.Deserialize<string[]>(user.BackupCodes) ?? [];
                backupCodesCount = codes.Length;
            }

            _logger.LogInformation("2FA status check for user '{UserId}': 2FA enabled={TwoFactorEnabled}, backup codes={BackupCodesCount}",
                userId, user.TwoFactorEnabled, backupCodesCount);

            return new UserTwoFactorStatusResponse(
                TwoFactorEnabled: user.TwoFactorEnabled,
                RemainingBackupCodes: backupCodesCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "2FA status check error for user '{UserId}'", userId);
            throw new InvalidOperationException("Failed to get 2FA status", ex);
        }
    }

    /// <summary>
    /// Validate JWT token and extract all claims
    /// Used by controller to extract user data from temp tokens
    /// </summary>
    public Dictionary<string, string>? ValidateTokenAndExtractClaims(string token)
    {
        try
        {
            return _jwtTokenGenerator.ValidateTokenAndGetClaims(token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return null;
        }
    }
}
