namespace TradingPlatform.Core.Dtos;

public sealed record AccountDto(
    Guid Id,
    Guid UserId,
    Guid? ParentAccountId,
    string AccountNumber,
    string Name,
    string AccountType,
    string Pillar,
    string Status,
    string Currency,
    decimal AvailableBalance,
    decimal ReservedBalance,
    DateTimeOffset CreatedAtUtc);
