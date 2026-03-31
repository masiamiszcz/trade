using TradingPlatform.Core.Enums;

namespace TradingPlatform.Core.Models;

public sealed record Instrument(
    Guid Id,
    string Symbol,
    string Name,
    InstrumentType Type,
    AccountPillar Pillar,
    string BaseCurrency,
    string QuoteCurrency,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
