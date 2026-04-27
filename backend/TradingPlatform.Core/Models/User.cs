using TradingPlatform.Core.Enums;

namespace TradingPlatform.Core.Models;

/// <summary>
/// User domain model - SINGLE SOURCE OF TRUTH for user data.
/// Status field drives all authentication and authorization logic.
/// Block/Delete information is immutable audit trail.
/// </summary>
public sealed record User(
    Guid Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    string TwoFactorSecret,
    string BackupCodes,
    UserStatus Status,                      // SINGLE SOURCE OF TRUTH
    string BaseCurrency,
    DateTimeOffset CreatedAtUtc,
    
    // NEW: User Lifecycle Management
    DateTimeOffset? BlockedUntilUtc = null,     // 48h temporary block
    string? BlockReason = null,                 // Why was user blocked?
    DateTimeOffset? DeletedAtUtc = null,        // When was user soft-deleted?
    string? DeleteReason = null,                // Why was user deleted?
    Guid? LastModifiedByAdminId = null         // Which admin made last change?
);
