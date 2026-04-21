
// IAdminAuthRepository
using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// Repository for admin auth operations
/// Handles user lookups and 2FA updates
/// </summary>
public interface IAdminAuthRepository
{
    /// <summary>Get admin by ID</summary>
    Task<User?> GetAdminByIdAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>Get admin by username</summary>
    Task<User?> GetAdminByUserNameAsync(string username, CancellationToken cancellationToken = default);

    /// <summary>Check if any admin exists (for bootstrap protection - ensures only ONE super admin)</summary>
    Task<bool> HasAnyAdminAsync(CancellationToken cancellationToken = default);

    /// <summary>Get admin with password hash (for login verification)</summary>
    Task<(User? admin, string? passwordHash)> GetAdminWithPasswordHashAsync(
        string usernameOrEmail, CancellationToken cancellationToken = default);

    /// <summary>Create new admin account</summary>
    Task CreateAdminAsync(User admin, string passwordHash, CancellationToken cancellationToken = default);

    /// <summary>Update admin 2FA settings (save encrypted secret + backup codes)</summary>
    Task UpdateAdminTwoFactorAsync(
        Guid adminId, string encryptedSecret, string backupCodesJson, bool enabled,
        CancellationToken cancellationToken = default);

    /// <summary>Clear admin 2FA settings (disable 2FA)</summary>
    Task ClearAdminTwoFactorAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>Update backup codes after using one</summary>
    Task UpdateAdminBackupCodesAsync(Guid adminId, string backupCodesJson, CancellationToken cancellationToken = default);

    /// <summary>Update last login attempt timestamp</summary>
    Task UpdateLastLoginAttemptAsync(Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>Check if admin is super admin (used for invite privilege check)</summary>
    Task<bool> IsUserSuperAdminAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Create admin entity with SuperAdmin flag (must call after CreateAdminAsync)</summary>
    Task CreateAdminEntityAsync(Guid userId, bool isSuperAdmin, CancellationToken cancellationToken = default);
}
    /// <summary>
    /// Repository for admin invitation management
    /// </summary>
    public interface IAdminInvitationRepository
    {
        /// <summary>Get invitation by token</summary>
        Task<dynamic?> GetByTokenAsync(string token, CancellationToken cancellationToken = default);

        /// <summary>Get invitation by email</summary>
        Task<dynamic?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

        /// <summary>Get invitation by id</summary>
        Task<dynamic?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>Get all expired invitations</summary>
        Task<dynamic> GetExpiredInvitationsAsync(CancellationToken cancellationToken = default);

        /// <summary>Add new invitation</summary>
        Task AddAsync(dynamic invitation, CancellationToken cancellationToken = default);

        /// <summary>Update invitation</summary>
        Task UpdateAsync(dynamic invitation, CancellationToken cancellationToken = default);

        /// <summary>Save changes</summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Repository for admin registration log
    /// </summary>
    public interface IAdminRegistrationLogRepository
    {
        /// <summary>Add registration log entry</summary>
        Task AddAsync(dynamic log, CancellationToken cancellationToken = default);

        /// <summary>Save changes</summary>
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
