namespace TradingPlatform.Data.Entities;

/// <summary>
/// Admin entity - represents admin privileges for a user
/// Users table = base, Admins table = role-specific (1:1 relationship)
/// This allows tracking which users are admins and who is super admin
/// </summary>
public sealed class AdminEntity
{
    /// <summary>Foreign key to Users table (PK)</summary>
    public Guid UserId { get; set; }

    /// <summary>Is this user a super admin? Only ONE super admin allowed (set during bootstrap)</summary>
    public bool IsSuperAdmin { get; set; }

    /// <summary>Navigation property to User</summary>
    public UserEntity User { get; set; } = null!;
}
