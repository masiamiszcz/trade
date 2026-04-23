namespace TradingPlatform.Core.Dtos;

using TradingPlatform.Core.Enums;

/// <summary>
/// Response DTO - Kanoniczny kontrakt dla instrumentu
/// </summary>
public sealed record InstrumentDto(
    Guid Id,
    string Symbol,
    string Name,
    string Description,
    string Type,
    string Pillar,
    string BaseCurrency,
    string QuoteCurrency,
    string Status,                     // Status workflow
    bool IsBlocked,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc,
    Guid? ModifiedBy = null,
    DateTimeOffset? ModifiedAtUtc = null,
    uint RowVersion = 0);

/// <summary>
/// Create Request - Minimal input dla nowego instrumentu
/// </summary>
public sealed record CreateInstrumentRequest(
    string Symbol,
    string Name,
    string Description,
    string Type,
    string Pillar,
    string BaseCurrency,
    string QuoteCurrency);

/// <summary>
/// Update Request - Edycja istniejącego instrumentu
/// </summary>
public sealed record UpdateInstrumentRequest(
    string Name,
    string? Description = null,
    string? BaseCurrency = null,
    string? QuoteCurrency = null);

/// <summary>
/// Reject Request - Odrzucenie instrumentu w oczekiwaniu na zatwierdzenie
/// Wymagane pole Reason (min 10 znaków) dla audytu
/// </summary>
public sealed record RejectInstrumentRequest(
    string Reason);

/// <summary>
/// Generic Request DTO - Reason/comment for administrative actions
/// Used for block/unblock requests
/// </summary>
public sealed record AdminRequestReasonRequest(
    string Reason);
