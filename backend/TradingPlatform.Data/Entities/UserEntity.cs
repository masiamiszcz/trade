using TradingPlatform.Core.Enums;

namespace TradingPlatform.Data.Entities;

public sealed class UserEntity
{
    public Guid Id { get; set; }

    // 👤 IDENTITY
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    // 🔐 AUTH
    public string PasswordHash { get; set; } = string.Empty;

    // 📧 EMAIL
    public bool EmailConfirmed { get; set; }

    // 🔐 2FA
    public bool TwoFactorEnabled { get; set; }
    public string? TwoFactorSecret { get; set; } // encrypted TOTP secret
    public string? BackupCodes { get; set; }     // hashed JSON array

    // 🧠 SECURITY CORE
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    // 🚦 ACCOUNT STATE
    public UserStatus Status { get; set; } = UserStatus.PendingEmailConfirmation;
    public UserRole Role { get; set; } = UserRole.User;

    // 💰 FINANCE
    public string BaseCurrency { get; set; } = "PLN";

    // 🕒 AUDIT
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? UpdatedAtUtc { get; set; }

    public DateTimeOffset? LastLoginAtUtc { get; set; }
    public DateTimeOffset? LastLoginAttemptUtc { get; set; }

    // 🚫 SECURITY / BLOCKING (TO CI BRAKOWAŁO)
    public DateTimeOffset? BlockedUntilUtc { get; set; }
    public string? BlockReason { get; set; }

    // 🗑 SOFT DELETE
    public DateTimeOffset? DeletedAtUtc { get; set; }

    // 🧾 NAVIGATION
    public ICollection<AccountEntity> Accounts { get; set; } = new List<AccountEntity>();
}