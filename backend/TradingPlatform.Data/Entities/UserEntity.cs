using TradingPlatform.Core.Enums;

namespace TradingPlatform.Data.Entities;

/// <summary>
/// User entity for EF Core persistence.
/// Mirrors User domain model with database-specific properties.
/// </summary>
public sealed class UserEntity
{
    public Guid Id { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool EmailConfirmed { get; set; }
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; }
    public string? BackupCodes { get; set; }
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    
    // SINGLE SOURCE OF TRUTH
    public UserStatus Status { get; set; } = UserStatus.PendingEmailConfirmation;
    public UserRole Role { get; set; } = UserRole.User;
    
    public string BaseCurrency { get; set; } = "PLN";
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }
    public DateTimeOffset? LastLoginAttempt { get; set; }
    
    // NEW: User Lifecycle Management (mapped to domain model)
    public DateTimeOffset? BlockedUntilUtc { get; set; }              // 48h temporary block
    public string? BlockReason { get; set; }                         // Why was user blocked?
    public DateTimeOffset? DeletedAtUtc { get; set; }                // When was user soft-deleted?
    public string? DeleteReason { get; set; }                        // Why was user deleted?
    public Guid? LastModifiedByAdminId { get; set; }                 // Which admin modified?

    // Navigation properties
    public ICollection<AccountEntity> Accounts { get; set; } = [];
}
