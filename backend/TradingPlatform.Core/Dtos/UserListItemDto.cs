namespace TradingPlatform.Core.Dtos;

/// <summary>
/// DTO for user list items in admin dashboard
/// Includes block/delete information for management
/// IsBlocked is computed based on Status, not stored separately
/// </summary>
public sealed record UserListItemDto(
    Guid Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    string Role,
    string Status,
    string? BlockReason,
    DateTimeOffset? BlockedUntilUtc,
    DateTimeOffset? DeletedAtUtc,
    DateTimeOffset? LastLoginAtUtc,
    DateTimeOffset CreatedAtUtc)
{
    /// <summary>Computed: user is blocked based on Status</summary>
    public bool IsBlocked => Status == "Blocked";
}
