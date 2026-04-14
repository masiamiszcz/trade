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
    UserStatus Status,
    string BaseCurrency,
    DateTimeOffset CreatedAtUtc);
