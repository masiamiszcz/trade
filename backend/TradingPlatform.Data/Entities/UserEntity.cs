using TradingPlatform.Core.Enums;

namespace TradingPlatform.Data.Entities;

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
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");
    public UserStatus Status { get; set; } = UserStatus.PendingEmailConfirmation;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public ICollection<AccountEntity> Accounts { get; set; } = [];
}
