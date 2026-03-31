using TradingPlatform.Core.Enums;

namespace TradingPlatform.Core.Models;

public sealed record Account(
    Guid Id,
    Guid UserId,
    Guid? ParentAccountId,
    string AccountNumber,
    string Name,
    AccountType AccountType,
    AccountPillar Pillar,
    AccountStatus Status,
    string Currency,
    decimal AvailableBalance,
    decimal ReservedBalance,
    DateTimeOffset CreatedAtUtc);
