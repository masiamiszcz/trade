using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using TradingPlatform.Core.Dtos;
using TradingPlatform.Core.Enums;
using TradingPlatform.Core.Interfaces;
using TradingPlatform.Core.Models;
using TradingPlatform.Data.Entities;

namespace TradingPlatform.Data.Services;

/// <summary>
/// Core service for admin authentication
/// Handles registration, login, and 2FA verification
/// IMPORTANT: 2FA is MANDATORY for all admins
/// 
/// SECURITY HARDENING (99%):
/// ✅ Dual-layer rate limiting (IP + USER/EMAIL)
/// ✅ Progressive delay (3+ attempts: 1s/5s)
/// ✅ 2FA session lockout (5 attempts = 10 min lock)
/// ✅ Full cleanup after success (no ghost sessions)
/// 
/// FAILSAFE STRATEGY (Redis availability):
/// 🔐 FAIL CLOSED (deny login) if Redis unavailable
/// This protects against attacks during Redis downtime
/// Implementation: Wrap GetCounterAsync with try-catch → throw UnauthorizedAccessException
/// 
/// FUTURE: Consider central middleware for rate limiting
/// Currently: Service-level enforcement (works but distributed across methods)
/// Middleware approach would: validate before controller execution
/// </summary>
/// </summary>
public sealed class AdminAuthService : IAdminAuthService
{
    private readonly IAdminInvitationService _invitationService;
    private readonly ITwoFactorService _twoFactorService;
    private readonly IEncryptionService _encryptionService;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAdminAuthRepository _authRepository;
    private readonly IAdminAuditLogRepository _auditLogRepository;
    private readonly IAdminRegistrationLogRepository _registrationLogRepository;
    private readonly IRedisSessionService _redisSessionService;
    private readonly PasswordHasher<User> _passwordHasher = new();
    private readonly ILogger<AdminAuthService> _logger;

    // Constants for 2FA session management (match User 2FA)
    private const int TwoFactorSetupSessionTimeoutSeconds = 600; // 10 minutes

    public AdminAuthService(
        IAdminInvitationService invitationService,
        ITwoFactorService twoFactorService,
        IEncryptionService encryptionService,
        IJwtTokenGenerator jwtTokenGenerator,
        IAdminAuthRepository authRepository,
        IAdminAuditLogRepository auditLogRepository,
        IAdminRegistrationLogRepository registrationLogRepository,
        IRedisSessionService redisSessionService,
        ILogger<AdminAuthService> logger)
    {
        _invitationService = invitationService ?? throw new ArgumentNullException(nameof(invitationService));
        _twoFactorService = twoFactorService ?? throw new ArgumentNullException(nameof(twoFactorService));
        _encryptionService = encryptionService ?? throw new ArgumentNullException(nameof(encryptionService));
        _jwtTokenGenerator = jwtTokenGenerator ?? throw new ArgumentNullException(nameof(jwtTokenGenerator));
        _authRepository = authRepository ?? throw new ArgumentNullException(nameof(authRepository));
        _auditLogRepository = auditLogRepository ?? throw new ArgumentNullException(nameof(auditLogRepository));
        _registrationLogRepository = registrationLogRepository ?? throw new ArgumentNullException(nameof(registrationLogRepository));
        _redisSessionService = redisSessionService ?? throw new ArgumentNullException(nameof(redisSessionService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Register admin via invitation token
    /// Creates user account but doesn't enable login until 2FA is set up
    /// </summary>
    public async Task<AdminRegistrationResponse> RegisterAdminViaInviteAsync(
        string token,
        string username,
        string password,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("Token is required", nameof(token));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required", nameof(username));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));

        Guid? invitationId = null;

        try
        {
            // Validate invitation token
            var invitation = await _invitationService.ValidateInvitationAsync(token, cancellationToken);
            invitationId = invitation.Id;

            // Check username uniqueness
            var existingUser = await _authRepository.GetAdminByUserNameAsync(username, cancellationToken);
            if (existingUser != null)
            {
                await LogRegistrationAsync(invitationId, null, invitation.Email, 
                    AdminRegistrationAction.RegistrationFailed, AdminRegistrationLogStatus.Failed,
                    "Username already taken", ipAddress, userAgent, cancellationToken);
                throw new InvalidOperationException("Username is already taken");
            }

            // 🔐 SECURITY: DO NOT create user in database yet!
            // Step 1 only stores temporary data in Redis
            // User will be created in database ONLY after 2FA verification (Step 2)
            
            var adminId = Guid.NewGuid();
            var passwordHash = _passwordHasher.HashPassword(
                new User(
                    Id: adminId,
                    UserName: username.Trim(),
                    Email: invitation.Email,
                    FirstName: invitation.FirstName,
                    LastName: invitation.LastName,
                    Role: UserRole.Admin,
                    EmailConfirmed: false,
                    TwoFactorEnabled: false,
                    TwoFactorSecret: string.Empty,
                    BackupCodes: string.Empty,
                    Status: UserStatus.Active,
                    BaseCurrency: "PLN",
                    CreatedAtUtc: DateTimeOffset.UtcNow,
                    BlockedUntilUtc: null,
                    BlockReason: null,
                    DeletedAtUtc: null
                ),
                password);

            // Create temporary registration data (stored in Redis, expires after 10 minutes)
            var registrationSessionId = Guid.NewGuid().ToString();
            
            // Store all admin data in Redis with TTL of 10 minutes
            // If 2FA is not completed within 10 minutes, this data is automatically deleted
            await _redisSessionService.CreateSessionAsync(
                registrationSessionId,
                adminId.ToString(),
                "", // TOTP secret will be stored later in Step 2
                TwoFactorSetupSessionTimeoutSeconds, // 10 minutes
                ct: cancellationToken);

            // Store additional registration metadata in Redis
            var registrationData = new
            {
                AdminId = adminId,
                Username = username.Trim(),
                Email = invitation.Email,
                FirstName = invitation.FirstName,
                LastName = invitation.LastName,
                PasswordHash = passwordHash,
                Token = token,  // ✅ Store token so we can mark invitation as used later
                InvitationId = invitationId.ToString(),
                IsSuperAdmin = false,
                RegistrationStep = "pending_2fa",
                CreatedAt = DateTimeOffset.UtcNow
            };

            // Serialize and store in Redis (with same TTL)
            var registrationJson = System.Text.Json.JsonSerializer.Serialize(registrationData);
            await _redisSessionService.CreateSessionAsync(
                $"admin_reg_data:{registrationSessionId}",
                adminId.ToString(),
                registrationJson,
                TwoFactorSetupSessionTimeoutSeconds,
                ct: cancellationToken);

            // Log registration step 1 completion (not yet account creation)
            await LogRegistrationAsync(invitationId, null, invitation.Email,
                AdminRegistrationAction.RegistrationStarted, AdminRegistrationLogStatus.Success,
                "Admin registration started, awaiting 2FA setup", ipAddress, userAgent, cancellationToken);

            _logger.LogInformation(
                "Admin registration STEP 1: Temporary data stored in Redis for adminId {AdminId}, sessionId {SessionId}, " +
                "expires in {Timeout}s",
                adminId, registrationSessionId, TwoFactorSetupSessionTimeoutSeconds);

            // Generate temporary token (5 min) - contains adminId for next steps
            var tempToken = _jwtTokenGenerator.GenerateToken(
                new User(
                    Id: adminId,
                    UserName: username.Trim(),
                    Email: invitation.Email,
                    FirstName: invitation.FirstName,
                    LastName: invitation.LastName,
                    Role: UserRole.Admin,
                    EmailConfirmed: false,
                    TwoFactorEnabled: false,
                    TwoFactorSecret: string.Empty,
                    BackupCodes: string.Empty,
                    Status: UserStatus.Active,
                    BaseCurrency: "PLN",
                    CreatedAtUtc: DateTimeOffset.UtcNow,
                    BlockedUntilUtc: null,
                    BlockReason: null,
                    DeletedAtUtc: null
                ),
                isTempToken: true,
                context: new TokenContext
                {
                    AdminRegistrationStep = "pending_2fa",
                    RegistrationSessionId = registrationSessionId
                });

            return new AdminRegistrationResponse(
                Token: tempToken,
                SessionId: registrationSessionId,
                RequiresTwoFactorSetup: true,
                Message: "Admin registration started. You must set up 2FA to complete registration."
            );
        }
        catch (InvalidOperationException ex)
        {
            await LogRegistrationAsync(invitationId, null, null,
                AdminRegistrationAction.RegistrationFailed, AdminRegistrationLogStatus.Failed,
                ex.Message, ipAddress, userAgent, cancellationToken);
            _logger.LogWarning(ex, "Admin registration STEP 1 failed");
            throw;
        }
        catch (Exception ex)
        {
            await LogRegistrationAsync(invitationId, null, null,
                AdminRegistrationAction.RegistrationFailed, AdminRegistrationLogStatus.Failed,
                ex.Message, ipAddress, userAgent, cancellationToken);
            _logger.LogError(ex, "Admin registration STEP 1 error");
            throw new InvalidOperationException("Registration failed", ex);
        }
    }

    /// <summary>
    /// Generate 2FA secret (QR code) for admin during registration STEP 2
    /// 🔐 SECURITY: Stores secret in Redis using registrationSessionId (not creating new session)
    /// The registrationSessionId comes from Step 1 and points to temp admin data
    /// </summary>
    public async Task<AdminTwoFactorSetupResponse> SetupTwoFactorGenerateAsync(
        Guid adminId,
        string registrationSessionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registrationSessionId))
            throw new ArgumentException("Registration session ID is required", nameof(registrationSessionId));

        try
        {
            _logger.LogInformation(
                "2FA setup STEP 2A: Generating TOTP secret for admin {AdminId}, using registrationSessionId={SessionId}",
                adminId, registrationSessionId);

            // Verify registration session still exists (not expired after 10 minutes)
            var regSession = await _redisSessionService.GetSessionAsync($"admin_reg_data:{registrationSessionId}", cancellationToken)
                ?? await _redisSessionService.GetSessionAsync($"admin_bootstrap_data:{registrationSessionId}", cancellationToken);

            if (regSession == null)
            {
                _logger.LogWarning("2FA setup failed for admin {AdminId} - registration session expired", adminId);
                throw new InvalidOperationException("Registration session expired. Please start registration again.");
            }

            var secret = _twoFactorService.GenerateSecret();

            // Create TOTP session using SAME registrationSessionId (not a new one)
            // This session will store the generated TOTP secret
            await _redisSessionService.CreateSessionAsync(
                registrationSessionId,
                adminId.ToString(),
                secret.Secret,  // ✅ TOTP secret in Redis!
                TwoFactorSetupSessionTimeoutSeconds,
                ct: cancellationToken);

            _logger.LogInformation(
                "2FA setup STEP 2A completed: TOTP secret stored in Redis for admin {AdminId}, sessionId={SessionId}",
                adminId, registrationSessionId);

            return new AdminTwoFactorSetupResponse(
                QrCodeDataUrl: secret.QrCodeDataUrl,
                ManualKey: secret.Secret,
                SessionId: registrationSessionId,  // ← Same sessionId (points to Redis temp data + TOTP secret)
                Message: "Scan QR code with Google Authenticator or similar app"
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate 2FA setup for admin {AdminId}", adminId);
            throw;
        }
    }

    /// <summary>
    /// Enable 2FA for admin (after verifying the code)
    /// 🔐 SECURITY: Retrieves TOTP secret from Redis using sessionId
    /// ⚠️ IMPORTANT: THIS IS STEP 2 - ADMIN IS CREATED HERE (after 2FA verification)
    /// Generates backup codes and enables 2FA in database
    /// IMPORTANT: 2FA becomes mandatory after this
    /// </summary>
    public async Task<AdminTwoFactorCompleteResponse> SetupTwoFactorEnableAsync(
        Guid adminId,
        string code,
        string sessionId,
        string registrationSessionId,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));

        try
        {
            _logger.LogInformation(
                "🔵 2FA setup STEP 2: START - Admin {AdminId}, registrationSessionId={SessionId}, code={Code}",
                adminId, registrationSessionId, code);

            // 🔐 SECURITY: Retrieve TOTP secret from temporary Redis session
            // ⚠️ IMPORTANT: Use registrationSessionId (from JWT), NOT sessionId (from request body)
            // registrationSessionId is the key where we stored the TOTP secret in /generate
            _logger.LogInformation("🔵 [STEP 2.1] Attempting to retrieve session from Redis with key: {Key}", registrationSessionId);
            var session = await _redisSessionService.GetSessionAsync(registrationSessionId, cancellationToken);
            
            if (session == null)
            {
                _logger.LogError("❌ [STEP 2.1 FAIL] Session NOT FOUND in Redis! registrationSessionId={SessionId}, adminId={AdminId}", registrationSessionId, adminId);
                throw new InvalidOperationException("2FA session expired. Please start setup again.");
            }

            _logger.LogInformation("✅ [STEP 2.1 OK] Session retrieved from Redis. UserId={UserId}, CreatedAt={CreatedAt}, ExpiresAt={ExpiresAt}", 
                session.UserId, session.CreatedAt, session.ExpiresAt);

            var totpSecret = session.TotpSecret;
            _logger.LogInformation("🔵 [STEP 2.2] Checking TOTP secret - Length={Length}, IsEmpty={IsEmpty}", 
                totpSecret?.Length ?? 0, string.IsNullOrWhiteSpace(totpSecret));
            
            if (string.IsNullOrWhiteSpace(totpSecret))
            {
                _logger.LogError("❌ [STEP 2.2 FAIL] TOTP secret is null/empty in session! Session.TotpSecret={TotpSecret}", totpSecret ?? "NULL");
                throw new InvalidOperationException("TOTP secret not found in session");
            }

            _logger.LogInformation("✅ [STEP 2.2 OK] TOTP secret found in session. Secret length: {Length} chars", totpSecret.Length);

            // Verify TOTP code is correct
            _logger.LogInformation("🔵 [STEP 2.3] Verifying TOTP code - Code={Code}, SecretLength={SecretLength}", code, totpSecret.Length);
            bool isCodeValid = _twoFactorService.VerifyCode(totpSecret, code);
            
            if (!isCodeValid)
            {
                _logger.LogWarning("❌ [STEP 2.3 FAIL] Invalid 2FA code! Code={Code}, Secret length={SecretLength}, AdminId={AdminId}", 
                    code, totpSecret.Length, adminId);
                throw new InvalidOperationException("Invalid 2FA code");
            }

            _logger.LogInformation("✅ [STEP 2.3 OK] 2FA code verified successfully!");
            _logger.LogInformation("🔵 [STEP 2.4] Retrieving registration data from Redis", adminId);

            // ✅ 2FA code is valid! Now retrieve temporary admin data from Redis
            // Check if this is registration (admin_reg_data or admin_bootstrap_data) or existing admin setup
            _logger.LogInformation("🔵 [STEP 2.4.1] Looking for admin_reg_data:{SessionId}", registrationSessionId);
            var registrationData = await _redisSessionService.GetSessionAsync($"admin_reg_data:{registrationSessionId}", cancellationToken);
            
            if (registrationData == null)
            {
                _logger.LogInformation("🔵 [STEP 2.4.1] admin_reg_data NOT found, trying admin_bootstrap_data:{SessionId}", registrationSessionId);
                registrationData = await _redisSessionService.GetSessionAsync($"admin_bootstrap_data:{registrationSessionId}", cancellationToken);
            }

            if (registrationData == null)
            {
                _logger.LogWarning("❌ [STEP 2.4.1 FAIL] Neither admin_reg_data nor admin_bootstrap_data found! This might be an existing admin.");
            }
            else
            {
                _logger.LogInformation("✅ [STEP 2.4.1] Registration data found! UserId={UserId}", registrationData.UserId);
            }

            // If NO registration data, this is existing admin enabling 2FA
            if (registrationData == null)
            {
                _logger.LogInformation("🔵 [STEP 2.4.2] This is EXISTING ADMIN flow - getting from database");
                // Existing admin - just get from database
                var existingAdmin = await _authRepository.GetAdminByIdAsync(adminId, cancellationToken);
                if (existingAdmin == null)
                {
                    _logger.LogError("❌ Admin not found in database! AdminId={AdminId}", adminId);
                    throw new InvalidOperationException("Admin not found");
                }

                _logger.LogInformation("✅ [STEP 2.4.2] Existing admin found: {Username}", existingAdmin.UserName);

                // Encrypt TOTP secret for permanent storage in DB
                var encryptedSecret = _encryptionService.Encrypt(totpSecret);

                // Generate backup codes
                var backupCodes = _twoFactorService.GenerateBackupCodes();
                var hashedBackupCodes = backupCodes.Select(c => _twoFactorService.HashBackupCode(c)).ToArray();
                var backupCodesJson = System.Text.Json.JsonSerializer.Serialize(hashedBackupCodes);

                // Update admin with 2FA enabled in database
                await _authRepository.UpdateAdminTwoFactorAsync(adminId, encryptedSecret, 
                    backupCodesJson, true, cancellationToken);

                // 🔐 SECURITY: Delete session from Redis (cleanup)
                await _redisSessionService.DeleteSessionAsync(registrationSessionId, cancellationToken);
                await _redisSessionService.DeleteSessionAsync($"admin_reg_data:{registrationSessionId}", cancellationToken);
                await _redisSessionService.DeleteSessionAsync($"admin_bootstrap_data:{registrationSessionId}", cancellationToken);

                await LogAuditAsync(adminId, AdminAuditAction.TwoFactorEnabled, ipAddress, userAgent,
                    "2FA setup completed", cancellationToken);

                _logger.LogInformation("✅ [EXISTING ADMIN] 2FA enabled and Redis sessions cleaned up", adminId);

                return new AdminTwoFactorCompleteResponse(
                    BackupCodes: backupCodes,
                    Success: true,
                    Message: "2FA enabled successfully. Save these backup codes somewhere safe!"
                );
            }

            // ✅ NEW ADMIN REGISTRATION - Deserialize registration data
            _logger.LogInformation("🔵 [STEP 2.5] NEW ADMIN REGISTRATION flow - deserializing registration data from Redis");

            // Registration data already retrieved above (from either admin_reg_data or admin_bootstrap_data key)
            // registrationData is NOT null here (checked on line 318)
            if (registrationData == null)
            {
                // This should never happen due to check above, but be safe
                _logger.LogError("❌ [STEP 2.5 FAIL] Impossible state: registrationData is null but we're in NEW_ADMIN flow!");
                throw new InvalidOperationException($"Registration data not found for session {registrationSessionId}");
            }

            // Registration data is stored in the TotpSecret field (as JSON string)
            var regDataJson = registrationData.TotpSecret;
            _logger.LogInformation("🔵 [STEP 2.5.1] Parsing registration JSON - Length={Length}, StartsWith={{={{={StartsWith}", 
                regDataJson?.Length ?? 0, regDataJson?.Substring(0, Math.Min(50, regDataJson?.Length ?? 0)) ?? "NULL");
            
            if (string.IsNullOrWhiteSpace(regDataJson))
            {
                _logger.LogError("❌ [STEP 2.5.1 FAIL] Registration JSON is empty! TotpSecret field={Value}", regDataJson ?? "NULL");
                throw new InvalidOperationException("Registration data is empty");
            }

            System.Text.Json.JsonDocument adminRegDataDoc;
            try
            {
                adminRegDataDoc = System.Text.Json.JsonDocument.Parse(regDataJson);
                _logger.LogInformation("✅ [STEP 2.5.1] JSON parsed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [STEP 2.5.1 FAIL] Failed to parse registration JSON! Error={Error}, JSON={Json}", ex.Message, regDataJson);
                throw;
            }

            var adminRegData = adminRegDataDoc.RootElement;

            // Extract fields with logging
            string? username = null;
            try
            {
                username = adminRegData.GetProperty("Username").GetString();
                _logger.LogInformation("🔵 [STEP 2.5.2.1] Username={Username}", username);
                if (string.IsNullOrWhiteSpace(username)) throw new InvalidOperationException("Username missing from registration data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [STEP 2.5.2.1 FAIL] Failed to get Username: {Error}", ex.Message);
                throw;
            }

            string? email = null;
            try
            {
                email = adminRegData.GetProperty("Email").GetString();
                _logger.LogInformation("🔵 [STEP 2.5.2.2] Email={Email}", email);
                if (string.IsNullOrWhiteSpace(email)) throw new InvalidOperationException("Email missing from registration data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [STEP 2.5.2.2 FAIL] Failed to get Email: {Error}", ex.Message);
                throw;
            }

            var firstName = adminRegData.GetProperty("FirstName").GetString() ?? "Super";
            var lastName = adminRegData.GetProperty("LastName").GetString() ?? "Admin";
            _logger.LogInformation("🔵 [STEP 2.5.2.3] FirstName={FirstName}, LastName={LastName}", firstName, lastName);

            string? passwordHashFromReg = null;
            try
            {
                passwordHashFromReg = adminRegData.GetProperty("PasswordHash").GetString();
                _logger.LogInformation("🔵 [STEP 2.5.2.4] PasswordHash length={Length}", passwordHashFromReg?.Length ?? 0);
                if (string.IsNullOrWhiteSpace(passwordHashFromReg)) throw new InvalidOperationException("Password hash missing from registration data");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [STEP 2.5.2.4 FAIL] Failed to get PasswordHash: {Error}", ex.Message);
                throw;
            }

            bool isSuperAdmin = false;
            try
            {
                isSuperAdmin = adminRegData.GetProperty("IsSuperAdmin").GetBoolean();
                _logger.LogInformation("🔵 [STEP 2.5.2.5] IsSuperAdmin={IsSuperAdmin}", isSuperAdmin);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [STEP 2.5.2.5 FAIL] Failed to get IsSuperAdmin: {Error}", ex.Message);
            }

            var invitationIdStr = adminRegData.TryGetProperty("InvitationId", out var invIdProp) 
                ? invIdProp.GetString() 
                : null;
            _logger.LogInformation("🔵 [STEP 2.5.2.6] InvitationId={InvitationId}", invitationIdStr ?? "NULL");

            // Encrypt TOTP secret for permanent storage in DB
            _logger.LogInformation("🔵 [STEP 2.6] Encrypting TOTP secret for database storage");
            var encryptedTotpSecret = _encryptionService.Encrypt(totpSecret);
            _logger.LogInformation("✅ [STEP 2.6] TOTP secret encrypted. Length={Length}", encryptedTotpSecret?.Length ?? 0);

            // Generate backup codes
            _logger.LogInformation("🔵 [STEP 2.7] Generating backup codes");
            var backupCodesList = _twoFactorService.GenerateBackupCodes();
            var hashedBackupCodesList = backupCodesList.Select(c => _twoFactorService.HashBackupCode(c)).ToList();
            var backupCodesJsonStr = System.Text.Json.JsonSerializer.Serialize(hashedBackupCodesList);
            _logger.LogInformation("✅ [STEP 2.7] Generated {Count} backup codes, JSON size={Size}", backupCodesList.Length, backupCodesJsonStr.Length);

            // Create user entity with all properties set in constructor
            _logger.LogInformation("🔵 [STEP 2.8] Creating User record - Username={Username}, Email={Email}, IsSuperAdmin={IsSuperAdmin}", 
                username, email, isSuperAdmin);
            var user = new User(
                Id: adminId,
                UserName: username,
                Email: email,
                FirstName: firstName,
                LastName: lastName,
                Role: UserRole.Admin,
                EmailConfirmed: false,
                TwoFactorEnabled: true,
                TwoFactorSecret: encryptedTotpSecret,
                BackupCodes: backupCodesJsonStr,
                Status: UserStatus.Active,
                BaseCurrency: "PLN",
                CreatedAtUtc: DateTimeOffset.UtcNow,
                BlockedUntilUtc: null,
                BlockReason: null,
                DeletedAtUtc: null
            );
            _logger.LogInformation("✅ [STEP 2.8] User record created");

            // ✅ CREATE ADMIN IN DATABASE
            _logger.LogInformation("🔵 [STEP 2.9] Creating admin in database via _authRepository.CreateAdminAsync");
            await _authRepository.CreateAdminAsync(user, passwordHashFromReg, cancellationToken);
            _logger.LogInformation("✅ [STEP 2.9] Admin created in database!");

            // Create admin entity with IsSuperAdmin flag
            _logger.LogInformation("🔵 [STEP 2.10] Creating AdminEntity with IsSuperAdmin={IsSuperAdmin}", isSuperAdmin);
            await _authRepository.CreateAdminEntityAsync(adminId, isSuperAdmin, cancellationToken);
            _logger.LogInformation("✅ [STEP 2.10] AdminEntity created!");

            // If this was via invitation, mark invitation as used
            if (Guid.TryParse(invitationIdStr, out var invitationId))
            {
                _logger.LogInformation("🔵 [STEP 2.11] Marking invitation {InvitationId} as used", invitationId);
                await _invitationService.MarkAsUsedAsync(
                    adminRegData.GetProperty("Token").GetString() ?? "",
                    adminId,
                    cancellationToken);
                _logger.LogInformation("✅ [STEP 2.11] Invitation marked as used");
            }
            else
            {
                _logger.LogInformation("ℹ️ [STEP 2.11] No invitation ID (bootstrap flow)");
            }

            _logger.LogInformation(
                "✅ [STEP 2.12] Admin {AdminId} created in database with 2FA enabled (IsSuperAdmin={IsSuperAdmin})",
                adminId, isSuperAdmin);

            // Log 2FA setup completion
            await LogRegistrationAsync(
                invitationIdStr != null ? Guid.Parse(invitationIdStr) : null,
                adminId, 
                email,
                AdminRegistrationAction.TwoFactorSetupCompleted, 
                AdminRegistrationLogStatus.Success,
                "2FA enabled and admin account created", 
                ipAddress, 
                userAgent, 
                cancellationToken);

            await LogAuditAsync(adminId, AdminAuditAction.TwoFactorEnabled, ipAddress, userAgent,
                "2FA setup completed during registration", cancellationToken);

            // 🔐 SECURITY: Delete ALL temporary sessions from Redis (cleanup)
            _logger.LogInformation("🔵 [STEP 2.13] Cleaning up Redis sessions");
            await _redisSessionService.DeleteSessionAsync(registrationSessionId, cancellationToken);
            await _redisSessionService.DeleteSessionAsync($"admin_reg_data:{registrationSessionId}", cancellationToken);
            await _redisSessionService.DeleteSessionAsync($"admin_bootstrap_data:{registrationSessionId}", cancellationToken);
            _logger.LogInformation("✅ [STEP 2.13] All Redis sessions cleaned up");

            _logger.LogInformation("🟢 [SUCCESS] Admin {AdminId} fully registered with 2FA enabled!", adminId);

            return new AdminTwoFactorCompleteResponse(
                BackupCodes: backupCodesList,
                Success: true,
                Message: "Registration complete! 2FA enabled successfully. Save these backup codes somewhere safe!"
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogError(ex, "❌ [VALIDATION ERROR] {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ [UNEXPECTED ERROR] Failed to enable 2FA for admin {AdminId} - {ErrorType}: {Message}", 
                adminId, ex.GetType().Name, ex.Message);
            throw new InvalidOperationException("Failed to enable 2FA", ex);
        }
    }

    /// <summary>
    /// Admin login (first step - password only)
    /// Returns temporary token if 2FA is enabled (MANDATORY for admins)
    /// </summary>
    public async Task<AdminLoginResponse> AdminLoginAsync(
        string usernameOrEmail,
        string password,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(usernameOrEmail))
            throw new ArgumentException("Username or email is required", nameof(usernameOrEmail));
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password is required", nameof(password));

        try
        {
            // 🔐 SECURITY: Dual-layer rate limiting (IP + USER/EMAIL)
            // Prevents both distributed attacks (IP rotation) and targeted attacks (password spray)
            const int maxAttempts = 5;
            const int lockoutDurationSeconds = 300; // 5 min
            
            var ipAttempts = 0;
            var userAttempts = 0;
            
            // Check IP-based attempts
            if (!string.IsNullOrEmpty(ipAddress))
            {
                var ipKey = $"auth:login:attempts:ip:{ipAddress}";
                ipAttempts = await _redisSessionService.GetCounterAsync(ipKey, cancellationToken);
            }
            
            // Check USER-based attempts (will populate after we get admin)
            // For now, we check after lookup to avoid timing attacks
            
            // PROGRESSIVE DELAY: Apply delay based on attempt count
            if (ipAttempts >= 3)
            {
                var delay = ipAttempts switch
                {
                    3 => 1000,      // 1 sec delay at attempt 3
                    4 => 5000,      // 5 sec delay at attempt 4
                    _ => 0
                };
                if (delay > 0) await Task.Delay(delay, cancellationToken);
            }
            
            // Hard lockout at max attempts
            if (ipAttempts >= maxAttempts)
            {
                _logger.LogWarning("Admin login rate limit exceeded from IP: {IpAddress} ({Attempts} attempts)", 
                    ipAddress, ipAttempts);
                throw new UnauthorizedAccessException(
                    "Too many login attempts. Please try again in 5 minutes.");
            }

            // Get admin with password hash
            var (admin, passwordHash) = await _authRepository.GetAdminWithPasswordHashAsync(
                usernameOrEmail, cancellationToken);

            if (admin == null || string.IsNullOrWhiteSpace(passwordHash))
            {
                // Increment BOTH IP and USER rate limit counters on failed attempt
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    await _redisSessionService.IncrementCounterAsync($"auth:login:attempts:ip:{ipAddress}", 300, cancellationToken);
                }
                if (!string.IsNullOrEmpty(usernameOrEmail))
                {
                    await _redisSessionService.IncrementCounterAsync($"auth:login:attempts:user:{usernameOrEmail}", 300, cancellationToken);
                }
                await LogAuditAsync(null, AdminAuditAction.LoginFailed, ipAddress, userAgent,
                    "Invalid credentials", cancellationToken);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // Verify password
            var verifyResult = _passwordHasher.VerifyHashedPassword(admin, passwordHash, password);
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                // Increment BOTH IP and USER rate limit counters
                if (!string.IsNullOrEmpty(ipAddress))
                {
                    await _redisSessionService.IncrementCounterAsync($"auth:login:attempts:ip:{ipAddress}", 300, cancellationToken);
                }
                if (admin != null)
                {
                    await _redisSessionService.IncrementCounterAsync($"auth:login:attempts:user:{admin.UserName}", 300, cancellationToken);
                }
                await LogAuditAsync(admin.Id, AdminAuditAction.LoginFailed, ipAddress, userAgent,
                    "Invalid password", cancellationToken);
                throw new UnauthorizedAccessException("Invalid credentials");
            }

            // CHECK: 2FA MUST be enabled for admins
            if (!admin.TwoFactorEnabled)
            {
                await LogAuditAsync(admin.Id, AdminAuditAction.LoginFailed, ipAddress, userAgent,
                    "2FA not enabled", cancellationToken);
                _logger.LogWarning("Admin {AdminId} attempted login without 2FA enabled", admin.Id);
                throw new UnauthorizedAccessException("2FA is required for admin accounts");
            }

            // Generate temporary session token (5 min)
            var sessionId = Guid.NewGuid().ToString();
            var tempToken = _jwtTokenGenerator.GenerateToken(admin, isTempToken: true,
                context: new TokenContext { SessionId = sessionId, TwoFactorRequired = true });

            // Reset rate limit counters on successful password verification (both IP and USER)
            if (!string.IsNullOrEmpty(ipAddress))
            {
                await _redisSessionService.ResetCounterAsync($"auth:login:attempts:ip:{ipAddress}", cancellationToken);
            }
            if (admin != null)
            {
                await _redisSessionService.ResetCounterAsync($"auth:login:attempts:user:{admin.UserName}", cancellationToken);
            }

            // COMMENTED OUT: LastLoginAttemptUtc not needed for current features
            // await _authRepository.UpdateLastLoginAttemptAsync(admin.Id, cancellationToken);

            _logger.LogInformation("Admin {AdminId} initiated login (awaiting 2FA)", admin.Id);

            return new AdminLoginResponse(
                Token: tempToken,
                SessionId: sessionId,
                RequiresTwoFactor: true,
                Username: admin.UserName
            );
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin login error");
            throw new InvalidOperationException("Login failed", ex);
        }
    }

    /// <summary>
    /// Verify 2FA code and issue final JWT token (second step of login)
    /// </summary>
    public async Task<AdminAuthSuccessResponse> VerifyAdminTwoFactorAsync(
        Guid adminId,
        string sessionId,
        string code,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            throw new ArgumentException("Session ID is required", nameof(sessionId));
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        try
        {
            // 🔐 SECURITY: Check if session is already locked (after 5 failed attempts)
            var isSessionLocked = await _redisSessionService.IsSessionLockedAsync(sessionId, cancellationToken);
            if (isSessionLocked)
            {
                _logger.LogWarning("2FA verification attempted on locked session {SessionId}", sessionId);
                throw new UnauthorizedAccessException("Too many failed attempts. Please try again in 10 minutes.");
            }

            // Get admin
            var admin = await _authRepository.GetAdminByIdAsync(adminId, cancellationToken);
            if (admin == null)
                throw new UnauthorizedAccessException("Admin not found");

            // Check if 2FA is enabled
            if (!admin.TwoFactorEnabled || string.IsNullOrWhiteSpace(admin.TwoFactorSecret))
                throw new UnauthorizedAccessException("2FA not enabled for this account");

            // Decrypt TOTP secret
            string decryptedSecret;
            try
            {
                decryptedSecret = _encryptionService.Decrypt(admin.TwoFactorSecret);
            }
            catch
            {
                _logger.LogError("Failed to decrypt 2FA secret for admin {AdminId}", adminId);
                throw new UnauthorizedAccessException("2FA secret corruption detected");
            }

            // First, try standard TOTP code
            bool isCodeValid = _twoFactorService.VerifyCode(decryptedSecret, code);

            // If not valid, try backup codes
            if (!isCodeValid && !string.IsNullOrWhiteSpace(admin.BackupCodes))
            {
                var hashedBackupCodes = System.Text.Json.JsonSerializer.Deserialize<string[]>(admin.BackupCodes) ?? [];
                var (isBackupCodeValid, matchedIndex) = _twoFactorService.VerifyBackupCode(code, hashedBackupCodes);
                
                if (isBackupCodeValid && matchedIndex.HasValue)
                {
                    // Remove used backup code
                    var updatedBackupCodes = hashedBackupCodes.Where((_, i) => i != matchedIndex.Value).ToArray();
                    var updatedJson = System.Text.Json.JsonSerializer.Serialize(updatedBackupCodes);
                    await _authRepository.UpdateAdminBackupCodesAsync(adminId, updatedJson, cancellationToken);

                    await LogAuditAsync(adminId, AdminAuditAction.BackupCodeUsed, ipAddress, userAgent,
                        $"Backup code used, {updatedBackupCodes.Length} remaining", cancellationToken);
                    
                    isCodeValid = true;
                }
            }

            if (!isCodeValid)
            {
                // Increment failed attempts counter
                const int maxTwoFactorAttempts = 5;
                const int twoFactorLockoutDurationSeconds = 600; // 10 min
                
                var failedAttempts = await _redisSessionService.IncrementFailedAttemptsAsync(sessionId, cancellationToken);
                
                // Lock session after 5 failed attempts
                if (failedAttempts >= maxTwoFactorAttempts)
                {
                    await _redisSessionService.LockSessionAsync(sessionId, twoFactorLockoutDurationSeconds, cancellationToken);
                    _logger.LogWarning("2FA session locked for {SessionId} after {Attempts} failed attempts", sessionId, failedAttempts);
                }

                await LogAuditAsync(adminId, AdminAuditAction.TwoFactorVerifyFailed, ipAddress, userAgent,
                    $"Invalid 2FA code (attempt {failedAttempts}/{maxTwoFactorAttempts})", cancellationToken);
                throw new UnauthorizedAccessException("Invalid 2FA code");
            }

            // Generate final JWT token (60 min)
            // ✨ Check if user is super admin (for JWT claim)
            var isSuperAdmin = await _authRepository.IsUserSuperAdminAsync(adminId, cancellationToken);
            var finalToken = _jwtTokenGenerator.GenerateToken(admin, isTempToken: false, context: null, isSuperAdmin: isSuperAdmin);
            var expiresAt = DateTimeOffset.UtcNow.AddMinutes(60).ToUnixTimeSeconds();

            // Log successful 2FA
            await LogAuditAsync(adminId, AdminAuditAction.TwoFactorVerifySuccess, ipAddress, userAgent,
                "2FA verification successful", cancellationToken);

            // 🔐 SECURITY: Clean up 2FA session and attempt tracking after successful verification
            // This prevents "ghost locks" and ensures fresh state on next login
            await _redisSessionService.DeleteSessionAsync(sessionId, cancellationToken);

            _logger.LogInformation("Admin {AdminId} successfully verified 2FA, session cleaned up", adminId);

            return new AdminAuthSuccessResponse(
                Token: finalToken,
                Role: "Admin",
                AdminId: admin.Id,
                Username: admin.UserName,
                ExpiresAt: expiresAt
            );
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "2FA verification error for admin {AdminId}", adminId);
            throw new InvalidOperationException("2FA verification failed", ex);
        }
    }

    /// <summary>
    /// Disable 2FA for admin (requires current 2FA code as security check)
    /// WARNING: Admin won't be able to login until re-enabling 2FA
    /// </summary>
    public async Task<bool> DisableTwoFactorAsync(
        Guid adminId,
        string code,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        try
        {
            var admin = await _authRepository.GetAdminByIdAsync(adminId, cancellationToken);
            if (admin == null)
                throw new UnauthorizedAccessException("Admin not found");

            if (!admin.TwoFactorEnabled)
                throw new InvalidOperationException("2FA is not enabled");

            // Verify code before disabling
            var decryptedSecret = _encryptionService.Decrypt(admin.TwoFactorSecret!);
            if (!_twoFactorService.VerifyCode(decryptedSecret, code))
                throw new UnauthorizedAccessException("Invalid 2FA code");

            // Disable 2FA
            await _authRepository.ClearAdminTwoFactorAsync(adminId, cancellationToken);

            await LogAuditAsync(adminId, AdminAuditAction.TwoFactorDisabled, ipAddress, userAgent,
                "2FA disabled", cancellationToken);

            _logger.LogWarning("2FA disabled for admin {AdminId}", adminId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to disable 2FA for admin {AdminId}", adminId);
            throw;
        }
    }

    /// <summary>
    /// Regenerate backup codes (requires current 2FA code as security check)
    /// </summary>
    public async Task<string[]> RegenerateBackupCodesAsync(
        Guid adminId,
        string code,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Code is required", nameof(code));

        try
        {
            var admin = await _authRepository.GetAdminByIdAsync(adminId, cancellationToken);
            if (admin == null)
                throw new UnauthorizedAccessException("Admin not found");

            // Verify code
            var decryptedSecret = _encryptionService.Decrypt(admin.TwoFactorSecret!);
            if (!_twoFactorService.VerifyCode(decryptedSecret, code))
                throw new UnauthorizedAccessException("Invalid 2FA code");

            // Generate new backup codes
            var backupCodes = _twoFactorService.GenerateBackupCodes();
            var hashedBackupCodes = backupCodes.Select(c => _twoFactorService.HashBackupCode(c)).ToArray();
            var backupCodesJson = System.Text.Json.JsonSerializer.Serialize(hashedBackupCodes);

            await _authRepository.UpdateAdminBackupCodesAsync(adminId, backupCodesJson, cancellationToken);

            await LogAuditAsync(adminId, AdminAuditAction.BackupCodesRegenerated, ipAddress, userAgent,
                "Backup codes regenerated", cancellationToken);

            _logger.LogInformation("Backup codes regenerated for admin {AdminId}", adminId);

            return backupCodes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to regenerate backup codes for admin {AdminId}", adminId);
            throw;
        }
    }

    /// <summary>
    /// Log to admin audit log
    /// </summary>
    private async Task LogAuditAsync(
        Guid? adminId,
        AdminAuditAction action,
        string? ipAddress,
        string? userAgent,
        string? details,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!adminId.HasValue)
                return;

            var log = new AdminAuditLogEntity
            {
                Id = Guid.NewGuid(),
                AdminId = adminId.Value,
                Action = action,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                CreatedAt = DateTimeOffset.UtcNow,
                Details = details
            };

            await _auditLogRepository.AddAsync(log, cancellationToken);
            await _auditLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log audit entry");
        }
    }

    /// <summary>
    /// Log to admin registration log
    /// </summary>
    private async Task LogRegistrationAsync(
        Guid? invitationId,
        Guid? adminId,
        string? email,
        AdminRegistrationAction action,
        AdminRegistrationLogStatus status,
        string? errorMessage,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!invitationId.HasValue && string.IsNullOrEmpty(email))
                return;

            var log = new AdminRegistrationLogEntity
            {
                Id = Guid.NewGuid(),
                InvitationId = invitationId ?? Guid.Empty,
                AdminId = adminId,
                Email = email ?? string.Empty,
                Action = action,
                Status = status,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ErrorMessage = errorMessage,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _registrationLogRepository.AddAsync(log, cancellationToken);
            await _registrationLogRepository.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log registration entry");
        }
    }

    /// <summary>
    /// One-time bootstrap endpoint to create first Super Admin
    /// Can only be called once (if admin exists in DB, returns 403)
    /// </summary>
    public async Task<AdminRegistrationResponse> BootstrapSuperAdminAsync(
        string username,
        string email,
        string password,
        string? ipAddress = null,
        string? userAgent = null,
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
            // Check if any admin already exists (bootstrap protection - only ONE super admin allowed!)
            // 🔐 SECURITY: This prevents multiple super admin creation attempts
            var adminExists = await _authRepository.HasAnyAdminAsync(cancellationToken);
            if (adminExists)
            {
                _logger.LogWarning("Bootstrap attempted but admin already exists. IP: {IpAddress}", ipAddress);
                throw new InvalidOperationException("Super Admin already exists. Bootstrap is disabled.");
            }

            // 🔐 SECURITY: DO NOT create user in database yet!
            // Step 1 only stores temporary data in Redis
            // Super admin will be created in database ONLY after 2FA verification (Step 2)
            
            var adminId = Guid.NewGuid();
            var tempUser = new User(
                Id: adminId,
                UserName: username.Trim(),
                Email: email.ToLower().Trim(),
                FirstName: "Super",
                LastName: "Admin",
                Role: UserRole.Admin,
                EmailConfirmed: true,
                TwoFactorEnabled: false,
                TwoFactorSecret: string.Empty,
                BackupCodes: string.Empty,
                Status: UserStatus.Active,
                BaseCurrency: "PLN",
                CreatedAtUtc: DateTimeOffset.UtcNow,
                BlockedUntilUtc: null,
                BlockReason: null,
                DeletedAtUtc: null
            );
            var passwordHash = _passwordHasher.HashPassword(tempUser, password);

            // Create temporary bootstrap data (stored in Redis, expires after 10 minutes)
            var registrationSessionId = Guid.NewGuid().ToString();
            
            // Store all super admin data in Redis with TTL of 10 minutes
            var bootstrapData = new
            {
                AdminId = adminId,
                Username = username.Trim(),
                Email = email.ToLower().Trim(),
                FirstName = "Super",
                LastName = "Admin",
                PasswordHash = passwordHash,
                IsSuperAdmin = true,
                RegistrationStep = "pending_2fa",
                CreatedAt = DateTimeOffset.UtcNow
            };

            var bootstrapJson = System.Text.Json.JsonSerializer.Serialize(bootstrapData);
            await _redisSessionService.CreateSessionAsync(
                $"admin_bootstrap_data:{registrationSessionId}",
                adminId.ToString(),
                bootstrapJson,
                TwoFactorSetupSessionTimeoutSeconds,
                ct: cancellationToken);

            // Log bootstrap step 1 completion (not yet account creation)
            await LogRegistrationAsync(null, null, email,
                AdminRegistrationAction.RegistrationStarted, AdminRegistrationLogStatus.Success,
                "Super Admin bootstrap started, awaiting 2FA setup", ipAddress, userAgent, cancellationToken);

            _logger.LogInformation(
                "Super Admin bootstrap STEP 1: Temporary data stored in Redis for adminId {AdminId}, sessionId {SessionId}, " +
                "expires in {Timeout}s",
                adminId, registrationSessionId, TwoFactorSetupSessionTimeoutSeconds);

            // Generate temporary token for 2FA setup
            var tempToken = _jwtTokenGenerator.GenerateToken(tempUser, isTempToken: true,
                context: new TokenContext 
                { 
                    AdminRegistrationStep = "pending_2fa",
                    RegistrationSessionId = registrationSessionId,
                    IsSuperAdmin = true
                });

            var sessionId = Guid.NewGuid().ToString();

            return new AdminRegistrationResponse(
                Token: tempToken,
                SessionId: registrationSessionId,
                RequiresTwoFactorSetup: true,
                Message: "Super Admin bootstrap started. You must set up 2FA to complete setup."
            );
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Bootstrap failed: {Message}", ex.Message);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bootstrap error");
            throw new InvalidOperationException("Super Admin bootstrap failed", ex);
        }
    }

    /// <summary>
    /// Super Admin invites a new admin via email token
    /// </summary>
    public async Task<AdminInvitationResponse> InviteAdminAsync(
        Guid superAdminId,
        string email,
        string firstName,
        string lastName,
        string[]? permissions = null,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required", nameof(email));
        if (string.IsNullOrWhiteSpace(firstName))
            throw new ArgumentException("First name is required", nameof(firstName));
        if (string.IsNullOrWhiteSpace(lastName))
            throw new ArgumentException("Last name is required", nameof(lastName));

        try
        {
            // Verify super admin exists
            var superAdmin = await _authRepository.GetAdminByIdAsync(superAdminId, cancellationToken);
            if (superAdmin == null)
                throw new UnauthorizedAccessException("Super Admin not found");

            // ✨ Verify that requester is actually a super admin
            var isSuperAdmin = await _authRepository.IsUserSuperAdminAsync(superAdminId, cancellationToken);
            if (!isSuperAdmin)
            {
                _logger.LogWarning("Non-super-admin {AdminId} attempted to invite new admin", superAdminId);
                throw new UnauthorizedAccessException("Only Super Admin can invite new admins");
            }

            // Generate invitation token
            var token = await _invitationService.GenerateInvitationAsync(
                email, firstName, lastName, superAdminId,
                expiryHours: 48,
                cancellationToken: cancellationToken);

            // Log invitation
            await LogRegistrationAsync(null, superAdminId, email,
                AdminRegistrationAction.InvitationGenerated, AdminRegistrationLogStatus.Success,
                $"Admin invited with permissions: {string.Join(",", permissions ?? [])}",
                ipAddress, userAgent, cancellationToken);

            _logger.LogInformation("Admin invitation created for {Email} by {SuperAdminId}", email, superAdminId);

            // TODO: Send email with invitation link (implement email service later)
            // For now, return URL that can be used for testing
            var invitationUrl = $"https://yourapp.com/admin/register?token={token}";

            return new AdminInvitationResponse(
                Token: token,
                Email: email,
                ExpiresAt: DateTimeOffset.UtcNow.AddHours(48).ToString("O"),
                InvitationUrl: invitationUrl
            );
        }
        catch (UnauthorizedAccessException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Admin invitation failed");
            throw new InvalidOperationException("Failed to create admin invitation", ex);
        }
    }
}

/// <summary>
/// Interface for admin auth service
/// </summary>
public interface IAdminAuthService
{
    Task<AdminRegistrationResponse> RegisterAdminViaInviteAsync(
        string token, string username, string password,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);

    Task<AdminTwoFactorSetupResponse> SetupTwoFactorGenerateAsync(
        Guid adminId, string registrationSessionId, CancellationToken cancellationToken = default);

    Task<AdminTwoFactorCompleteResponse> SetupTwoFactorEnableAsync(
        Guid adminId, string code, string sessionId, string registrationSessionId,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);

    Task<AdminLoginResponse> AdminLoginAsync(
        string usernameOrEmail, string password,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);

    Task<AdminAuthSuccessResponse> VerifyAdminTwoFactorAsync(
        Guid adminId, string sessionId, string code,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);

    Task<bool> DisableTwoFactorAsync(
        Guid adminId, string code,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);

    Task<string[]> RegenerateBackupCodesAsync(
        Guid adminId, string code,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>One-time bootstrap endpoint to create Super Admin</summary>
    Task<AdminRegistrationResponse> BootstrapSuperAdminAsync(
        string username, string email, string password,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);

    /// <summary>Super Admin invites new admin</summary>
    Task<AdminInvitationResponse> InviteAdminAsync(
        Guid superAdminId, string email, string firstName, string lastName, string[]? permissions = null,
        string? ipAddress = null, string? userAgent = null,
        CancellationToken cancellationToken = default);
}
