using TradingPlatform.Core.Enums;

namespace TradingPlatform.Core.Models;

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
    UserStatus Status,
    string BaseCurrency = "PLN",
    DateTimeOffset CreatedAtUtc = default,
    DateTimeOffset? BlockedUntilUtc = null,
    string? BlockReason = null,
    DateTimeOffset? DeletedAtUtc = null,
    DateTimeOffset? LastLoginAtUtc = null
)
{
    public DateTimeOffset CreatedAtUtcValue =>
        CreatedAtUtc == default ? DateTimeOffset.UtcNow : CreatedAtUtc;

    /// <summary>Computed property: user is blocked based on Status</summary>
    public bool IsBlocked => Status == UserStatus.Blocked;

    /// <summary>Computed property: user is deleted based on Status</summary>
    public bool IsDeleted => Status == UserStatus.Deleted;
}