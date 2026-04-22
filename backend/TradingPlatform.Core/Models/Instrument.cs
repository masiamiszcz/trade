using TradingPlatform.Core.Enums;

namespace TradingPlatform.Core.Models;

/// <summary>
/// Instrument - Master data dla instrumentów handlowych
/// Enterprise Pattern: Immutable record z jasnym stanem workflow
/// </summary>
public sealed record Instrument(
    Guid Id,
    string Symbol,
    string Name,
    string Description,                // ← NOWE: Opis dla admina
    InstrumentType Type,
    AccountPillar Pillar,
    string BaseCurrency,
    string QuoteCurrency,
    InstrumentStatus Status,           // ← NOWE: Kanoniczny status workflow
    bool IsBlocked,                    // ← Zachowujemy, ale teraz tylko dla fast-path
    Guid CreatedBy,                    // ← NOWE: Który admin utworzył
    DateTimeOffset CreatedAtUtc,
    Guid? ModifiedBy = null,           // ← NOWE: Który admin zmienił
    DateTimeOffset? ModifiedAtUtc = null, // ← NOWE: Kiedy ostatnia zmiana
    uint RowVersion = 0);              // ← NOWE: Concurrency token (optimistic locking)
