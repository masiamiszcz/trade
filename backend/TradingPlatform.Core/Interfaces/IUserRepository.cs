using TradingPlatform.Core.Models;

namespace TradingPlatform.Core.Interfaces;

/// <summary>
/// User repository contract.
/// Handles all user persistence and lifecycle operations.
/// </summary>
public interface IUserRepository
{
    // ===== READ OPERATIONS =====

    Task<User?> GetByUserNameAsync(string userName, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get user including soft-deleted.
    /// Used for validation (e.g., can't delete already deleted user).
    /// </summary>
    Task<User?> GetUserByIdIncludingDeletedAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<(User? user, string? passwordHash)> GetByUserNameOrEmailWithPasswordHashAsync(string userNameOrEmail, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all active users (excludes soft-deleted by default).
    /// </summary>
    Task<IEnumerable<User>> GetAllUsersAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all users with optional soft-deleted filtering.
    /// </summary>
    Task<IEnumerable<User>> GetAllUsersAsync(bool includeDeleted = false, CancellationToken cancellationToken = default);

    // ===== WRITE OPERATIONS =====

    Task AddAsync(User user, string passwordHash, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    // ===== USER LIFECYCLE OPERATIONS (NO APPROVAL) =====

    /// <summary>
    /// Block user temporarily (immediate operation, no approval needed).
    /// Status → Blocked, sets BlockedUntilUtc (usually 48h from now).
    /// </summary>
    Task BlockUserAsync(Guid userId, string reason, DateTimeOffset blockedUntil, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unblock user (immediate operation, no approval needed).
    /// Status → Active, clears BlockedUntilUtc.
    /// </summary>
    Task UnblockUserAsync(Guid userId, Guid adminId, CancellationToken cancellationToken = default);

    // ===== USER LIFECYCLE OPERATIONS (WITH APPROVAL) =====

    /// <summary>
    /// Soft delete user after approval.
    /// Status → Deleted, sets DeletedAtUtc.
    /// </summary>
    Task DeleteUserAsync(Guid userId, string reason, Guid adminId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Restore soft-deleted user after approval.
    /// Status → Active, clears DeletedAtUtc.
    /// </summary>
    Task RestoreUserAsync(Guid userId, Guid adminId, CancellationToken cancellationToken = default);
}
