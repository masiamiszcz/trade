using TradingPlatform.Core.Enums;

namespace TradingPlatform.Core.Models;

public sealed record User(
    Guid Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    bool EmailConfirmed,
    bool TwoFactorEnabled,
    UserStatus Status,
    DateTimeOffset CreatedAtUtc);
