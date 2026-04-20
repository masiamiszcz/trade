namespace TradingPlatform.Core.Dtos;

// InstrumentDto

public sealed record InstrumentDto(
    Guid Id,
    string Symbol,
    string Name,
    string Type,
    string Pillar,
    string BaseCurrency,
    string QuoteCurrency,
    bool IsActive,
    bool IsBlocked,
    DateTimeOffset CreatedAtUtc);

public sealed record CreateInstrumentRequest(
    string Symbol,
    string Name,
    string Type,
    string Pillar,
    string BaseCurrency,
    string QuoteCurrency);

public sealed record UpdateInstrumentRequest(
    string Name,
    bool IsActive);
